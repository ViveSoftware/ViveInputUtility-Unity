//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const string EX_CAM_UI_SWITCH_TOOLTIP = "Enable this option to toggle quad view by pressing the switch key in play mode. (After the config file loaded successfully)";

        public const bool AUTO_LOAD_EXTERNAL_CAMERA_CONFIG_ON_START_DEFAULT_VALUE = true;
        public const string EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE = "externalcamera.cfg";
        public const bool ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE = true;
        public const KeyCode EXTERNAL_CAMERA_SWITCH_KEY_DEFAULT_VALUE = KeyCode.M;
        public const KeyCode EXTERNAL_CAMERA_SWITCH_KEY_MODIFIER_DEFAULT_VALUE = KeyCode.RightShift;

        [SerializeField]
        private bool m_autoLoadExternalCameraConfigOnStart = AUTO_LOAD_EXTERNAL_CAMERA_CONFIG_ON_START_DEFAULT_VALUE;
        [SerializeField]
        private string m_externalCameraConfigFilePath = EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE;
        [SerializeField, Tooltip(EX_CAM_UI_SWITCH_TOOLTIP)]
        private bool m_enableExternalCameraSwitch = ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE;
        [SerializeField]
        private KeyCode m_externalCameraSwitchKey = EXTERNAL_CAMERA_SWITCH_KEY_DEFAULT_VALUE;
        [SerializeField]
        private KeyCode m_externalCameraSwitchKeyModifier = EXTERNAL_CAMERA_SWITCH_KEY_MODIFIER_DEFAULT_VALUE;

        public static bool autoLoadExternalCameraConfigOnStart { get { return Instance == null ? AUTO_LOAD_EXTERNAL_CAMERA_CONFIG_ON_START_DEFAULT_VALUE : s_instance.m_autoLoadExternalCameraConfigOnStart; } set { if (Instance != null) { Instance.m_autoLoadExternalCameraConfigOnStart = value; } } }
        public static string externalCameraConfigFilePath { get { return Instance == null ? EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE : s_instance.m_externalCameraConfigFilePath; } set { if (Instance != null) { Instance.m_externalCameraConfigFilePath = value; } } }
        public static bool enableExternalCameraSwitch { get { return Instance == null ? ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE : s_instance.m_enableExternalCameraSwitch; } set { if (Instance != null) { Instance.m_enableExternalCameraSwitch = value; } } }
        public static KeyCode externalCameraSwitchKey { get { return Instance == null ? EXTERNAL_CAMERA_SWITCH_KEY_DEFAULT_VALUE : s_instance.m_externalCameraSwitchKey; } set { if (Instance != null) { Instance.m_externalCameraSwitchKey = value; } } }
        public static KeyCode externalCameraSwitchKeyModifier { get { return Instance == null ? EXTERNAL_CAMERA_SWITCH_KEY_MODIFIER_DEFAULT_VALUE : s_instance.m_externalCameraSwitchKeyModifier; } set { if (Instance != null) { Instance.m_externalCameraSwitchKeyModifier = value; } } }
    }
}