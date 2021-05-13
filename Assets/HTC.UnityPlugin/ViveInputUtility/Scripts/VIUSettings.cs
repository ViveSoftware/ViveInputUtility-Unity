//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const string DEFAULT_RESOURCE_PATH = "VIUSettings";
        public const string INDIVIDUAL_TOUCHPAD_JOYSTICK_VALUE_TOOLTIP = "Set touchpad and joystick value individually for different controller type. For example, Vive Controller will have touchpad value but no thumbstick value, Oculus Touch will have thumbstick value but no touchpad value.";
        public const bool AUTO_CHECK_NEW_VIU_VERSION_DEFAULT_VALUE = true;
        public const float VIRTUAL_DPAD_DEAD_ZONE_DEFAULT_VALUE = 0.25f;
        public const bool INDIVIDUAL_TOUCHPAD_JOYSTICK_VALUE_DEFAULT_VALUE = false;

        [SerializeField]
        private bool m_autoCheckNewVIUVersion = AUTO_CHECK_NEW_VIU_VERSION_DEFAULT_VALUE;
        [SerializeField]
        private float m_virtualDPadDeadZone = VIRTUAL_DPAD_DEAD_ZONE_DEFAULT_VALUE;
        [SerializeField, Tooltip(INDIVIDUAL_TOUCHPAD_JOYSTICK_VALUE_TOOLTIP)]
        private bool m_individualTouchpadJoystickValue = INDIVIDUAL_TOUCHPAD_JOYSTICK_VALUE_DEFAULT_VALUE;

        [Serializable]
        public class DeviceModelArray : EnumArray<VRModuleDeviceModel, GameObject> { }

        [SerializeField]
        private DeviceModelArray m_overrideDeviceModel = new DeviceModelArray();

        public static bool autoCheckNewVIUVersion { get { return Instance == null ? AUTO_CHECK_NEW_VIU_VERSION_DEFAULT_VALUE : s_instance.m_autoCheckNewVIUVersion; } set { if (Instance != null) { Instance.m_autoCheckNewVIUVersion = value; } } }
        public static float virtualDPadDeadZone { get { return Instance == null ? VIRTUAL_DPAD_DEAD_ZONE_DEFAULT_VALUE : s_instance.m_virtualDPadDeadZone; } set { if (Instance != null) { Instance.m_virtualDPadDeadZone = value; } } }
        public static bool individualTouchpadJoystickValue { get { return Instance == null ? INDIVIDUAL_TOUCHPAD_JOYSTICK_VALUE_DEFAULT_VALUE : s_instance.m_individualTouchpadJoystickValue; } set { if (Instance != null) { Instance.m_individualTouchpadJoystickValue = value; } } }

        public static GameObject GetOverrideDeviceModel(VRModuleDeviceModel model) { return Instance == null || s_instance.m_overrideDeviceModel == null ? null : s_instance.m_overrideDeviceModel[model]; }
        public static void SetOverrideDeviceModel(VRModuleDeviceModel model, GameObject obj)
        {
            if (Instance == null) { return; }
            if (s_instance.m_overrideDeviceModel == null) { return; }
            s_instance.m_overrideDeviceModel[model] = obj;
        }

        private static VIUSettings s_instance = null;

        public static VIUSettings Instance
        {
            get
            {
                if (s_instance == null)
                {
                    LoadFromResource();
                }

                return s_instance;
            }
        }

        public static void LoadFromResource(string path = null)
        {
            if (path == null)
            {
                path = DEFAULT_RESOURCE_PATH;
            }

            if ((s_instance = Resources.Load<VIUSettings>(path)) == null)
            {
                s_instance = CreateInstance<VIUSettings>();
                s_instance.m_bindingInterfaceObjectSource = Resources.Load<GameObject>(BINDING_INTERFACE_PREFAB_DEFAULT_RESOURCE_PATH);
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}