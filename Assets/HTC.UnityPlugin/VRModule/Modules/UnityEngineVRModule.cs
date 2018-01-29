//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

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
        public static class ButtonKeyCode
        {
            private static KeyCode[] s_codes = new KeyCode[]
            {
                KeyCode.JoystickButton0,
                KeyCode.JoystickButton2,
                KeyCode.JoystickButton10,
                KeyCode.JoystickButton12,
                KeyCode.JoystickButton9,
                KeyCode.JoystickButton8,
                KeyCode.JoystickButton17,
                KeyCode.JoystickButton16,
                KeyCode.JoystickButton1,
                KeyCode.JoystickButton3,
                KeyCode.JoystickButton11,
                KeyCode.JoystickButton13,
                KeyCode.JoystickButton15,
                KeyCode.JoystickButton14,
            };

            public static int Count = s_codes.Length;

            public static KeyCode RMenuPress = s_codes[0];
            public static KeyCode LMenuPress = s_codes[1];
            public static KeyCode RMenuTouch = s_codes[2];
            public static KeyCode LMenuTouch = s_codes[3];
            public static KeyCode RPadPress = s_codes[4];
            public static KeyCode LPadPress = s_codes[5];
            public static KeyCode RPadTouch = s_codes[6];
            public static KeyCode LPadTouch = s_codes[7];
            public static KeyCode RAKeyPress = s_codes[8];
            public static KeyCode LAKeyPress = s_codes[9];
            public static KeyCode RAKeyTouch = s_codes[10];
            public static KeyCode LAKeyTouch = s_codes[11];
            public static KeyCode RTriggerTouch = s_codes[12];
            public static KeyCode LTriggerTouch = s_codes[13];

            public static KeyCode Index(int i) { return s_codes[i]; }
        }

        public static class ButtonAxisName
        {
            private static readonly string[] s_names = new string[]
            {
                "HTC_VIU_LeftTrackpadHorizontal",
                "HTC_VIU_LeftTrackpadVertical",
                "HTC_VIU_RightTrackpadHorizontal",
                "HTC_VIU_RightTrackpadVertical",
                "HTC_VIU_LeftTrigger",
                "HTC_VIU_RightTrigger",
                "HTC_VIU_LeftGrip",
                "HTC_VIU_RightGrip",
            };

            public static int Count = s_names.Length;

            public static string LPadX = s_names[0];
            public static string LPadY = s_names[1];
            public static string RPadX = s_names[2];
            public static string RPadY = s_names[3];
            public static string LTrigger = s_names[4];
            public static string RTrigger = s_names[5];
            public static string LGrip = s_names[6];
            public static string RGrip = s_names[7];

            public static string Index(int i) { return s_names[i]; }
        }

#if UNITY_EDITOR
        public static class ButtonAxisID
        {
            private static readonly int[] s_ids = new int[]
            {
                0,
                1,
                3,
                4,
                8,
                9,
                10,
                11,
            };

            public static int Count = s_ids.Length;

            public static int LPadX = s_ids[0];
            public static int LPadY = s_ids[1];
            public static int RPadX = s_ids[2];
            public static int RPadY = s_ids[3];
            public static int LTrigger = s_ids[4];
            public static int RTrigger = s_ids[5];
            public static int LGtip = s_ids[6];
            public static int RGtip = s_ids[7];

            public static int Index(int i) { return s_ids[i]; }
        }
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
    }
}