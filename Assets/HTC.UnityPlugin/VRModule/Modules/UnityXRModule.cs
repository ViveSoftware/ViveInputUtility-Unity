//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;
using System;
#if VIU_WAVEXR_ESSENCE_RENDERMODEL
using Wave.Essence;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public enum XRInputSubsystemType
    {
        Unknown,
        OpenVR,
        Oculus,
        WMR,
        MagicLeap,
    }

    public sealed class UnityXRModule : VRModule.ModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.UnityXR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.UnityXR; } }

        public const string WAVE_XR_LOADER_NAME = "Wave XR Loader";
        public const string WAVE_XR_LOADER_CLASS_NAME = "WaveXRLoader";

#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance != null && s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
                if (hook.GetComponent<TrackedPoseDriver>() == null)
                {
                    hook.gameObject.AddComponent<TrackedPoseDriver>();
                }
            }
        }

        [RenderModelHook.CreatorPriorityAttirbute(0)]
        private class RenderModelCreator : RenderModelHook.DefaultRenderModelCreator
        {
            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void UpdateRenderModel()
            {
#if VIU_WAVEXR_ESSENCE_RENDERMODEL
                if (HasActiveLoader(WAVE_XR_LOADER_NAME))
                {
                    if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }
                    if (VRModule.IsValidDeviceIndex(m_index) && m_index == VRModule.GetRightControllerDeviceIndex())
                    {
                        var go = new GameObject("Model");
                        go.transform.SetParent(hook.transform, false);
                        go.AddComponent<Wave.Essence.Controller.RenderModel>();
                        go.AddComponent<Wave.Essence.Controller.ButtonEffect>();
                        go.AddComponent<Wave.Essence.Controller.ShowIndicator>();
                    }
                    else if (VRModule.IsValidDeviceIndex(m_index) && m_index == VRModule.GetLeftControllerDeviceIndex())
                    {
                        var go = new GameObject("Model");
                        go.transform.SetParent(hook.transform, false);
                        var rm = go.AddComponent<Wave.Essence.Controller.RenderModel>();
                        rm.transform.gameObject.SetActive(false);
                        rm.WhichHand = XR_Hand.NonDominant;
                        rm.transform.gameObject.SetActive(true);
                        var be = go.AddComponent<Wave.Essence.Controller.ButtonEffect>();
                        be.transform.gameObject.SetActive(false);
                        be.HandType = XR_Hand.NonDominant;
                        be.transform.gameObject.SetActive(true);
                        go.AddComponent<Wave.Essence.Controller.ShowIndicator>();
                    }
                    else
                    {
                        // deacitvate object for render model
                        if (m_model != null)
                        {
                            m_model.gameObject.SetActive(false);
                        }
                    }
                }
                else
#endif
                {
                    base.UpdateRenderModel();
                }
            }
        }

        private class HapticVibrationState
        {
            public uint deviceIndex;
            public float amplitude;
            public float remainingDuration;
            public float remainingDelay;

            public HapticVibrationState(uint index, float amp, float duration, float delay)
            {
                deviceIndex = index;
                amplitude = amp;
                remainingDuration = duration;
                remainingDelay = delay;
            }
        }

        private const uint DEVICE_STATE_LENGTH = 16;
        private static UnityXRModule s_moduleInstance;

        private XRInputSubsystemType m_currentInputSubsystemType = XRInputSubsystemType.Unknown;
        private uint m_rightHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private uint m_leftHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private Dictionary<int, uint> m_deviceUidToIndex = new Dictionary<int, uint>();
        private List<InputDevice> m_indexToDevices = new List<InputDevice>();
        private List<InputDevice> m_connectedDevices = new List<InputDevice>();
        private List<HapticVibrationState> m_activeHapticVibrationStates = new List<HapticVibrationState>();

        public static bool HasActiveLoader(string loaderName = null)
        {
            var instance = XRGeneralSettings.Instance;
            if (instance == null) { return false; }
            var manager = instance.Manager;
            if (manager == null) { return false; }
            var loader = manager.activeLoader;
            if (loader == null) { return false; }
            if (loaderName != null && loaderName != loader.name) { return false; }
            return true;
        }

        public override bool ShouldActiveModule()
        {
            return VIUSettings.activateUnityXRModule && HasActiveLoader();
        }

        public override void OnActivated()
        {
            s_moduleInstance = this;
            m_currentInputSubsystemType = DetectCurrentInputSubsystemType();
            EnsureDeviceStateLength(DEVICE_STATE_LENGTH);
            Debug.Log("Activated XRLoader Name: " + XRGeneralSettings.Instance.Manager.activeLoader.name);
        }

        public override void OnDeactivated()
        {
            s_moduleInstance = null;
            m_deviceUidToIndex.Clear();
            m_indexToDevices.Clear();
            m_connectedDevices.Clear();
        }

        public override uint GetLeftControllerDeviceIndex() { return m_leftHandedDeviceIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_rightHandedDeviceIndex; }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.Stationary:
                    SetAllXRInputSubsystemTrackingOriginMode(TrackingOriginModeFlags.Device);
                    break;
                case VRModuleTrackingSpaceType.RoomScale:
                    SetAllXRInputSubsystemTrackingOriginMode(TrackingOriginModeFlags.Floor);
                    break;
            }
        }

        // NOTE: Frequency not supported
        public override void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85.0f, float amplitude = 0.125f, float startSecondsFromNow = 0.0f)
        {
            InputDevice device;
            if (TryGetDevice(deviceIndex, out device))
            {
                if (!device.isValid)
                {
                    return;
                }

                HapticCapabilities capabilities;
                if (device.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        for (int i = m_activeHapticVibrationStates.Count - 1; i >= 0; i--)
                        {
                            if (m_activeHapticVibrationStates[i].deviceIndex == deviceIndex)
                            {
                                m_activeHapticVibrationStates.RemoveAt(i);
                            }
                        }

                        m_activeHapticVibrationStates.Add(new HapticVibrationState(deviceIndex, amplitude, durationSeconds, startSecondsFromNow));
                    }
                }
            }
        }

        public override void Update()
        {
            UpdateLockPhysicsUpdateRate();
            UpdateHapticVibration();
        }

        public override void BeforeRenderUpdate()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            uint deviceIndex;

            FlushDeviceState();

            // mark all devices as disconnected
            deviceIndex = 0u;
            while (TryGetValidDeviceState(deviceIndex++, out prevState, out currState))
            {
                currState.isConnected = false;
            }

            bool roleChanged = false;
            InputDevices.GetDevices(m_connectedDevices);
            foreach (InputDevice device in m_connectedDevices)
            {
                deviceIndex = GetOrCreateDeviceIndex(device);
                EnsureValidDeviceState(deviceIndex, out prevState, out currState);

                if (!prevState.isConnected)
                {
                    currState.deviceClass = GetDeviceClass(device.name, device.characteristics);
                    currState.serialNumber = device.name + " " + device.serialNumber + " " + (int)device.characteristics;
                    currState.modelNumber = device.name;
                    currState.renderModelName = device.name;

                    SetupKnownDeviceModel(currState);

                    if ((device.characteristics & InputDeviceCharacteristics.Left) > 0u)
                    {
                        m_leftHandedDeviceIndex = deviceIndex;
                        roleChanged = true;
                    }
                    else if ((device.characteristics & InputDeviceCharacteristics.Right) > 0u)
                    {
                        m_rightHandedDeviceIndex = deviceIndex;
                        roleChanged = true;
                    }

                    Debug.LogFormat("Device connected: {0} / {1} / {2} / {3} / {4} / {5} ({6})", deviceIndex, currState.deviceClass, currState.deviceModel, currState.modelNumber, currState.serialNumber, device.name, device.characteristics);
                }

                bool isTracked = false;
                device.TryGetFeatureValue(CommonUsages.isTracked, out isTracked);
                currState.isPoseValid = device.isValid && isTracked;
                currState.isConnected = true;

                UpdateTrackingState(currState, device);
                if (currState.deviceClass == VRModuleDeviceClass.Controller || currState.deviceModel == VRModuleDeviceModel.ViveTracker)
                {
                    UpdateControllerState(currState, device);
                }
            }

            //UpdateHandHeldDeviceIndex();

            deviceIndex = 0u;
            // reset all devices that is not connected in this frame
            while (TryGetValidDeviceState(deviceIndex, out prevState, out currState))
            {
                if (!currState.isConnected)
                {
                    currState.Reset();

                    if (deviceIndex == m_leftHandedDeviceIndex)
                    {
                        m_leftHandedDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
                        roleChanged = true;
                    }
                    else if (deviceIndex == m_rightHandedDeviceIndex)
                    {
                        m_rightHandedDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
                        roleChanged = true;
                    }
                }

                ++deviceIndex;
            }

            if (roleChanged) { InvokeControllerRoleChangedEvent(); }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
            ProcessDeviceInputChanged();
        }

        private void UpdateLockPhysicsUpdateRate()
        {
            if (VRModule.lockPhysicsUpdateRateToRenderFrequency && Time.timeScale > 0.0f)
            {
                List<XRDisplaySubsystem> displaySystems = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySystems);

                float minRefreshRate = float.MaxValue;
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
        }

        private void UpdateHapticVibration()
        {
            for (int i = m_activeHapticVibrationStates.Count - 1; i >= 0; i--)
            {
                HapticVibrationState state = m_activeHapticVibrationStates[i];
                if (state.remainingDelay > 0.0f)
                {
                    state.remainingDelay -= Time.deltaTime;
                    continue;
                }

                InputDevice device;
                if (TryGetDevice(state.deviceIndex, out device))
                {
                    if (device.isValid)
                    {
                        device.SendHapticImpulse(0, state.amplitude);
                    }
                }

                state.remainingDuration -= Time.deltaTime;
                if (state.remainingDuration <= 0)
                {
                    m_activeHapticVibrationStates.RemoveAt(i);
                }
            }
        }

        private void UpdateTrackingState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            Vector3 position = Vector3.zero;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
            {
                state.position = position;
            }

            Quaternion rotation = Quaternion.identity;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                state.rotation = rotation;
            }

            Vector3 velocity = Vector3.zero;
            if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
            {
                state.velocity = velocity;
            }

            Vector3 angularVelocity = Vector3.zero;
            if (device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out angularVelocity))
            {
                state.angularVelocity = angularVelocity;
            }
        }

        private void UpdateControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            switch (state.deviceModel)
            {
                case VRModuleDeviceModel.ViveController:
                    UpdateViveControllerState(state, device);
                    break;
                case VRModuleDeviceModel.ViveCosmosControllerLeft:
                case VRModuleDeviceModel.ViveCosmosControllerRight:
                    UpdateViveCosmosControllerState(state, device);
                    break;
                case VRModuleDeviceModel.ViveTracker:
                    UpdateViveTrackerState(state, device);
                    break;
                case VRModuleDeviceModel.OculusTouchLeft:
                case VRModuleDeviceModel.OculusTouchRight:
                case VRModuleDeviceModel.OculusGoController:
                case VRModuleDeviceModel.OculusQuestControllerLeft:
                case VRModuleDeviceModel.OculusQuestControllerRight:
                    UpdateOculusControllerState(state, device);
                    break;
                case VRModuleDeviceModel.WMRControllerLeft:
                case VRModuleDeviceModel.WMRControllerRight:
                    UpdateWMRControllerState(state, device);
                    break;
                case VRModuleDeviceModel.KnucklesLeft:
                case VRModuleDeviceModel.KnucklesRight:
                case VRModuleDeviceModel.IndexControllerLeft:
                case VRModuleDeviceModel.IndexControllerRight:
                    UpdateIndexControllerState(state, device);
                    break;
                case VRModuleDeviceModel.MagicLeapController:
                    UpdateMagicLeapControllerState(state, device);
                    break;
                case VRModuleDeviceModel.ViveFocusChirp:
                    UpdateViveFocusChirpControllerState(state, device);
                    break;
                case VRModuleDeviceModel.ViveFocusFinch:
                    UpdateViveFocusFinchControllerState(state, device);
                    break;
            }
        }

        private bool TryGetDevice(uint index, out InputDevice deviceOut)
        {
            deviceOut = default;
            if (index < m_indexToDevices.Count)
            {
                deviceOut = m_indexToDevices[(int)index];
                return true;
            }

            return false;
        }

        private uint GetOrCreateDeviceIndex(InputDevice device)
        {
            uint index = 0;
            int uid = GetDeviceUID(device);
            if (m_deviceUidToIndex.TryGetValue(uid, out index))
            {
                return index;
            }

            uint newIndex = (uint)m_deviceUidToIndex.Count;
            m_deviceUidToIndex.Add(uid, newIndex);
            m_indexToDevices.Add(device);

            return newIndex;
        }

        private VRModuleDeviceClass GetDeviceClass(string name, InputDeviceCharacteristics characteristics)
        {
            bool isTracker = Regex.IsMatch(name, @"tracker", RegexOptions.IgnoreCase);
            if ((characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
            {
                return VRModuleDeviceClass.HMD;
            }

            if ((characteristics & InputDeviceCharacteristics.Controller) != 0 && !isTracker)
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

        private void SetAllXRInputSubsystemTrackingOriginMode(TrackingOriginModeFlags mode)
        {
            List<XRInputSubsystem> systems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(systems);
            foreach (XRInputSubsystem system in systems)
            {
                if (!system.TrySetTrackingOriginMode(mode))
                {
                    Debug.LogWarning("Failed to set TrackingOriginModeFlags to XRInputSubsystem: " + system.SubsystemDescriptor.id);
                }
            }
        }

        private int GetDeviceUID(InputDevice device)
        {
#if CSHARP_7_OR_LATER
            return (device.name, device.serialNumber, device.characteristics).GetHashCode();
#else
            return new { device.name, device.serialNumber, device.characteristics }.GetHashCode();
#endif
        }

        private XRInputSubsystemType DetectCurrentInputSubsystemType()
        {
            List<XRInputSubsystem> systems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(systems);
            if (systems.Count == 0)
            {
                Debug.LogWarning("No XRInputSubsystem detected.");
                return XRInputSubsystemType.Unknown;
            }

            string id = systems[0].SubsystemDescriptor.id;
            Debug.Log("Activated XRInputSubsystem Name: " + id);

            if (Regex.IsMatch(id, @"openvr", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.OpenVR;
            }
            else if (Regex.IsMatch(id, @"oculus", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.Oculus;
            }
            else if (Regex.IsMatch(id, @"windows mixed reality", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.WMR;
            }
            else if (Regex.IsMatch(id, @"magicleap", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.MagicLeap;
            }

            return XRInputSubsystemType.Unknown;
        }

        private void UpdateViveControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool primaryAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primaryAxisClick);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);

            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateViveCosmosControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton); // X/A
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryTouch); // X/A

            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // Y/B
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryTouch); // Y/B

            bool primaryAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);

            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));

            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool bumperButton = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("BumperButton"));

            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primaryAxisClick);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Bumper, bumperButton);

            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            state.SetButtonTouch(VRModuleRawButton.Grip, gripButton);
            state.SetButtonTouch(VRModuleRawButton.Bumper, bumperButton);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateViveTrackerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);

            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateOculusControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton); // X/A
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // Y/B
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryTouch); // X/A
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryTouch); // Y/B
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Joystick
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Joystick
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Joystick

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Axis0, primary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
            state.SetButtonTouch(VRModuleRawButton.Grip, grip >= 0.05f);
            state.SetButtonTouch(VRModuleRawButton.CapSenseGrip, grip >= 0.05f);
            state.SetButtonTouch(VRModuleRawButton.Axis0, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            state.SetAxisValue(VRModuleRawAxis.JoystickX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.JoystickY, primary2DAxis.y);

            if (m_currentInputSubsystemType == XRInputSubsystemType.OpenVR)
            {
                bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));
                state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            }
            else if (m_currentInputSubsystemType == XRInputSubsystemType.Oculus)
            {
                bool thumbrest = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Thumbrest"));
                float indexTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("IndexTouch"));
                float thumbTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("ThumbTouch")); // Not in use

                state.SetButtonTouch(VRModuleRawButton.Touchpad, thumbrest);
                state.SetButtonTouch(VRModuleRawButton.Trigger, indexTouch >= 1.0f);

                if ((device.characteristics & InputDeviceCharacteristics.Left) != 0)
                {
                    bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
                    state.SetButtonPress(VRModuleRawButton.System, menuButton);
                }
            }
        }

        private void UpdateWMRControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Touchpad
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick"));
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Touchpad
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // Joystick

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Axis0, secondary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.JoystickX, secondary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.JoystickY, secondary2DAxis.y);

            if (m_currentInputSubsystemType == XRInputSubsystemType.WMR)
            {
                float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);
                float sourceLossRisk = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("SourceLossRisk")); // Not in use
                Vector3 pointerPosition = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector3>("PointerPosition")); // Not in use
                Vector3 sourceMitigationDirection = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector3>("SourceMitigationDirection")); // Not in use
                Quaternion pointerRotation = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Quaternion>("PointerRotation")); // Not in use

                // conflict with JoystickX
                //state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            }
        }

        private void UpdateIndexControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            // TODO: Get finger curl values once OpenVR XR Plugin supports
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // B
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick")); // Joystick
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton); // trigger >= 0.5
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton); // grip force >= 0.5
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("PrimaryTouch"));
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("SecondaryTouch"));
            bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));
            bool gripTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("GripTouch"));
            bool gripGrab = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("GripGrab")); // gripCapacitive >= 0.7
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // Joystick
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip); // grip force
            float gripCapacitive = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("GripCapacitive")); // touch area on grip
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Axis0, secondary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            state.SetButtonTouch(VRModuleRawButton.Grip, gripTouch);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
            state.SetButtonTouch(VRModuleRawButton.Axis0, secondary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.JoystickX, secondary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.JoystickY, secondary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            // conflict with JoystickX
            //state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
        }

        private void UpdateMagicLeapControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // Bumper
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            uint MLControllerType = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<uint>("MLControllerType")); // Not in use
            uint MLControllerDOF = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<uint>("MLControllerDOF")); // Not in use
            uint MLControllerCalibrationAccuracy = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<uint>("MLControllerCalibrationAccuracy")); // Not in use
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            float MLControllerTouch1Force = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("MLControllerTouch1Force")); // Not in use
            float MLControllerTouch2Force = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("MLControllerTouch2Force")); // Not in use
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // Not in use

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Bumper, secondaryButton);

            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateViveFocusChirpControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Touchpad
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Touchpad
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick")); // No data
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // No data
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // No data
            Vector2 dPad = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector2>("DPad"));

            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.DPadUp, dPad.y > 0);
            state.SetButtonPress(VRModuleRawButton.DPadDown, dPad.y < 0);
            state.SetButtonPress(VRModuleRawButton.DPadLeft, dPad.x < 0);
            state.SetButtonPress(VRModuleRawButton.DPadRight, dPad.x > 0);

            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateViveFocusFinchControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Touchpad
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Touchpad
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick")); // No data
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // No data
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton); // Trigger
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton); // No Data
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger); // No Data
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // No data
            Vector2 dPad = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector2>("DPad")); // No Data

            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Trigger, gripButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.DPadUp, dPad.y > 0);
            state.SetButtonPress(VRModuleRawButton.DPadDown, dPad.y < 0);
            state.SetButtonPress(VRModuleRawButton.DPadLeft, dPad.x < 0);
            state.SetButtonPress(VRModuleRawButton.DPadRight, dPad.x > 0);

            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private bool GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<bool> feature)
        {
            bool value = false;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have bool feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private uint GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<uint> feature)
        {
            uint value = 0;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have uint feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private float GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<float> feature)
        {
            float value = 0.0f;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have float feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private Vector2 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector2> feature)
        {
            Vector2 value = Vector2.zero;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have Vector2 feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private Vector3 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector3> feature)
        {
            Vector3 value = Vector3.zero;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have Vector3 feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature)
        {
            Quaternion value = Quaternion.identity;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have Quaternion feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private Hand GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Hand> feature)
        {
            Hand value;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have Hand feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private Bone GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Bone> feature)
        {
            Bone value;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have Bone feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private Eyes GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Eyes> feature)
        {
            Eyes value;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have Eyes feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private InputTrackingState GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<InputTrackingState> feature)
        {
            InputTrackingState value;
            if (device.TryGetFeatureValue(feature, out value))
            {
                return value;
            }

#if UNITY_EDITOR
            Debug.LogWarningFormat("Device {0} doesn't have InputTrackingState feature {1}. Return default value instead.", device.name, feature.name);
#endif

            return default;
        }

        private void LogDeviceFeatureUsages(InputDevice device)
        {
            List<InputFeatureUsage> usages = new List<InputFeatureUsage>();
            if (device.TryGetFeatureUsages(usages))
            {
                string strUsages = "";
                foreach (var usage in usages)
                {
                    strUsages += "[" + usage.type.Name + "] " + usage.name + "\n";
                }

                Debug.Log(device.name + " feature usages:\n\n" + strUsages);
            }
        }

        private static string CharacteristicsToString(InputDeviceCharacteristics ch)
        {
            if (ch == 0u) { return " No Characteristic"; }
            var chu = (uint)ch;
            var str = string.Empty;
            for (var i = 1u; chu > 0u; i <<= 1)
            {
                if ((chu & i) == 0u) { continue; }
                str += " " + (InputDeviceCharacteristics)i;
                chu &= ~i;
            }
            return str;
        }
#endif
    }
}