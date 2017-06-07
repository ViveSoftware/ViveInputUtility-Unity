//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
        public enum UnityVRControllerButton
        {
            LeftMenuButtonPress,
            RightMenuButtonPress,
            LeftTrackpadPress,
            RightTrackpadPress,
            LeftTrackpadTouch,
            RightTrackpadTouch,
            LeftTriggerTouch,
            RightTriggerTouch,
        }

        public enum UnityVRControllerAxis
        {
            LeftTrackpadHorizontal,
            LeftTrackpadVertical,
            RightTrackpadHorizontal,
            RightTrackpadVertical,
            LeftTriggerSqueeze,
            RightTriggerSqueeze,
            LeftGripSqueeze,
            RightGripSqueeze,
        }

        public static readonly KeyCode[] vrControllerButtonKeyCodes = new KeyCode[]
        {
                KeyCode.JoystickButton2,
                KeyCode.JoystickButton0,
                KeyCode.JoystickButton8,
                KeyCode.JoystickButton9,
                KeyCode.JoystickButton16,
                KeyCode.JoystickButton17,
                KeyCode.JoystickButton14,
                KeyCode.JoystickButton15,
        };

        public static readonly string[] vrControllerAxisVirtualButtonNames = new string[]
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
#if UNITY_EDITOR
        public static readonly int[] vrControllerAxisIDs = new int[]
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
#endif
    }
}