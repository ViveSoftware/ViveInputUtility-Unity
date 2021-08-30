//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
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
        private uint uxrRightIndex = INVALID_DEVICE_INDEX;
        private uint uxrLeftIndex = INVALID_DEVICE_INDEX;
        private uint moduleRightIndex = INVALID_DEVICE_INDEX;
        private uint moduleLeftIndex = INVALID_DEVICE_INDEX;
        private VRModule.SubmoduleBase.Collection submodules = new VRModule.SubmoduleBase.Collection(
            new ViveHandTrackingSubmodule(),
            new WaveHandTrackingSubmodule()
            );

        protected VRModuleKnownXRLoader KnownActiveLoader { get { return knownActiveLoader; } }
        protected VRModuleKnownXRInputSubsystem KnownActiveInputSubsystem { get { return knownActiveInputSubsystem; } }

        public override void OnActivated()
        {
            knownActiveLoader = GetKnownActiveLoader();
            knownActiveInputSubsystem = GetKnownActiveInputSubsystem();
            EnsureDeviceStateLength(8);
            UpdateTrackingSpaceType();
            submodules.ActivateAllModules();
            Debug.Log("Activated XRLoader Name: " + XRGeneralSettings.Instance.Manager.activeLoader.name);
        }

        public override void OnDeactivated()
        {
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

            // mark all devices as disconnected
            // therefore, if a device should stay alive in this frame
            // should be set as connected in the next stage
            deviceIndex = 0u;
            for (var len = GetDeviceStateLength(); deviceIndex < len; ++deviceIndex)
            {
                if (TryGetValidDeviceState(deviceIndex, out prevState, out currState))
                {
                    currState.isConnected = false;
                }
            }

            InputDevices.GetDevices(connectedDevices);

            foreach (var device in connectedDevices)
            {
                if (!indexMap.TryGetIndex(device, out deviceIndex))
                {
                    if (indexMap.TryMapAsHMD(device))
                    {
                        deviceIndex = VRModule.HMD_DEVICE_INDEX;
                        EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                    }
                    else
                    {
                        // this function will skip VRModule.HMD_DEVICE_INDEX (preserved index for HMD)
                        deviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);
                        indexMap.MapNonHMD(device, deviceIndex);
                    }

                    currState.deviceClass = GetDeviceClass(device.name, device.characteristics);
                    currState.serialNumber = device.name + " " + device.serialNumber + " " + (int)device.characteristics;
                    currState.modelNumber = device.name + " (" + device.characteristics + ")";
                    currState.renderModelName = device.name + " (" + device.characteristics + ")";

                    SetupKnownDeviceModel(currState);

                    Debug.LogFormat("Device connected: {0} / {1} / {2} / {3} / {4} / {5} ({6})",
                        currState.deviceIndex,
                        currState.deviceClass,
                        currState.deviceModel,
                        currState.modelNumber,
                        currState.serialNumber,
                        device.name,
                        device.characteristics);

                    if ((device.characteristics & InputDeviceCharacteristics.Right) > 0u)
                    {
                        uxrRightIndex = deviceIndex;
                    }
                    else if ((device.characteristics & InputDeviceCharacteristics.Left) > 0u)
                    {
                        uxrLeftIndex = deviceIndex;
                    }

                    UpdateNewConnectedInputDevice(currState, device);
                }
                else
                {
                    EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                }
                currState.isConnected = true;
                // update device Poses
                currState.isPoseValid = GetDeviceFeatureValueOrDefault(device, CommonUsages.isTracked);
                currState.position = GetDeviceFeatureValueOrDefault(device, CommonUsages.devicePosition);
                currState.rotation = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceRotation);
                currState.velocity = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceVelocity);
                currState.angularVelocity = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceAngularVelocity);

                // TODO: update hand skeleton pose
            }

            // unmap index for disconnected device state
            deviceIndex = 0u;
            for (var len = GetDeviceStateLength(); deviceIndex < len; ++deviceIndex)
            {
                if (indexMap.IsMapped(deviceIndex))
                {
                    EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                    if (prevState.isConnected && !currState.isConnected)
                    {
                        indexMap.UnmapByIndex(deviceIndex);
                        currState.Reset();
                        if (uxrRightIndex == deviceIndex) { uxrRightIndex = INVALID_DEVICE_INDEX; }
                        if (uxrLeftIndex == deviceIndex) { uxrLeftIndex = INVALID_DEVICE_INDEX; }
                    }
                }
            }

            submodules.UpdateModulesDeviceConnectionAndPoses();

            // process hand role
            var subRightIndex = submodules.GetFirstRightHandedIndex();
            var currentRight = (subRightIndex == INVALID_DEVICE_INDEX || (TryGetValidDeviceState(uxrRightIndex, out prevState, out currState) && currState.isPoseValid)) ? uxrRightIndex : subRightIndex;
            var subLeftIndex = submodules.GetFirstLeftHandedIndex();
            var currentLeft = (subLeftIndex == INVALID_DEVICE_INDEX || (TryGetValidDeviceState(uxrLeftIndex, out prevState, out currState) && currState.isPoseValid)) ? uxrLeftIndex : subLeftIndex;
            var roleChanged = ChangeProp.Set(ref moduleRightIndex, currentRight);
            roleChanged |= ChangeProp.Set(ref moduleLeftIndex, currentLeft);

            if (roleChanged)
            {
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
                    if ((device.characteristics & InputDeviceCharacteristics.Controller) > 0)
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

            if ((characteristics & InputDeviceCharacteristics.TrackedDevice) != 0)
            {
                return VRModuleDeviceClass.GenericTracker;
            }

            return VRModuleDeviceClass.Invalid;
        }

        private static void SetAllXRInputSubsystemTrackingOriginMode(TrackingOriginModeFlags value)
        {
            var activeSubsys = ListPool<XRInputSubsystem>.Get();
            try
            {
                SubsystemManager.GetInstances(activeSubsys);
                foreach (var subsys in activeSubsys)
                {
                    if (!subsys.running) { continue; }
                    if (!subsys.TrySetTrackingOriginMode(value))
                    {
                        Debug.LogWarning("Failed to set TrackingOriginModeFlags(" + value + ") to XRInputSubsystem: " + subsys.SubsystemDescriptor.id);
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
                    SubsystemManager.GetInstances(displaySystems);

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
