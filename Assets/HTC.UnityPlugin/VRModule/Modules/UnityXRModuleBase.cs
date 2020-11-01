//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

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
    }

    public enum VRModuleKnownXRInputSubsystem
    {
        Unknown,
        OpenVR,
        Oculus,
        WindowsXR,
        MagicLeap,
        WaveXR,
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
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.OpenVR, matchNameRgx = new Regex("openvr", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.Oculus, matchNameRgx = new Regex("oculus", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.WindowsXR, matchNameRgx = new Regex("windows", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.MagicLeap, matchNameRgx = new Regex("magicleap", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.WaveXR, matchNameRgx = new Regex("wave", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        };

        private static List<XRInputSubsystemProfile> inputSubsystemProfiles = new List<XRInputSubsystemProfile>()
        {
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.OpenVR, matchNameRgx = new Regex("openvr", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.Oculus, matchNameRgx = new Regex("oculus", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.WindowsXR, matchNameRgx = new Regex("windows", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.MagicLeap, matchNameRgx = new Regex("magicleap", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.WaveXR, matchNameRgx = new Regex("wave", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        };

        private VRModuleKnownXRLoader knownActiveLoader;
        private VRModuleKnownXRInputSubsystem knownActiveInputSubsystem;

        protected VRModuleKnownXRLoader KnownActiveLoader { get { return knownActiveLoader; } }
        protected VRModuleKnownXRInputSubsystem KnownActiveInputSubsystem { get { return knownActiveInputSubsystem; } }

        public override void OnActivated()
        {
            knownActiveLoader = GetKnownActiveLoader();
            knownActiveInputSubsystem = GetKnownActiveInputSubsystem();
            EnsureDeviceStateLength(8);
            UpdateTrackingSpaceType();
            Debug.Log("Activated XRLoader Name: " + XRGeneralSettings.Instance.Manager.activeLoader.name);
        }

        public override void OnDeactivated()
        {
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
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            uint deviceIndex;

            FlushDeviceState();

            // mark all devices as disconnected
            // therefore, if a device should stay alive in this frame
            // should be set as connected in the next stage
            deviceIndex = 0u;
            while (TryGetValidDeviceState(deviceIndex++, out prevState, out currState))
            {
                currState.isConnected = false;
            }

            UpdateInputDevices();

            UpdateCustomDevices();

            // preserve last hand role index
            var pRightIndex = PrioritizedRightHandIndex;
            var pLeftIndex = PrioritizedLeftHandIndex;
            // process all disconnected devices
            deviceIndex = 0u;
            while (TryGetValidDeviceState(deviceIndex, out prevState, out currState))
            {
                if (!prevState.isConnected)
                {
                    if (currState.isConnected)
                    {
                        UpdateHandRoleForNewConnectedDevice(deviceIndex, currState);
                    }
                }
                else
                {
                    if (!currState.isConnected)
                    {
                        // reset all devices that is not connected in this frame
                        currState.Reset();

                        UpdateHandRoleForDisconnectedDevice(deviceIndex);

                        if (indexMap.Index2Device(deviceIndex).isValid)
                        {
                            OnInputDeviceDisconnected(deviceIndex);
                        }
                        else
                        {
                            OnCustomDeviceDisconnected(deviceIndex);
                        }
                    }
                }

                ++deviceIndex;
            }

            if (pRightIndex != PrioritizedRightHandIndex || pLeftIndex != PrioritizedLeftHandIndex)
            {
                BeforeHandRoleChanged();
                InvokeControllerRoleChangedEvent();
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
            ProcessDeviceInputChanged();
        }

        public override void Update()
        {
            UpdateLockPhysicsUpdateRate();
            UpdateHapticVibration();
        }

        private uint uxrRightHandIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint uxrLeftHandIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint ctrlRightHandIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint ctrlLeftHandIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint trackedRightHandIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint trackedLeftHandIndex = VRModule.INVALID_DEVICE_INDEX;
        protected uint PrioritizedRightHandIndex { get { var len = GetDeviceStateLength(); return uxrRightHandIndex < len ? uxrRightHandIndex : ctrlRightHandIndex < len ? ctrlRightHandIndex : trackedRightHandIndex; } }
        protected uint PrioritizedLeftHandIndex { get { var len = GetDeviceStateLength(); return uxrLeftHandIndex < len ? uxrLeftHandIndex : ctrlLeftHandIndex < len ? ctrlLeftHandIndex : trackedLeftHandIndex; } }

        private void UpdateHandRoleForNewConnectedDevice(uint index, IVRModuleDeviceStateRW state)
        {
            var inputDevice = indexMap.Index2Device(index);
            var inputDeviceChar = inputDevice.characteristics;
            const InputDeviceCharacteristics handChar = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Left;

            if (inputDevice.isValid && (inputDeviceChar & handChar) > 0)
            {
                if ((inputDeviceChar & InputDeviceCharacteristics.Right) > 0)
                {
                    uxrRightHandIndex = index;
                }
                else
                {
                    uxrLeftHandIndex = index;
                }
            }
            else
            {
                switch (state.deviceClass)
                {
                    case VRModuleDeviceClass.Controller:
                        if (state.deviceModel.IsRight()) { ctrlRightHandIndex = index; }
                        else if (state.deviceModel.IsLeft()) { ctrlLeftHandIndex = index; }
                        break;
                    case VRModuleDeviceClass.TrackedHand:
                        if (state.deviceModel.IsRight()) { trackedRightHandIndex = index; }
                        else if (state.deviceModel.IsLeft()) { trackedLeftHandIndex = index; }
                        break;
                }
            }
        }

        private void UpdateHandRoleForDisconnectedDevice(uint index)
        {
            if (uxrRightHandIndex == index) { uxrRightHandIndex = VRModule.INVALID_DEVICE_INDEX; }
            if (uxrLeftHandIndex == index) { uxrLeftHandIndex = VRModule.INVALID_DEVICE_INDEX; }
            if (ctrlRightHandIndex == index) { ctrlRightHandIndex = VRModule.INVALID_DEVICE_INDEX; }
            if (ctrlLeftHandIndex == index) { ctrlLeftHandIndex = VRModule.INVALID_DEVICE_INDEX; }
            if (trackedRightHandIndex == index) { trackedRightHandIndex = VRModule.INVALID_DEVICE_INDEX; }
            if (trackedLeftHandIndex == index) { trackedLeftHandIndex = VRModule.INVALID_DEVICE_INDEX; }
        }

        public override uint GetRightControllerDeviceIndex() { return PrioritizedRightHandIndex; }

        public override uint GetLeftControllerDeviceIndex() { return PrioritizedLeftHandIndex; }

        private IndexMap indexMap = new IndexMap();
        private void UpdateInputDevices()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            uint deviceIndex;

            InputDevices.GetDevices(connectedDevices);
            foreach (var connectedDevice in connectedDevices)
            {
                if (!indexMap.TryGetIndex(connectedDevice, out deviceIndex))
                {
                    if (indexMap.MapAsHMD(connectedDevice))
                    {
                        deviceIndex = VRModule.HMD_DEVICE_INDEX;
                        EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                    }
                    else
                    {
                        // this function will skip VRModule.HMD_DEVICE_INDEX (preserved index for HMD)
                        deviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);
                        indexMap.MapNonHMD(connectedDevice, deviceIndex);
                    }

                    currState.isConnected = true;

                    UpdateNewConnectedInputDevice(currState, connectedDevice);
                }
                else
                {
                    EnsureValidDeviceState(deviceIndex, out prevState, out currState);

                    currState.isConnected = true;
                }

                UpdateInputDeviceTrackingState(currState, connectedDevice);

                if ((connectedDevice.characteristics & InputDeviceCharacteristics.Controller) > 0)
                {
                    UpdateInputDevicesControllerState(currState, connectedDevice);
                }
            }
        }

        // setup constent propert
        // setup deviceModel
        // setup LeftHandDeviceIndex/RightHandDeviceIndex
        protected virtual void UpdateNewConnectedInputDevice(IVRModuleDeviceStateRW state, InputDevice device)
        {
            state.deviceClass = GetDeviceClass(device.name, device.characteristics);
            state.serialNumber = device.name + " " + device.serialNumber + " " + (int)device.characteristics;
            state.modelNumber = device.name;
            state.renderModelName = device.name;

            SetupKnownDeviceModel(state);

            Debug.LogFormat("Device connected: {0} / {1} / {2} / {3} / {4} / {5} ({6})", state.deviceIndex, state.deviceClass, state.deviceModel, state.modelNumber, state.serialNumber, device.name, device.characteristics);
        }

        protected virtual void UpdateInputDeviceTrackingState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            if (!device.isValid) { Debug.LogError("[UpdateInputDeviceTrackingState] unable to update invalid device"); return; }

            state.isPoseValid = GetDeviceFeatureValueOrDefault(device, CommonUsages.isTracked);
            state.position = GetDeviceFeatureValueOrDefault(device, CommonUsages.devicePosition);
            state.rotation = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceRotation);
            state.velocity = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceVelocity);
            state.angularVelocity = GetDeviceFeatureValueOrDefault(device, CommonUsages.deviceAngularVelocity);
        }

        protected virtual void UpdateInputDevicesControllerState(IVRModuleDeviceStateRW state, InputDevice device) { }

        // note: LeftHandDeviceIndex/RightHandDeviceIndex is not reliable in this stage
        // get/set HandDeviceIndex in BeforeHandRoleChanged stage
        private void OnInputDeviceDisconnected(uint index)
        {
            indexMap.UnmapByIndex(index);
        }

        protected virtual void UpdateCustomDevices()
        {
            // sudo code:
            // foreach devices
            //   if is new incomming device
            //     use FindOrAllocateUnusedIndex() to get valid index and assign to this device
            //
            //     use EnsureValidDeviceState to get deviceState by index
            //     set deviceState.Connected = true;
            //
            //   if is already connected device
            //     get index previously associated with the device
            //
            //     use EnsureValidDeviceState to get deviceState by index
            //     set deviceState.Connected = true;
            //
            //   update deviceState tracking data
            //   if has input, update deviceState input data
        }

        // note: LeftHandDeviceIndex/RightHandDeviceIndex is not reliable in this stage
        // get/set HandDeviceIndex in BeforeHandRoleChanged stage
        protected virtual void OnCustomDeviceDisconnected(uint index)
        {

        }

        // Incase prev hand is disconnected, and want to fallback to other controller
        protected virtual void BeforeHandRoleChanged() { }

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
                for (uint i = 0, imax = maxHapticStateIndex; i < imax; ++i)
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
