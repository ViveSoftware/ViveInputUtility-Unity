//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using HTC.UnityPlugin.Vive;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.UnityNativeVR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.UnityNativeVR; } }

#if !UNITY_2020_1_OR_NEWER
        private static KeyCode[] s_keyCodes = new KeyCode[]
        {
            KeyCode.JoystickButton0,
            KeyCode.JoystickButton1,
            KeyCode.JoystickButton2,
            KeyCode.JoystickButton3,
            KeyCode.JoystickButton4,
            KeyCode.JoystickButton5,
            KeyCode.JoystickButton6,
            KeyCode.JoystickButton7,
            KeyCode.JoystickButton8,
            KeyCode.JoystickButton9,
            KeyCode.JoystickButton10,
            KeyCode.JoystickButton11,
            KeyCode.JoystickButton12,
            KeyCode.JoystickButton13,
            KeyCode.JoystickButton14,
            KeyCode.JoystickButton15,
            KeyCode.JoystickButton16,
            KeyCode.JoystickButton17,
            KeyCode.JoystickButton18,
            KeyCode.JoystickButton19,
        };

        private static string[] s_axisNames = new string[]
        {
            "HTC_VIU_UnityAxis1",
            "HTC_VIU_UnityAxis2",
            "HTC_VIU_UnityAxis3",
            "HTC_VIU_UnityAxis4",
            "HTC_VIU_UnityAxis5",
            "HTC_VIU_UnityAxis6",
            "HTC_VIU_UnityAxis7",
            "HTC_VIU_UnityAxis8",
            "HTC_VIU_UnityAxis9",
            "HTC_VIU_UnityAxis10",
            "HTC_VIU_UnityAxis11",
            "HTC_VIU_UnityAxis12",
            "HTC_VIU_UnityAxis13",
            "HTC_VIU_UnityAxis14",
            "HTC_VIU_UnityAxis15",
            "HTC_VIU_UnityAxis16",
            "HTC_VIU_UnityAxis17",
            "HTC_VIU_UnityAxis18",
            "HTC_VIU_UnityAxis19",
            "HTC_VIU_UnityAxis20",
            "HTC_VIU_UnityAxis21",
            "HTC_VIU_UnityAxis22",
            "HTC_VIU_UnityAxis23",
            "HTC_VIU_UnityAxis24",
            "HTC_VIU_UnityAxis25",
            "HTC_VIU_UnityAxis26",
            "HTC_VIU_UnityAxis27",
        };

        public static bool GetUnityButton(int id)
        {
            return Input.GetKey(s_keyCodes[id]);
        }

        public static float GetUnityAxis(int id)
        {
            return Input.GetAxisRaw(s_axisNames[id - 1]);
        }
#if UNITY_EDITOR
        public static int GetUnityAxisCount() { return s_axisNames.Length; }

        public static string GetUnityAxisNameByIndex(int index) { return s_axisNames[index]; }

        public static int GetUnityAxisIdByIndex(int index) { return index + 1; }
#endif

        public override bool ShouldActiveModule() { return VIUSettings.activateUnityNativeVRModule && XRSettings.enabled; }

        public override void Update()
        {
            // set physics update rate to vr render rate
            if (VRModule.lockPhysicsUpdateRateToRenderFrequency && Time.timeScale > 0.0f)
            {
                // FIXME: VRDevice.refreshRate returns zero in Unity 5.6.0 or older version
#if UNITY_5_6_OR_NEWER
                Time.fixedDeltaTime = 1f / XRDevice.refreshRate;
#else
                Time.fixedDeltaTime = 1f / 90f;
#endif
            }
        }

        private static void UpdateLeftControllerInput(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            switch (currState.deviceModel)
            {
                case VRModuleDeviceModel.ViveCosmosControllerLeft:
                case VRModuleDeviceModel.ViveController:
                    Update_L_Vive(prevState, currState);
                    break;
                case VRModuleDeviceModel.OculusQuestControllerLeft:
                case VRModuleDeviceModel.OculusGoController:
                case VRModuleDeviceModel.OculusTouchLeft:
                    Update_L_OculusTouch(prevState, currState);
                    break;
                case VRModuleDeviceModel.KnucklesLeft:
                case VRModuleDeviceModel.IndexControllerLeft:
                    Update_L_Knuckles(prevState, currState);
                    break;
                case VRModuleDeviceModel.WMRControllerLeft:
                    Update_L_MicrosoftMR(prevState, currState);
                    break;
            }
        }

        private static void UpdateRightControllerInput(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            switch (currState.deviceModel)
            {
                case VRModuleDeviceModel.ViveCosmosControllerRight:
                case VRModuleDeviceModel.ViveController:
                    Update_R_Vive(prevState, currState);
                    break;
                case VRModuleDeviceModel.OculusQuestControllerRight:
                case VRModuleDeviceModel.OculusGoController:
                case VRModuleDeviceModel.OculusTouchRight:
                    Update_R_OculusTouch(prevState, currState);
                    break;
                case VRModuleDeviceModel.KnucklesRight:
                case VRModuleDeviceModel.IndexControllerRight:
                    Update_R_Knuckles(prevState, currState);
                    break;
                case VRModuleDeviceModel.WMRControllerRight:
                    Update_R_MicrosoftMR(prevState, currState);
                    break;
            }
        }

        private static void Update_L_Vive(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var primaryButtonPress = GetUnityButton(3);
            var menuPress = GetUnityButton(2);
            var padPress = GetUnityButton(8);
            var triggerTouch = GetUnityButton(14);
            var padTouch = GetUnityButton(16);

            var padX = GetUnityAxis(1);
            var padY = GetUnityAxis(2);
            var trigger = GetUnityAxis(9);
            var grip = GetUnityAxis(11);

            currState.SetButtonPress(VRModuleRawButton.A, primaryButtonPress);
            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuPress);
            currState.SetButtonPress(VRModuleRawButton.Grip, grip >= 1.0f);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, padPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.55f, 0.45f));

            currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -padY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
        }

        private static void Update_R_Vive(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var primaryButtonPress = GetUnityButton(1);
            var menuPress = GetUnityButton(0);
            var padPress = GetUnityButton(9);
            var triggerTouch = GetUnityButton(15);
            var padTouch = GetUnityButton(17);

            var padX = GetUnityAxis(4);
            var padY = GetUnityAxis(5);
            var trigger = GetUnityAxis(10);
            var grip = GetUnityAxis(12);

            currState.SetButtonPress(VRModuleRawButton.A, primaryButtonPress);
            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuPress);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, padPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.55f, 0.45f));
            currState.SetButtonPress(VRModuleRawButton.Grip, grip >= 1.0f);

            currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -padY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
        }

        private static void Update_L_OculusTouch(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var startPress = GetUnityButton(6);
            var xPress = GetUnityButton(2);
            var yPress = GetUnityButton(3);
            var stickPress = GetUnityButton(8);
            var gripPress = GetUnityButton(4);
            var xTouch = GetUnityButton(12);
            var yTouch = GetUnityButton(13);
            var triggerTouch = GetUnityButton(14);
            var stickTouch = GetUnityButton(16);

            var stickX = GetUnityAxis(1);
            var stickY = GetUnityAxis(2);
            var trigger = GetUnityAxis(9);
            var grip = GetUnityAxis(11);

            currState.SetButtonPress(VRModuleRawButton.System, startPress);
            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, yPress);
            currState.SetButtonPress(VRModuleRawButton.A, xPress);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, stickPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.55f, 0.45f));
            currState.SetButtonPress(VRModuleRawButton.Grip, gripPress);
            currState.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripPress);

            currState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, yTouch);
            currState.SetButtonTouch(VRModuleRawButton.A, xTouch);
            currState.SetButtonTouch(VRModuleRawButton.Touchpad, stickTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            currState.SetButtonTouch(VRModuleRawButton.Grip, grip >= 0.05f);
            currState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, grip >= 0.05f);

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, stickX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -stickY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            currState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
        }

        private static void Update_R_OculusTouch(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var aPress = GetUnityButton(0);
            var bPress = GetUnityButton(1);
            var stickPress = GetUnityButton(9);
            var gripPress = GetUnityButton(5);
            var aTouch = GetUnityButton(10);
            var bTouch = GetUnityButton(11);
            var triggerTouch = GetUnityButton(15);
            var stickTouch = GetUnityButton(17);

            var stickX = GetUnityAxis(4);
            var stickY = GetUnityAxis(5);
            var trigger = GetUnityAxis(10);
            var grip = GetUnityAxis(12);

            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, bPress);
            currState.SetButtonPress(VRModuleRawButton.A, aPress);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, stickPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.55f, 0.45f));
            currState.SetButtonPress(VRModuleRawButton.Grip, gripPress);
            currState.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripPress);

            currState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, bTouch);
            currState.SetButtonTouch(VRModuleRawButton.A, aTouch);
            currState.SetButtonTouch(VRModuleRawButton.Touchpad, stickTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            currState.SetButtonTouch(VRModuleRawButton.Grip, grip >= 0.05f);
            currState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, grip >= 0.05f);

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, stickX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -stickY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            currState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
        }

        private static void Update_L_Knuckles(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var innerPress = GetUnityButton(2);
            var outerPress = GetUnityButton(3);
            var padPress = GetUnityButton(8);
            var triggerTouch = GetUnityButton(14);
            var padTouch = GetUnityButton(16);

            var padX = GetUnityAxis(1);
            var padY = GetUnityAxis(2);
            var trigger = GetUnityAxis(9);
            var grip = GetUnityAxis(11);
            var index = GetUnityAxis(20);
            var middle = GetUnityAxis(22);
            var ring = GetUnityAxis(24);
            var pinky = GetUnityAxis(26);

            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, outerPress);
            currState.SetButtonPress(VRModuleRawButton.A, innerPress);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, padPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.55f, 0.45f));
            currState.SetButtonPress(VRModuleRawButton.Grip, grip >= 1.0f);

            currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -padY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            currState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            currState.SetAxisValue(VRModuleRawAxis.IndexCurl, index);
            currState.SetAxisValue(VRModuleRawAxis.MiddleCurl, middle);
            currState.SetAxisValue(VRModuleRawAxis.RingCurl, ring);
            currState.SetAxisValue(VRModuleRawAxis.PinkyCurl, pinky);
        }

        private static void Update_R_Knuckles(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var innerPress = GetUnityButton(0);
            var outerPress = GetUnityButton(1);
            var padPress = GetUnityButton(9);
            var triggerTouch = GetUnityButton(15);
            var padTouch = GetUnityButton(17);

            var padX = GetUnityAxis(4);
            var padY = GetUnityAxis(5);
            var trigger = GetUnityAxis(10);
            var grip = GetUnityAxis(12);
            var index = GetUnityAxis(21);
            var middle = GetUnityAxis(23);
            var ring = GetUnityAxis(25);
            var pinky = GetUnityAxis(27);

            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, outerPress);
            currState.SetButtonPress(VRModuleRawButton.A, innerPress);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, padPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.55f, 0.45f));
            currState.SetButtonPress(VRModuleRawButton.Grip, grip >= 1.0f);

            currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -padY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            currState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
            currState.SetAxisValue(VRModuleRawAxis.IndexCurl, index);
            currState.SetAxisValue(VRModuleRawAxis.MiddleCurl, middle);
            currState.SetAxisValue(VRModuleRawAxis.RingCurl, ring);
            currState.SetAxisValue(VRModuleRawAxis.PinkyCurl, pinky);
        }

        private static void Update_L_MicrosoftMR(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var menuPress = GetUnityButton(2);
            var padPress = GetUnityButton(8);
            var triggerPress = GetUnityButton(14);
            var padTouch = GetUnityButton(16);

            var stickX = GetUnityAxis(1);
            var stickY = GetUnityAxis(2);
            var trigger = GetUnityAxis(9);
            var grip = GetUnityAxis(11);
            var padX = GetUnityAxis(17);
            var padY = GetUnityAxis(18);

            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuPress);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, padPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, triggerPress);
            currState.SetButtonPress(VRModuleRawButton.Grip, grip >= 1f);

            currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.25f, 0.20f));

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -padY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            currState.SetAxisValue(VRModuleRawAxis.JoystickX, stickX);
            currState.SetAxisValue(VRModuleRawAxis.JoystickY, -stickY);
        }

        private static void Update_R_MicrosoftMR(IVRModuleDeviceState prevState, IVRModuleDeviceStateRW currState)
        {
            var menuPress = GetUnityButton(0);
            var padPress = GetUnityButton(9);
            var triggerPress = GetUnityButton(15);
            var padTouch = GetUnityButton(17);

            var stickX = GetUnityAxis(4);
            var stickY = GetUnityAxis(5);
            var trigger = GetUnityAxis(10);
            var grip = GetUnityAxis(12);
            var padX = GetUnityAxis(19);
            var padY = GetUnityAxis(20);

            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuPress);
            currState.SetButtonPress(VRModuleRawButton.Touchpad, padPress);
            currState.SetButtonPress(VRModuleRawButton.Trigger, triggerPress);
            currState.SetButtonPress(VRModuleRawButton.Grip, grip >= 1f);

            currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouch);
            currState.SetButtonTouch(VRModuleRawButton.Trigger, AxisToPress(prevState.GetButtonPress(VRModuleRawButton.Trigger), trigger, 0.25f, 0.20f));

            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padX);
            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, -padY);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            currState.SetAxisValue(VRModuleRawAxis.JoystickX, stickX);
            currState.SetAxisValue(VRModuleRawAxis.JoystickY, -stickY);
        }
#endif
    }
}