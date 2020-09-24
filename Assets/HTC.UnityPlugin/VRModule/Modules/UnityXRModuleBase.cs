//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
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
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.OpenVR, matchNameRgx = new Regex("openvr", RegexOptions.IgnoreCase) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.Oculus, matchNameRgx = new Regex("oculus", RegexOptions.IgnoreCase) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.WindowsXR, matchNameRgx = new Regex("windows", RegexOptions.IgnoreCase) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.MagicLeap, matchNameRgx = new Regex("magicleap", RegexOptions.IgnoreCase) },
            new XRLoaderProfile() { loader = VRModuleKnownXRLoader.WaveXR, matchNameRgx = new Regex("wave", RegexOptions.IgnoreCase) },
        };

        private static List<XRInputSubsystemProfile> inputSubsystemProfiles = new List<XRInputSubsystemProfile>()
        {
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.OpenVR, matchNameRgx = new Regex("openvr", RegexOptions.IgnoreCase) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.Oculus, matchNameRgx = new Regex("oculus", RegexOptions.IgnoreCase) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.WindowsXR, matchNameRgx = new Regex("windows", RegexOptions.IgnoreCase) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.MagicLeap, matchNameRgx = new Regex("magicleap", RegexOptions.IgnoreCase) },
            new XRInputSubsystemProfile() { subsystem = VRModuleKnownXRInputSubsystem.WaveXR, matchNameRgx = new Regex("wave", RegexOptions.IgnoreCase) },
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
            ResetIndexedInputDevices();
        }

        public override void UpdateTrackingSpaceType()
        {
            var originFlag = default(TrackingOriginModeFlags);
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.Stationary: originFlag = TrackingOriginModeFlags.Device; break;
                case VRModuleTrackingSpaceType.RoomScale: originFlag = TrackingOriginModeFlags.Floor; break;
                default: return;
            }
            SetAllXRInputSubsystemTrackingOriginMode(originFlag);
        }

        protected uint FindOrAllocateUnusedIndex()
        {
            uint index;
            if (!FindFirstUnusedIndex(out index))
            {
                index = GetDeviceStateLength();
                EnsureDeviceStateLength(index + 1u);
            }
            return index;
        }

        protected bool FindFirstUnusedIndex(out uint index)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            var i = 0u;
            while (TryGetValidDeviceState(i, out prevState, out currState))
            {
                if (!prevState.isConnected && !currState.isConnected)
                {
                    index = i;
                    return true;
                }
                ++i;
            }
            index = default(uint);
            return false;
        }

        private List<InputDevice> conntectedDevices = new List<InputDevice>();
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

                        if (GetInputDeviceByIndex(deviceIndex).isValid)
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

            if (IsHandRoleIndexDirty())
            {
                BeforeHandRoleChanged();
                ResetHandRoleIndexDirty();
                InvokeControllerRoleChangedEvent();
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
            ProcessDeviceInputChanged();
        }

        private bool handRoleIndexDirty;
        private uint uxrRightHandIndex;
        private uint uxrLeftHandIndex;
        private uint ctrlRightHandIndex;
        private uint ctrlLeftHandIndex;
        private uint trackedRightHandIndex;
        private uint trackedLeftHandIndex;
        private bool IsHandRoleIndexDirty() { return handRoleIndexDirty; }
        private void ResetHandRoleIndexDirty() { handRoleIndexDirty = false; }

        private void UpdateHandRoleForNewConnectedDevice(uint index, IVRModuleDeviceStateRW state)
        {
            var inputDevice = GetInputDeviceByIndex(index);
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
                handRoleIndexDirty = true;
            }
            else
            {
                switch (state.deviceClass)
                {
                    case VRModuleDeviceClass.Controller:
                        if (state.deviceModel.IsRight())
                        {
                            ctrlRightHandIndex = index;
                            handRoleIndexDirty = true;
                        }
                        else if (state.deviceModel.IsLeft())
                        {
                            ctrlLeftHandIndex = index;
                            handRoleIndexDirty = true;
                        }
                        break;
                    case VRModuleDeviceClass.TrackedHand:
                        if (state.deviceModel.IsRight())
                        {
                            trackedRightHandIndex = index;
                            handRoleIndexDirty = true;
                        }
                        else if (state.deviceModel.IsLeft())
                        {
                            trackedLeftHandIndex = index;
                            handRoleIndexDirty = true;
                        }
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

        public override uint GetRightControllerDeviceIndex()
        {
            return VRModule.IsValidDeviceIndex(uxrRightHandIndex) ? uxrRightHandIndex : VRModule.IsValidDeviceIndex(ctrlRightHandIndex) ? ctrlRightHandIndex : trackedRightHandIndex;
        }

        public override uint GetLeftControllerDeviceIndex()
        {
            return VRModule.IsValidDeviceIndex(uxrLeftHandIndex) ? uxrLeftHandIndex : VRModule.IsValidDeviceIndex(ctrlLeftHandIndex) ? ctrlLeftHandIndex : trackedLeftHandIndex;
        }

        protected InputDevice GetInputDeviceByIndex(uint index)
        {
            return index < (uint)indexedInputDevices.Count ? indexedInputDevices[(int)index] : default(InputDevice);
        }

        private void ResetIndexedInputDevices()
        {
            indexForInputDevices.Clear();
            indexedInputDevices.Clear();
        }

        private Dictionary<int, uint> indexForInputDevices = new Dictionary<int, uint>();
        private List<InputDevice> indexedInputDevices = new List<InputDevice>();
        private void UpdateInputDevices()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            uint deviceIndex;

            InputDevices.GetDevices(conntectedDevices);
            foreach (var connectedDevice in conntectedDevices)
            {
                var deviceID = GetInputDeviceInternalID(connectedDevice);
                if (!indexForInputDevices.TryGetValue(deviceID, out deviceIndex))
                {
                    deviceIndex = FindOrAllocateUnusedIndex();
                    // assign the index to the new connected device
                    indexForInputDevices.Add(deviceID, deviceIndex);
                    while (deviceIndex >= indexedInputDevices.Count) { indexedInputDevices.Add(default(InputDevice)); }
                    indexedInputDevices[(int)deviceIndex] = connectedDevice;

                    EnsureValidDeviceState(deviceIndex, out prevState, out currState); Debug.Assert(!prevState.isConnected);
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
            if (index < indexedInputDevices.Count && indexedInputDevices[(int)index].isValid)
            {
                indexForInputDevices.Remove(GetInputDeviceInternalID(indexedInputDevices[(int)index]));
                indexedInputDevices[(int)index] = default(InputDevice);
            }
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

        private static int GetInputDeviceInternalID(InputDevice device)
        {
#if CSHARP_7_OR_LATER
            return (device, device.name, device.characteristics).GetHashCode();
#else
            return new { device, device.name, device.characteristics }.GetHashCode();
#endif
        }

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
            finally
            {
                ListPool<XRInputSubsystem>.Release(activeSubsys);
            }
        }
#endif
    }
}