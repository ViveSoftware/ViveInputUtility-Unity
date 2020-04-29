//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

#if VIU_XR_PLUGIN_MANAGEMENT
using UnityEngine.XR.Management;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public enum XRInputSubsystemType
    {
        Unknown,
        OpenVR,
        Oculus,
        WMR,
    }

    public sealed class UnityXRPluginManagementVRModule : VRModule.ModuleBase
    {
        public override int moduleIndex { get { return (int)VRModuleActiveEnum.UnityXRPluginManagement; } }

#if VIU_XR_PLUGIN_MANAGEMENT
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance != null && s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
                if (hook.GetComponent<VivePoseTracker>() == null)
                {
                    VivePoseTracker poseTracker = hook.gameObject.AddComponent<VivePoseTracker>();
                    poseTracker.viveRole.SetEx(DeviceRole.Hmd);
                }
            }
        }

        private const uint DEVICE_STATE_LENGTH = 16;
        private static UnityXRPluginManagementVRModule s_moduleInstance;

        private XRInputSubsystemType m_currentInputSubsystemType = XRInputSubsystemType.Unknown;
        private uint m_rightHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private uint m_leftHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private Dictionary<string, uint> m_deviceSerialToIndex = new Dictionary<string, uint>();
        private List<InputDevice> m_indexToDevices = new List<InputDevice>();
        private List<InputDevice> m_connectedDevices = new List<InputDevice>();

        [MenuItem("ZZZ/Vibration")]
        public static void Test()
        {
            VRModule.TriggerHapticVibration(2, 2.0f);
        }

        public override bool ShouldActiveModule()
        {
            return VIUSettings.activateUnityNativeVRModule && XRGeneralSettings.Instance.InitManagerOnStart;
        }

        public override void OnActivated()
        {
            s_moduleInstance = this;
            m_currentInputSubsystemType = DetectCurrentInputSubsystemType();
            EnsureDeviceStateLength(DEVICE_STATE_LENGTH);

            Debug.Log("Detected XRInputSubsystemType: " + m_currentInputSubsystemType);
        }

        public override void OnDeactivated()
        {
            s_moduleInstance = null;
            m_deviceSerialToIndex.Clear();
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

        public override void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85.0f, float amplitude = 0.125f, float startSecondsFromNow = 0.0f)
        {
            if (TryGetDevice(deviceIndex, out InputDevice device))
            {
                if (!device.isValid)
                {
                    return;
                }

                if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities))
                {
                    if (capabilities.supportsBuffer)
                    {
                        // TODO: Frequency settings
                    }

                    if (capabilities.supportsImpulse)
                    {
                        device.SendHapticImpulse(0, amplitude, durationSeconds);
                    }
                }
            }
        }

        public override void Update()
        {
            UpdateLockPhysicsUpdateRate();
        }

        public override void BeforeRenderUpdate()
        {
            FlushDeviceState();

            InputDevices.GetDevices(m_connectedDevices);
            foreach (InputDevice device in m_connectedDevices)
            {
                uint deviceIndex = GetOrCreateDeviceIndex(device);

                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                EnsureValidDeviceState(deviceIndex, out prevState, out currState);

                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = GetDeviceClass(device.characteristics);
                    currState.serialNumber = GetDeviceSerial(device);
                    currState.modelNumber = device.name;
                    currState.renderModelName = device.name;

                    SetupKnownDeviceModel(currState);

                    Debug.LogFormat("Device connected: {0} / {1} / {2} / {3} / {4} ({5})", deviceIndex, currState.deviceClass, currState.deviceModel, currState.modelNumber, currState.serialNumber, device.characteristics);

                    // Debug
                    LogDeviceFeatureUsages(device);
                }

                bool isTracked = GetDeviceFeatureValueOrDefault(device, CommonUsages.isTracked);
                currState.isPoseValid = device.isValid && isTracked;

                UpdateTrackingState(currState, device);
                if (currState.deviceClass == VRModuleDeviceClass.Controller)
                {
                    UpdateControllerState(currState, device);
                }
            }
            UpdateHandHeldDeviceIndex();

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
                    if (system.TryGetDisplayRefreshRate(out float rate))
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

        private void UpdateTrackingState(IVRModuleDeviceStateRW state, InputDevice device)
        {

            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                state.position = position;
            }

            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                state.rotation = rotation;
            }

            if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 velocity))
            {
                state.velocity = velocity;
            }

            if (device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 angularVelocity))
            {
                state.angularVelocity = angularVelocity;
            }
        }

        private void UpdateControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            switch (state.deviceModel)
            {
                case VRModuleDeviceModel.ViveController:
                case VRModuleDeviceModel.ViveCosmosControllerLeft:
                case VRModuleDeviceModel.ViveCosmosControllerRight:
                    UpdateViveControllerState(state, device);
                    break;
                case VRModuleDeviceModel.OculusTouchLeft:
                case VRModuleDeviceModel.OculusTouchRight:
                case VRModuleDeviceModel.OculusGoController:
                case VRModuleDeviceModel.OculusQuestControllerLeft:
                case VRModuleDeviceModel.OculusQuestControllerRight:
                    UpdateOculusControllerState(state, device);
                    break;
                case VRModuleDeviceModel.OculusGearVrController:
                    UpdateGearVRControllerState(state, device);
                    break;
                case VRModuleDeviceModel.DaydreamController:
                    UpdateDaydreamControllerState(state, device);
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
            }
        }

        private void UpdateHandHeldDeviceIndex()
        {
            uint leftHandedDeviceIndex = INVALID_DEVICE_INDEX;
            InputDevice leftHandedDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftHandedDevice.isValid)
            {
                leftHandedDeviceIndex = GetDeviceIndex(leftHandedDevice.serialNumber);
            }

            uint rightHandedDeviceIndex = INVALID_DEVICE_INDEX;
            InputDevice rightHandedDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightHandedDevice.isValid)
            {
                rightHandedDeviceIndex = GetDeviceIndex(rightHandedDevice.serialNumber);
            }

            if (m_rightHandedDeviceIndex != rightHandedDeviceIndex || m_leftHandedDeviceIndex != leftHandedDeviceIndex)
            {
                InvokeControllerRoleChangedEvent();
            }

            m_leftHandedDeviceIndex = leftHandedDeviceIndex;
            m_rightHandedDeviceIndex = rightHandedDeviceIndex;
        }

        private uint GetDeviceIndex(string serial)
        {
            if (m_deviceSerialToIndex.TryGetValue(serial, out uint index))
            {
                return index;
            }

            return INVALID_DEVICE_INDEX;
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
            string serial = GetDeviceSerial(device);
            if (m_deviceSerialToIndex.TryGetValue(serial, out uint index))
            {
                return index;
            }
            
            uint newIndex = (uint)m_deviceSerialToIndex.Count;
            m_deviceSerialToIndex.Add(serial, newIndex);
            m_indexToDevices.Add(device);

            return newIndex;
        }

        private VRModuleDeviceClass GetDeviceClass(InputDeviceCharacteristics characteristics)
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

        private string GetDeviceSerial(InputDevice device)
        {
            if (!string.IsNullOrEmpty(device.serialNumber))
            {
                return device.serialNumber;
            }

            return device.name;
        }

        private XRInputSubsystemType DetectCurrentInputSubsystemType()
        {
            List<XRInputSubsystem> systems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(systems);
            if (systems.Count == 0)
            {
                return XRInputSubsystemType.Unknown;
            }

            string id = systems[0].SubsystemDescriptor.id;
            Debug.Log("Activated XRInputSubsystem: " + id);

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
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, -primary2DAxis.y);
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
            state.SetAxisValue(VRModuleRawAxis.JoystickY, -primary2DAxis.y);

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

        private void UpdateGearVRControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {

        }

        private void UpdateDaydreamControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {

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
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis);

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Axis0, secondary2DAxisClick);
            
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, -primary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.JoystickX, secondary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.JoystickY, -secondary2DAxis.y);

            if (m_currentInputSubsystemType == XRInputSubsystemType.WMR)
            {
                float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);
                float sourceLossRisk = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("SourceLossRisk")); // Not in use
                Vector3 pointerPosition = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector3>("PointerPosition")); // Not in use
                Vector3 sourceMitigationDirection = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector3>("SourceMitigationDirection")); // Not in use
                Quaternion pointerRotation = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Quaternion>("PointerRotation")); // Not in use

                state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            }
        }

        private void UpdateIndexControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            // TODO: Get finger curl values once OpenVR XR Plugin supports
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // B
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick")); // Joystick
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("PrimaryTouch"));
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("SecondaryTouch"));
            bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));
            bool gripGrab = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("GripGrab"));
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // Joystick
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);
            float gripCapacitive = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("GripCapacitive")); // Not in use
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Axis0, secondary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            state.SetButtonTouch(VRModuleRawButton.Grip, gripGrab);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
            state.SetButtonTouch(VRModuleRawButton.Axis0, secondary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, -primary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
        }

        private bool GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<bool> feature)
        {
            if (device.TryGetFeatureValue(feature, out bool value))
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
            if (device.TryGetFeatureValue(feature, out uint value))
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
            if (device.TryGetFeatureValue(feature, out float value))
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
            if (device.TryGetFeatureValue(feature, out Vector2 value))
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
            if (device.TryGetFeatureValue(feature, out Vector3 value))
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
            if (device.TryGetFeatureValue(feature, out Quaternion value))
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
            if (device.TryGetFeatureValue(feature, out Hand value))
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
            if (device.TryGetFeatureValue(feature, out Bone value))
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
            if (device.TryGetFeatureValue(feature, out Eyes value))
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
            if (device.TryGetFeatureValue(feature, out InputTrackingState value))
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
                    if (strUsages.Length > 0)
                    {
                        strUsages += ", ";
                    }

                    strUsages += usage.name;
                }

                Debug.Log(device.name + " feature usages:\n\n" + strUsages + "\n");
            }
        }
#endif
    }
}