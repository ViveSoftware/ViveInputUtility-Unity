//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Text;
using UnityEngine;
using Valve.VR;
using System;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#elif UNITY_5_4_OR_NEWER
using XRSettings = UnityEngine.VR.VRSettings;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class SteamVRModule : VRModule.ModuleBase
    {
#if VIU_STEAMVR_2_0_0_OR_NEWER
        private const int DEVICE_COUNT = 11;

        private static bool s_pathInitialized;
        private static bool s_actionInitialized;
        private static int s_buttonActionLength;
        private static int s_axisActionLength;
        private static EnumUtils.EnumDisplayInfo s_buttonInfo;
        private static EnumUtils.EnumDisplayInfo s_axisInfo;
        private static string[] s_pressActionPaths;
        private static string[] s_touchActionPaths;
        private static string[] s_axisActionPaths;
        private static ulong[] s_pressActions;
        private static ulong[] s_touchActions;
        private static ulong[] s_axisActions;
        private static string[] s_inputSourcePaths;
        private static ulong[] s_inputSources;

        public static void InitializePaths()
        {
            if (s_pathInitialized) { return; }
            s_pathInitialized = true;

            s_buttonInfo = EnumUtils.GetDisplayInfo(typeof(VRModuleRawButton));
            s_buttonActionLength = s_buttonInfo.maxValue - s_buttonInfo.minValue + 1;
            s_pressActionPaths = new string[s_buttonActionLength];
            s_touchActionPaths = new string[s_buttonActionLength];
            for (int i = 0, value = s_buttonInfo.minValue, imax = s_buttonInfo.maxValue; value <= imax; ++i, ++value)
            {
                var buttonName = Enum.GetName(typeof(VRModuleRawButton), value).ToLower();
                s_pressActionPaths[i] = "/actions/htc_viu_actions/in/viu_press_" + buttonName;
                s_touchActionPaths[i] = "/actions/htc_viu_actions/in/viu_touch_" + buttonName;
            }

            s_axisInfo = EnumUtils.GetDisplayInfo(typeof(VRModuleRawAxis));
            s_axisActionLength = s_axisInfo.maxValue - s_axisInfo.minValue + 1;
            s_axisActionPaths = new string[s_axisActionLength];
            for (int i = 0, value = s_axisInfo.minValue, imax = s_axisInfo.maxValue; value <= imax; ++i, ++value)
            {
                var axisName = Enum.GetName(typeof(VRModuleRawAxis), value).ToLower();
                s_axisActionPaths[i] = "/actions/htc_viu_actions/in/viu_axis_" + axisName;
            }

            s_inputSourcePaths = new string[DEVICE_COUNT]
            {
                "/user/head",
                "/user/hand/left",
                "/user/hand/right",
                "/user/foot/left",
                "/user/foot/right",
                "/user/shoulder/left",
                "/user/shoulder/right",
                "/user/waist",
                "/user/chest",
                "/user/camera",
                "/user/keyboard",
            };
        }

        public static void InitializeHandles()
        {
            if (!Application.isPlaying || s_actionInitialized) { return; }
            s_actionInitialized = true;

            InitializePaths();

            SteamVR_Input.Initialize();

            var inputSystem = OpenVR.Input;
            if (inputSystem == null)
            {
                Debug.LogError("Fail loading OpenVR.Input");
                return;
            }

            s_pressActions = new ulong[s_buttonActionLength];
            s_touchActions = new ulong[s_buttonActionLength];
            for (int i = 0, imax = s_buttonActionLength; i < imax; ++i)
            {
                EVRInputError error;

                if (!string.IsNullOrEmpty(s_pressActionPaths[i]))
                {
                    error = inputSystem.GetActionHandle(s_pressActionPaths[i], ref s_pressActions[i]);
                    if (error != EVRInputError.None)
                    {
                        Debug.LogError("Load " + s_pressActionPaths[i] + " action failed! " + error);
                    }
                }

                if (!string.IsNullOrEmpty(s_touchActionPaths[i]))
                {
                    error = inputSystem.GetActionHandle(s_touchActionPaths[i], ref s_touchActions[i]);
                    if (error != EVRInputError.None)
                    {
                        Debug.LogError("Load " + s_touchActionPaths[i] + " action failed! " + error);
                    }
                }
            }

            s_axisActions = new ulong[s_axisActionLength];
            for (int i = 0, imax = s_axisActionLength; i < imax; ++i)
            {
                EVRInputError error;

                if (!string.IsNullOrEmpty(s_axisActionPaths[i]))
                {
                    error = inputSystem.GetActionHandle(s_axisActionPaths[i], ref s_axisActions[i]);
                    if (error != EVRInputError.None)
                    {
                        Debug.LogError("Load " + s_axisActionPaths[i] + " action failed! " + error);
                    }
                }
            }

            s_inputSources = new ulong[DEVICE_COUNT];
            for (int i = 0; i < DEVICE_COUNT; ++i)
            {
                EVRInputError error;

                if (!string.IsNullOrEmpty(s_inputSourcePaths[i]))
                {
                    error = inputSystem.GetInputSourceHandle(s_inputSourcePaths[i], ref s_inputSources[i]);
                    if (error != EVRInputError.None)
                    {
                        Debug.LogError("Load " + s_inputSourcePaths[i] + " input source failed! " + error);
                    }
                }
            }
        }

        public override bool ShouldActiveModule()
        {
#if UNITY_5_4_OR_NEWER
            return VIUSettings.activateSteamVRModule && XRSettings.enabled && XRSettings.loadedDeviceName == "OpenVR";
#else
            return VIUSettings.activateSteamVRModule && SteamVR.enabled;
#endif
        }

        public override void OnActivated()
        {
            InitializeHandles();

            SteamVR_Input.OnNonVisualActionsUpdated += UpdateDeviceInput;
            SteamVR_Input.OnPosesUpdated += UpdateDevicePose;
        }

        private void UpdateDeviceInput()
        {

        }

        private void UpdateDevicePose(bool obj)
        {

        }
#endif
    }
}
