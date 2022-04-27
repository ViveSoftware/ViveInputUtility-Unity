//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    [Obsolete("Use VRModuleKnownXRInputSubsystem instead")]
    public enum XRInputSubsystemType
    {
        Unknown,
        OpenVR,
        Oculus,
        WMR,
        MagicLeap,
    }

    public sealed class UnityXRModule : UnityXRModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.UnityXR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.UnityXR; } }

        public const string WAVE_XR_LOADER_NAME = "Wave XR Loader";
        public const string WAVE_XR_LOADER_CLASS_NAME = "WaveXRLoader";
        public const string OPENXR_LOADER_NAME = "Open XR Loader";
        public const string OPENXR_LOADER_CLASS_NAME = "OpenXRLoader";

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
        private static UnityXRModule s_moduleInstance;

        public override bool ShouldActiveModule()
        {
            return VIUSettings.activateUnityXRModule && HasActiveLoader();
        }

        public override void OnActivated()
        {
            s_moduleInstance = this;
            base.OnActivated();
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            s_moduleInstance = null;
        }

        private Action<IVRModuleDeviceStateRW, InputDevice>[] updateControllerStateDelegates = new Action<IVRModuleDeviceStateRW, InputDevice>[VRModule.MAX_DEVICE_COUNT];
        protected override void UpdateNewConnectedInputDevice(IVRModuleDeviceStateRW state, InputDevice device)
        {
            Action<IVRModuleDeviceStateRW, InputDevice> updateFunc;
            switch (state.deviceModel)
            {
                case VRModuleDeviceModel.ViveController:
                    updateFunc = UpdateViveControllerState;
                    break;
                case VRModuleDeviceModel.ViveCosmosControllerLeft:
                case VRModuleDeviceModel.ViveCosmosControllerRight:
                    updateFunc = UpdateViveCosmosControllerState;
                    break;
                case VRModuleDeviceModel.ViveTracker:
                case VRModuleDeviceModel.ViveTracker3:
                    updateFunc = UpdateViveTrackerState;
                    break;
                case VRModuleDeviceModel.OculusTouchLeft:
                case VRModuleDeviceModel.OculusTouchRight:
                case VRModuleDeviceModel.OculusGoController:
                case VRModuleDeviceModel.OculusQuestControllerLeft:
                case VRModuleDeviceModel.OculusQuestControllerRight:
                case VRModuleDeviceModel.OculusQuest2ControllerLeft:
                case VRModuleDeviceModel.OculusQuest2ControllerRight:
                    updateFunc = UpdateOculusControllerState;
                    break;
                case VRModuleDeviceModel.WMRControllerLeft:
                case VRModuleDeviceModel.WMRControllerRight:
                    updateFunc = UpdateWMRControllerState;
                    break;
                case VRModuleDeviceModel.KnucklesLeft:
                case VRModuleDeviceModel.KnucklesRight:
                case VRModuleDeviceModel.IndexControllerLeft:
                case VRModuleDeviceModel.IndexControllerRight:
                    updateFunc = UpdateIndexControllerState;
                    break;
                case VRModuleDeviceModel.MagicLeapController:
                    updateFunc = UpdateMagicLeapControllerState;
                    break;
                case VRModuleDeviceModel.ViveFocusChirp:
                    updateFunc = UpdateViveFocusChirpControllerState;
                    break;
                case VRModuleDeviceModel.ViveFocusFinch:
                case VRModuleDeviceModel.ViveFlowPhoneController:
                    updateFunc = UpdateViveFocusFinchControllerState;
                    break;
                case VRModuleDeviceModel.KhronosSimpleController:
                    updateFunc = UpdateKhronosSimpleControllerState;
                    break;
                case VRModuleDeviceModel.ViveFocus3ControllerLeft:
                case VRModuleDeviceModel.ViveFocus3ControllerRight:
                    updateFunc = UpdateWaveCRControllerState;
                    break;
                case VRModuleDeviceModel.ViveWristTracker:
                    updateFunc = UpdateViveWristTrackerState;
                    break;
                default:
                    updateFunc = UpdateUnknownControllerState;
                    break;
            }

            updateControllerStateDelegates[state.deviceIndex] = updateFunc;
        }

        protected override void UpdateInputDevicesControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            var updateFunc = updateControllerStateDelegates[state.deviceIndex];
            if (updateFunc != null) { updateFunc(state, device); }
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

        private void UpdateUnknownControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryTouch);
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton);
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryTouch);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxisClick);
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxisTouch);

            float triggerValue = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            float gripValue = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);

            Vector2 primary2DAxisValue = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);
            Vector2 secondary2DAxisValue = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton | menuButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Joystick, secondary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch | menuButton);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonTouch(VRModuleRawButton.Grip, gripButton);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
            state.SetButtonTouch(VRModuleRawButton.Joystick, secondary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, triggerValue);
            state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, gripValue);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxisValue.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxisValue.y);
            state.SetAxisValue(VRModuleRawAxis.JoystickX, secondary2DAxisValue.x);
            state.SetAxisValue(VRModuleRawAxis.JoystickY, secondary2DAxisValue.y);
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

            if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.OpenXR)
            {
                bool systemButton = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("SystemButton")); // Always false
                float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip); // 0 or 1

                state.SetButtonPress(VRModuleRawButton.System, systemButton);
                state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            }
        }

        private void UpdateViveCosmosControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton); // X/A
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // Y/B
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);

            if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.OpenVR)
            {
                bool primaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryTouch); // X/A
                bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryTouch); // Y/B
                bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
                bool bumperButton = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("BumperButton"));
                bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
                bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));

                state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
                state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
                state.SetButtonPress(VRModuleRawButton.Bumper, bumperButton);

                state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
                state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
                state.SetButtonTouch(VRModuleRawButton.Grip, gripButton);
                state.SetButtonTouch(VRModuleRawButton.Bumper, bumperButton);
                state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            }
            else if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.OpenXR)
            {
                bool gripPressed = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("GripPressed"));
                bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
                bool triggerPressed = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerPressed"));
                bool shoulderButton = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("ShoulderButton"));
                float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);

                state.SetButtonPress(VRModuleRawButton.Grip, gripPressed);
                state.SetButtonPress(VRModuleRawButton.Trigger, triggerPressed);
                state.SetButtonPress(VRModuleRawButton.Bumper, shoulderButton);

                state.SetButtonTouch(VRModuleRawButton.A, primaryButton);
                state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryButton);
                state.SetButtonTouch(VRModuleRawButton.Grip, gripPressed);
                state.SetButtonTouch(VRModuleRawButton.Bumper, shoulderButton);
                state.SetButtonTouch(VRModuleRawButton.Trigger, triggerPressed);

                // conflict with JoystickX
                //state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);

                // menuButton conflicts with secondaryButton

                // Unused
                // [Vector3] PointerPosition
                // [Quaternion] PointerRotation
                // [Vector3] PointerVelocity
                // [Vector3] PointerAngularVelocity
            }
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
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);

            if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.OpenVR)
            {
                bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));

                state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            }
            else if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.Oculus)
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
            else if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.OpenXR)
            {
                bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));

                state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);

                if ((device.characteristics & InputDeviceCharacteristics.Left) != 0)
                {
                    bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);

                    state.SetButtonPress(VRModuleRawButton.System, menuButton);
                }

                // Unused
                // [Vector3] PointerPosition
                // [Quaternion] PointerRotation
                // [Vector3] PointerVelocity
                // [Vector3] PointerAngularVelocity
            }
        }

        private void UpdateWMRControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);

            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);

            if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.OpenVR)
            {
                bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Touchpad
                bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick"));
                bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Touchpad
                Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
                Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // Joystick

                state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
                state.SetButtonPress(VRModuleRawButton.Joystick, secondary2DAxisClick);
                state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
                state.SetButtonTouch(VRModuleRawButton.Joystick, secondary2DAxisClick);

                state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
                state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
                state.SetAxisValue(VRModuleRawAxis.JoystickX, secondary2DAxis.x);
                state.SetAxisValue(VRModuleRawAxis.JoystickY, secondary2DAxis.y);
            }
            else if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.WindowsXR)
            {
                float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);
                float sourceLossRisk = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("SourceLossRisk")); // Not in use
                Vector3 pointerPosition = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector3>("PointerPosition")); // Not in use
                Vector3 sourceMitigationDirection = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector3>("SourceMitigationDirection")); // Not in use
                Quaternion pointerRotation = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Quaternion>("PointerRotation")); // Not in use

                bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Touchpad
                bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick"));
                bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Touchpad
                Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
                Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // Joystick

                state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
                state.SetButtonPress(VRModuleRawButton.Joystick, secondary2DAxisClick);
                state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
                state.SetButtonTouch(VRModuleRawButton.Joystick, secondary2DAxisClick);

                state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
                state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
                state.SetAxisValue(VRModuleRawAxis.JoystickX, secondary2DAxis.x);
                state.SetAxisValue(VRModuleRawAxis.JoystickY, secondary2DAxis.y);

                // conflict with JoystickX
                //state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            }
            else if (KnownActiveInputSubsystem == VRModuleKnownXRInputSubsystem.OpenXR)
            {
                float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);

                bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Joystick; always false
                bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
                bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxisClick); // Touchpad
                bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // Touchpad
                Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Joystick; always 0
                Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // Touchpad

                state.SetButtonPress(VRModuleRawButton.Touchpad, secondary2DAxisClick);
                state.SetButtonPress(VRModuleRawButton.Joystick, primary2DAxisClick);
                state.SetButtonTouch(VRModuleRawButton.Touchpad, secondary2DAxisTouch);
                state.SetButtonTouch(VRModuleRawButton.Joystick, primary2DAxisTouch);

                state.SetAxisValue(VRModuleRawAxis.TouchpadX, secondary2DAxis.x);
                state.SetAxisValue(VRModuleRawAxis.TouchpadY, secondary2DAxis.y);
                state.SetAxisValue(VRModuleRawAxis.JoystickX, primary2DAxis.x);
                state.SetAxisValue(VRModuleRawAxis.JoystickY, primary2DAxis.y);

                // conflict with JoystickX
                //state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            }
        }

        private void UpdateIndexControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool menuTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("MenuTouch"));
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("PrimaryTouch"));
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // B
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("SecondaryTouch"));
            float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            float gripForce = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("GripForce"));
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));
            // joystick
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            // touchpad
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis);
            float secondary2DAxisForce = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("Secondary2DAxisForce"));
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch"));

            state.SetButtonPress(VRModuleRawButton.System, menuButton);
            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, secondary2DAxisForce > 0.5f);
            state.SetButtonPress(VRModuleRawButton.Joystick, primary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.System, menuTouch);
            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
            state.SetButtonTouch(VRModuleRawButton.Grip, grip > 0f);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, secondary2DAxisTouch);
            state.SetButtonTouch(VRModuleRawButton.Joystick, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.TouchpadX, secondary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, secondary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.JoystickX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.JoystickY, primary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.Axis1Y, gripForce);
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

        private void UpdateKhronosSimpleControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);

            // Unused
            // [Vector3] PointerPosition
            // [Quaternion] PointerRotation
            // [Vector3] PointerVelocity
            // [Vector3] PointerAngularVelocity
        }

        private void UpdateViveWristTrackerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, primaryButton);
            state.SetButtonPress(VRModuleRawButton.System, menuButton);
        }

        private void UpdateWaveCRControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick"));
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch"));
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton);
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool gripTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("GripTouch"));
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis);
            Vector2 dPad = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector2>("DPad"));

            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.System, menuButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonTouch(VRModuleRawButton.Grip, gripTouch);
            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
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
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton); // No Data
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger); // No Data
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // No data
            Vector2 dPad = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector2>("DPad")); // No Data

            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
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
#endif
    }
}