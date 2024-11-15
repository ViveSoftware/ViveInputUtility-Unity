//========= Copyright 2016-2024, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public enum VRModuleKnownXRLoader
    {
        Unknown,
        OpenVR,
        Oculus,
        WindowsXR,
        MagicLeap,
        WaveXR,
        OpenXR,
    }

    public enum VRModuleKnownXRInputSubsystem
    {
        Unknown,
        OpenVR,
        Oculus,
        WindowsXR,
        MagicLeap,
        WaveXR,
        OpenXR,
    }

    public abstract partial class UnityXRModuleBase : VRModule.ModuleBase
    {
#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
        private struct XRLoaderProfile
        {
            public VRModuleKnownXRLoader loader;
            public Regex matchNameRgx;
            public string fixedName;
        }

        private struct XRInputSubsystemProfile
        {
            public VRModuleKnownXRInputSubsystem subsystem;
            public Regex matchNameRgx;
            public string fixedName;
        }

        private static List<XRLoaderProfile> loaderProfiles = new List<XRLoaderProfile>()
        {
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.OpenVR, matchNameRgx = new Regex("openvr", REGEX_OPTIONS) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.Oculus, matchNameRgx = new Regex("oculus", REGEX_OPTIONS) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.WindowsXR, matchNameRgx = new Regex("windows", REGEX_OPTIONS) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.MagicLeap, matchNameRgx = new Regex("magicleap", REGEX_OPTIONS) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.WaveXR, matchNameRgx = new Regex("wave", REGEX_OPTIONS) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.OpenXR, matchNameRgx = new Regex("open xr", REGEX_OPTIONS) },
        };

        private static List<XRInputSubsystemProfile> inputSubsystemProfiles = new List<XRInputSubsystemProfile>()
        {
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.OpenVR, matchNameRgx = new Regex("openvr", REGEX_OPTIONS) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.Oculus, matchNameRgx = new Regex("oculus", REGEX_OPTIONS) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.WindowsXR, matchNameRgx = new Regex("windows", REGEX_OPTIONS) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.MagicLeap, matchNameRgx = new Regex("magicleap", REGEX_OPTIONS) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.WaveXR, matchNameRgx = new Regex("wave", REGEX_OPTIONS) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.OpenXR, matchNameRgx = new Regex("openxr", REGEX_OPTIONS) },
        };

        private VRModuleKnownXRLoader knownActiveLoader;
        private VRModuleKnownXRInputSubsystem knownActiveInputSubsystem;
        private IndexMap indexMap = new IndexMap();
        private uint moduleRightIndex = INVALID_DEVICE_INDEX;
        private uint moduleLeftIndex = INVALID_DEVICE_INDEX;

        private enum RolePriority
        {
            Controller,
            TrackedHand,
            Tracker,
            Other,
        }
        private struct RoleIdx
        {
            public IVRModuleDeviceStateRW ds;
            public RolePriority pri;
            public uint index { get { return ds.deviceIndex; } }
            public RoleIdx(IVRModuleDeviceStateRW deviceState)
            {
                ds = deviceState;
                switch (deviceState.deviceClass)
                {
                    case VRModuleDeviceClass.Controller: pri = RolePriority.Controller; break;
                    case VRModuleDeviceClass.TrackedHand: pri = RolePriority.TrackedHand; break;
                    case VRModuleDeviceClass.GenericTracker: pri = RolePriority.Tracker; break;
                    default: pri = RolePriority.Other; break;
                }
            }
        }
        private List<RoleIdx> rightIndice = new List<RoleIdx>();
        private List<RoleIdx> leftIndice = new List<RoleIdx>();
        private static int CompareRoleIdx(RoleIdx a, RoleIdx b)
        {
            var c = a.ds.isPoseValid.CompareTo(b.ds.isPoseValid);
            if (c != 0) { return -c; }

            c = a.pri - b.pri;
            if (c != 0) { return c; }

            if (a.pri == RolePriority.TrackedHand)
            {
                foreach (var j in JointEnumArray.StaticEnums)
                {
                    c = a.ds.handJoints[j].isValid.CompareTo(b.ds.handJoints[j].isValid);
                    if (c != 0) { return -c; }
                }
            }

            return a.ds.deviceIndex.CompareTo(b.ds.deviceIndex);
        }

        private static string shortIdx(uint i) { return i == INVALID_DEVICE_INDEX ? "X" : i.ToString(); }

        private VRModule.SubmoduleBase.Collection submodules = new VRModule.SubmoduleBase.Collection(
            new ViveHandTrackingSubmodule()
            , new WaveHandTrackingSubmodule()
            , new WaveTrackerSubmodule()
            , new UnityXRHandSubmodule()
            );

        private bool[] prevDeviceConnected = new bool[VRModule.MAX_DEVICE_COUNT];
        private bool[] currDeviceConnected = new bool[VRModule.MAX_DEVICE_COUNT];
        private int prevMaxConnectedIndex = -1;
        private int currMaxConnectedIndex = -1;
        private void FlushDeviceConnectedState()
        {
            var temp = prevDeviceConnected;
            prevDeviceConnected = currDeviceConnected;
            currDeviceConnected = temp;
            prevMaxConnectedIndex = currMaxConnectedIndex;
            currMaxConnectedIndex = -1;
            Array.Clear(currDeviceConnected, 0, (int)VRModule.MAX_DEVICE_COUNT);
        }

        protected VRModuleKnownXRLoader KnownActiveLoader { get { return knownActiveLoader; } }
        protected VRModuleKnownXRInputSubsystem KnownActiveInputSubsystem { get { return knownActiveInputSubsystem; } }

        public override void OnActivated()
        {
            knownActiveLoader = GetKnownActiveLoader();
            knownActiveInputSubsystem = GetKnownActiveInputSubsystem();
            EnsureDeviceStateLength(8);
            UpdateTrackingSpaceType();
            submodules.ActivateAllModules();
            Debug.Log("[UnityXRModule] OnActivated XRLoader Name: " + XRGeneralSettings.Instance.Manager.activeLoader.name);
        }

        public override void OnDeactivated()
        {
            Debug.Log("[UnityXRModule] OnDeactivated");
            submodules.DeactivateAllModules();
            indexMap.Clear();
        }

        public override void UpdateTrackingSpaceType()
        {
            TrackingOriginModeFlags originFlag;
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.Stationary: originFlag = TrackingOriginModeFlags.Device; break;
                case VRModuleTrackingSpaceType.RoomScale: originFlag = TrackingOriginModeFlags.Floor; break;
                default: return;
            }

            SetAllXRInputSubsystemTrackingOriginMode(originFlag);
        }

        private List<InputDevice> connectedDevices = new List<InputDevice>();
        public sealed override void BeforeRenderUpdate()
        {
            if (knownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.Unknown)
            {
                knownActiveInputSubsystem = GetKnownActiveInputSubsystem();
            }

            // update device connection and poses
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            uint deviceIndex;

            FlushDeviceState();
            FlushDeviceConnectedState();
            rightIndice.Clear();
            leftIndice.Clear();

            InputDevices.GetDevices(connectedDevices);

            foreach (var device in connectedDevices)
            {
                if (!device.isValid) { continue; }
                if (!indexMap.TryGetIndex(device, out deviceIndex))
                {
                    string deviceName;
                    if (indexMap.TryMapAsHMD(device))
                    {
                        deviceIndex = VRModule.HMD_DEVICE_INDEX;
                        EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                        deviceName = device.name;
                    }
                    else
                    {
                        // this function will skip VRModule.HMD_DEVICE_INDEX (preserved index for HMD)
                        deviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);
                        indexMap.MapNonHMD(device, deviceIndex);
                        deviceName = device.name;
                    }

                    // will not identify tracked hand here, currently all tracked hand should created by submodule
                    currState.deviceClass = GetDeviceClass(device.name, device.characteristics);
                    currState.serialNumber = deviceName + " " + device.serialNumber + " " + (int)device.characteristics;
                    currState.modelNumber = deviceName + " (" + device.characteristics + ")";
                    currState.renderModelName = deviceName + " (" + device.characteristics + ")";

                    SetupKnownDeviceModel(currState);

                    Debug.LogFormat("[UnityXRModule] found new InputDevice: [{0}] nm=\"{2}\" sn=\"{1}\" ch=({4}) mf=\"{3}\"",
                        currState.deviceIndex,
                        device.serialNumber,
                        device.name,
                        device.manufacturer,
                        device.characteristics);

                    UpdateNewConnectedInputDevice(currState, device);
                }
                else
                {
                    EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                }

                // update device Poses
                currState.isConnected = true;
                currState.isPoseValid = GetDeviceFeatureValueOrDefault(device, CommonUsages.isTracked);

                if (VIUSettings.preferUnityXRPointerPose)
                {
                    currState.position = GetDeviceFeatureValueOrDefault(device, pointerPositionFeature, CommonUsages.devicePosition);
                    currState.rotation = GetDeviceFeatureValueOrDefault(device, pointerRotationFeature, CommonUsages.deviceRotation);
                    currState.velocity = GetDeviceFeatureValueOrDefault(device, pointerVelocityFeature, CommonUsages.deviceVelocity);
                    currState.angularVelocity = GetDeviceFeatureValueOrDefault(device, pointerAngularVelocityFeature, CommonUsages.deviceAngularVelocity);
                }
                else
                {
                    currState.position = GetDeviceFeatureValueOrDefault(device, CommonUsages.devicePosition);
                    currState.rotation = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceRotation);
                    currState.velocity = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceVelocity);
                    currState.angularVelocity = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceAngularVelocity);
                }

                currDeviceConnected[deviceIndex] = true;
                if (currMaxConnectedIndex < (int)deviceIndex) { currMaxConnectedIndex = (int)deviceIndex; }

                if (currState.deviceClass != VRModuleDeviceClass.Invalid)
                {
                    if ((device.characteristics & InputDeviceCharacteristics.Right) != 0u) { rightIndice.Add(new RoleIdx(currState)); }
                    else if ((device.characteristics & InputDeviceCharacteristics.Left) != 0u) { leftIndice.Add(new RoleIdx(currState)); }
                }
                // TODO: update hand skeleton pose
            }

            // unmap index for disconnected device state
            for (int i = 0, imax = prevMaxConnectedIndex; i <= imax; ++i)
            {
                if (prevDeviceConnected[i] && !currDeviceConnected[i])
                {
                    if (indexMap.IsMapped((uint)i))
                    {
                        indexMap.UnmapByIndex((uint)i);
                        //Debug.Log("[UnityXRModule] unmap [" + i + "]");
                    }
                    else
                    {
                        Debug.LogWarning("[UnityXRModule] unmap failed: [" + i + "] already unmapped");
                    }

                    if (TryGetValidDeviceState((uint)i, out prevState, out currState) && currState.isConnected)
                    {
                        currState.Reset();
                        //Debug.Log("[UnityXRModule] reset [" + i + "]");
                    }
                    else
                    {
                        Debug.LogWarning("[UnityXRModule] reset state failed: [" + i + "] already been reset");
                    }
                }
            }

            submodules.UpdateModulesDeviceConnectionAndPoses();

            for (uint i = 0u, imax = GetDeviceStateLength(); i < imax; ++i)
            {
                if (!TryGetValidDeviceState(i, out prevState, out currState)) { continue; }
                if (prevState.isConnected != currState.isConnected)
                {
                    var isDisconnect = prevState.isConnected;
                    var readState = isDisconnect ? prevState : (IVRModuleDeviceState)currState;
                    Debug.LogFormat("[UnityXRModule] device {0}connected: [{1}] sn=\"{5}\" cl={2} md={3} mn=\"{4}\"",
                        isDisconnect ? "dis" : "",
                        readState.deviceIndex,
                        readState.deviceClass,
                        readState.deviceModel,
                        readState.modelNumber,
                        readState.serialNumber);
                }
            }

            for (int i = 0, imax = submodules.ModuleCount; i < imax; ++i)
            {
                if (!submodules[i].isActivated) { continue; }
                if (TryGetValidDeviceState(submodules[i].GetRightHandedIndex(), out prevState, out currState) && currState.isConnected)
                {
                    rightIndice.Add(new RoleIdx(currState));
                }
                if (TryGetValidDeviceState(submodules[i].GetLeftHandedIndex(), out prevState, out currState) && currState.isConnected)
                {
                    leftIndice.Add(new RoleIdx(currState));
                }
            }

            // process hand role
            var newRightIndex = INVALID_DEVICE_INDEX;
            if (rightIndice.Count != 0)
            {
                if (rightIndice.Count != 1) { rightIndice.Sort(CompareRoleIdx); }
                newRightIndex = rightIndice[0].index;
            }
            var newLeftIndex = INVALID_DEVICE_INDEX;
            if (leftIndice.Count != 0)
            {
                if (leftIndice.Count != 1) { leftIndice.Sort(CompareRoleIdx); }
                newLeftIndex = leftIndice[0].index;
            }
            if ((moduleRightIndex != newRightIndex) | (moduleLeftIndex != newLeftIndex))
            {
                Debug.Log("[UnityXRModule] role changed: [" + shortIdx(moduleLeftIndex) + "=>" + shortIdx(newLeftIndex) + "]L [" + shortIdx(moduleRightIndex) + "=>" + shortIdx(newRightIndex) + "]R");
                moduleRightIndex = newRightIndex;
                moduleLeftIndex = newLeftIndex;
                InvokeControllerRoleChangedEvent();
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
        }

        public override void Update()
        {
            UpdateLockPhysicsUpdateRate();

            for (uint deviceIndex = 0u, len = GetDeviceStateLength(); deviceIndex < len; ++deviceIndex)
            {
                InputDevice device;
                if (indexMap.TryGetDevice(deviceIndex, out device))
                {
                    if ((device.characteristics & InputDeviceCharacteristics.Controller) > 0
                        || (device.characteristics & InputDeviceCharacteristics.TrackedDevice) > 0)
                    {
                        IVRModuleDeviceState prevState;
                        IVRModuleDeviceStateRW currState;
                        EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                        UpdateInputDevicesControllerState(currState, device);
                    }
                }
            }

            submodules.UpdateAllModulesActivity();
            submodules.UpdateModulesDeviceInput();

            UpdateHapticVibration();
            ProcessDeviceInputChanged();
        }

        public override uint GetRightControllerDeviceIndex() { return moduleRightIndex; }

        public override uint GetLeftControllerDeviceIndex() { return moduleLeftIndex; }

        protected virtual void UpdateNewConnectedInputDevice(IVRModuleDeviceStateRW state, InputDevice device) { }

        protected virtual void UpdateInputDevicesControllerState(IVRModuleDeviceStateRW state, InputDevice device) { }

        protected static VRModuleDeviceClass GetDeviceClass(string name, InputDeviceCharacteristics characteristics)
        {
            if ((characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
            {
                return VRModuleDeviceClass.HMD;
            }

            if ((characteristics & InputDeviceCharacteristics.Controller) != 0)
            {
                return VRModuleDeviceClass.Controller;
            }

            if ((characteristics & InputDeviceCharacteristics.TrackingReference) != 0)
            {
                return VRModuleDeviceClass.TrackingReference;
            }

            if ((characteristics & InputDeviceCharacteristics.HandTracking) != 0)
            {
                return VRModuleDeviceClass.Invalid;
            }

            if ((characteristics & InputDeviceCharacteristics.TrackedDevice) != 0)
            {
                return VRModuleDeviceClass.GenericTracker;
            }

            // will not identify tracked hand here, currently all tracked hand should created by submodule

            return VRModuleDeviceClass.Invalid;
        }

        private static void SetAllXRInputSubsystemTrackingOriginMode(TrackingOriginModeFlags value)
        {
            var activeSubsys = ListPool<XRInputSubsystem>.Get();
            try
            {
#if UNITY_6000_0_OR_NEWER
                SubsystemManager.GetSubsystems(activeSubsys);
#else
                SubsystemManager.GetInstances(activeSubsys);
#endif
                foreach (var subsys in activeSubsys)
                {
                    if (!subsys.running) { continue; }
                    if (!subsys.TrySetTrackingOriginMode(value))
                    {
#if UNITY_6000_0_OR_NEWER
                        var subsysId = subsys.subsystemDescriptor.id;
#else
                        var subsysId = subsys.SubsystemDescriptor.id;
#endif
                        Debug.LogWarning("Failed to set TrackingOriginModeFlags(" + value + ") to XRInputSubsystem: " + subsysId);
                    }
                }
            }
            finally { ListPool<XRInputSubsystem>.Release(activeSubsys); }
        }

        private struct HapticState
        {
            public float amplitude;
            public float startTime;
            public float endTime;

            public HapticState(float amp, float startTime, float endTime)
            {
                this.amplitude = amp;
                this.endTime = endTime;
                this.startTime = startTime;
            }
        }

        private uint maxHapticStateIndex = 0u;
        private HapticState[] hapticStates = new HapticState[VRModule.MAX_DEVICE_COUNT];

        public override void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            TriggerHapticVibration(deviceIndex, durationMicroSec * 1000000f);
        }

        // NOTE: Frequency not supported
        public override void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85.0f, float amplitude = 0.125f, float startSecondsFromNow = 0.0f)
        {
            InputDevice device;
            if (indexMap.TryGetDevice(deviceIndex, out device))
            {
                HapticCapabilities capabilities;
                if (device.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        var now = Time.unscaledTime;
                        hapticStates[deviceIndex] = new HapticState()
                        {
                            amplitude = amplitude,
                            startTime = now + startSecondsFromNow,
                            endTime = now + startSecondsFromNow + durationSeconds,
                        };

                        if (deviceIndex > maxHapticStateIndex) { maxHapticStateIndex = deviceIndex; }
                    }
                }
            }
        }

        protected void UpdateHapticVibration()
        {
            if (maxHapticStateIndex > 0u)
            {
                var now = Time.unscaledTime;
                var newMaxIndex = 0u;
                for (uint i = 0, imax = maxHapticStateIndex; i <= imax; ++i)
                {
                    InputDevice device;
                    if (now <= hapticStates[i].endTime)
                    {
                        if (now >= hapticStates[i].startTime && indexMap.TryGetDevice(i, out device))
                        {
                            device.SendHapticImpulse(0u, hapticStates[i].amplitude);
                        }

                        if (i > newMaxIndex) { newMaxIndex = i; }
                    }
                }
                maxHapticStateIndex = newMaxIndex;
            }
        }

        protected void UpdateLockPhysicsUpdateRate()
        {
            if (VRModule.lockPhysicsUpdateRateToRenderFrequency && Time.timeScale > 0.0f)
            {
                var displaySystems = ListPool<XRDisplaySubsystem>.Get();
                try
                {
#if UNITY_6000_0_OR_NEWER
                    SubsystemManager.GetSubsystems(displaySystems);
#else
                    SubsystemManager.GetInstances(displaySystems);
#endif

                    var minRefreshRate = float.MaxValue;
                    foreach (XRDisplaySubsystem system in displaySystems)
                    {
                        float rate = 60.0f;
                        if (system.TryGetDisplayRefreshRate(out rate))
                        {
                            if (rate < minRefreshRate)
                            {
                                minRefreshRate = rate;
                            }
                        }
                    }

                    if (minRefreshRate > 0 && minRefreshRate < float.MaxValue)
                    {
                        Time.fixedDeltaTime = 1.0f / minRefreshRate;
                    }
                }
                finally { ListPool<XRDisplaySubsystem>.Release(displaySystems); }
            }
        }
#endif
    }
}
