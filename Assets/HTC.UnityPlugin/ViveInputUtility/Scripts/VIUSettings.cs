//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class VIUSettings : ScriptableObject
    {
        public const string DEFAULT_RESOURCE_PATH = "VIUSettings";

        public const string BIND_UI_SWITCH_TOOLTIP = "Enable this option to open binding interface by pressing the switch key in play mode.";
        public const string EX_CAM_UI_SWITCH_TOOLTIP = "Enable this option to toggle quad view by pressing the switch key in play mode. (After the config file loaded successfully)";
        public const string SIMULATE_TRACKPAD_TOUCH_TOOLTIP = "Hold Shift key and move the mouse to simulate trackpad touching event";
        public const string SIMULATOR_KEY_MOVE_SPEED_TOOLTIP = "W/A/S/D";
        public const string SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP = "Arrow Up/Down/Left/Right";

        public const bool AUTO_CHECK_NEW_VIU_VERSION_DEFAULT_VALUE = true;

        public const bool AUTO_LOAD_BINDING_CONFIG_ON_START_DEFAULT_VALUE = true;
        public const string BINDING_CONFIG_FILE_PATH_DEFAULT_VALUE = "vive_role_bindings.cfg";
        public const bool ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE = true;
        public const KeyCode BINDING_INTERFACE_SWITCH_KEY_DEFAULT_VALUE = KeyCode.B;
        public const KeyCode BINDING_INTERFACE_SWITCH_KEY_MODIFIER_DEFAULT_VALUE = KeyCode.RightShift;
        public const string BINDING_INTERFACE_PREFAB_DEFAULT_RESOURCE_PATH = "VIUBindingInterface";

        public const bool AUTO_LOAD_EXTERNAL_CAMERA_CONFIG_ON_START_DEFAULT_VALUE = true;
        public const string EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE = "externalcamera.cfg";
        public const bool ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE = true;
        public const KeyCode EXTERNAL_CAMERA_SWITCH_KEY_DEFAULT_VALUE = KeyCode.M;
        public const KeyCode EXTERNAL_CAMERA_SWITCH_KEY_MODIFIER_DEFAULT_VALUE = KeyCode.RightShift;

        public const bool ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE = false;
        public const bool SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE = true;
        public const bool ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE = true;
        public const bool SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE = true;
        public const float SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE = 1.5f;
        public const float SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE = 90f;
        public const float SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE = 90f;

        public const bool ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE = true;
        public const bool DAYDREAM_SYNC_PAD_PRESS_TO_TRIGGER_DEFAULT_VALUE = true;

        public const bool ACTIVATE_WAVE_VR_MODULE_DEFAULT_VALUE = true;
        public const bool SIMULATE_WAVE_VR_6DOF_CONTROLLER_DEFAULT_VALUE = false;
        public const bool WAVE_VR_ADD_VIRTUAL_ARM_TO_3DOF_CONTROLLER = true;
        public static readonly Vector3 WAVE_VR_VIRTUAL_NECK_POSITION_DEFAULT_VALUE = new Vector3(0.0f, -0.15f, 0.0f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_ELBOW_REST_POSITION_DEFAULT_VALUE = new Vector3(0.195f, -0.5f, 0.005f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_ARM_EXTENSION_OFFSET_DEFAULT_VALUE = new Vector3(-0.13f, 0.14f, 0.08f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_WRIST_REST_POSITION_DEFAULT_VALUE = new Vector3(0.0f, 0.0f, 0.35f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_HAND_REST_POSITION_DEFAULT_VALUE = new Vector3(0.0f, 0.0f, 0.05f);

        public const bool ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE = true;
        public const bool ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE = true;
        public const bool ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE = true;

        private static VIUSettings s_instance = null;

        [SerializeField]
        private bool m_autoCheckNewVIUVersion = AUTO_CHECK_NEW_VIU_VERSION_DEFAULT_VALUE;

        [SerializeField]
        private bool m_autoLoadBindingConfigOnStart = AUTO_LOAD_BINDING_CONFIG_ON_START_DEFAULT_VALUE;
        [SerializeField]
        private string m_bindingConfigFilePath = BINDING_CONFIG_FILE_PATH_DEFAULT_VALUE;
        [SerializeField, Tooltip(BIND_UI_SWITCH_TOOLTIP)]
        private bool m_enableBindingInterfaceSwitch = ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE;
        [SerializeField]
        private KeyCode m_bindingInterfaceSwitchKey = BINDING_INTERFACE_SWITCH_KEY_DEFAULT_VALUE;
        [SerializeField]
        private KeyCode m_bindingInterfaceSwitchKeyModifier = BINDING_INTERFACE_SWITCH_KEY_MODIFIER_DEFAULT_VALUE;
        [SerializeField]
        private GameObject m_bindingInterfaceObjectSource = null;

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

        [SerializeField]
        private bool m_simulatorAutoTrackMainCamera = SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATE_TRACKPAD_TOUCH_TOOLTIP)]
        private bool m_simulateTrackpadTouch = SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableSimulatorKeyboardMouseControl = ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATOR_KEY_MOVE_SPEED_TOOLTIP)]
        private float m_simulatorKeyMoveSpeed = SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE;
        [SerializeField]
        private float m_simulatorMouseRotateSpeed = SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATOR_KEY_MOVE_SPEED_TOOLTIP)]
        private float m_simulatorKeyRotateSpeed = SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE;

        [SerializeField]
        private bool m_activateGoogleVRModule = ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_daydreamSyncPadPressToTrigger = DAYDREAM_SYNC_PAD_PRESS_TO_TRIGGER_DEFAULT_VALUE;

        [SerializeField]
        private bool m_activateWaveVRModule = ACTIVATE_WAVE_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_simulateWaveVR6DoFController = SIMULATE_WAVE_VR_6DOF_CONTROLLER_DEFAULT_VALUE;
        [SerializeField]
        private bool m_waveVRAddVirtualArmTo3DoFController = WAVE_VR_ADD_VIRTUAL_ARM_TO_3DOF_CONTROLLER;
        [SerializeField]
        private Vector3 m_waveVRVirtualNeckPosition = WAVE_VR_VIRTUAL_NECK_POSITION_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualElbowRestPosition = WAVE_VR_VIRTUAL_ELBOW_REST_POSITION_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualArmExtensionOffset = WAVE_VR_VIRTUAL_ARM_EXTENSION_OFFSET_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualWristRestPosition = WAVE_VR_VIRTUAL_WRIST_REST_POSITION_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualHandRestPosition = WAVE_VR_VIRTUAL_HAND_REST_POSITION_DEFAULT_VALUE;

        [SerializeField]
        private bool m_activateSimulatorModule = ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateUnityNativeVRModule = ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateSteamVRModule = ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateOculusVRModule = ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE;

        public static bool autoCheckNewVIUVersion { get { return Instance == null ? AUTO_CHECK_NEW_VIU_VERSION_DEFAULT_VALUE : s_instance.m_autoCheckNewVIUVersion; } set { if (Instance != null) { Instance.m_autoCheckNewVIUVersion = value; } } }

        public static bool autoLoadBindingConfigOnStart { get { return Instance == null ? AUTO_LOAD_BINDING_CONFIG_ON_START_DEFAULT_VALUE : s_instance.m_autoLoadBindingConfigOnStart; } set { if (Instance != null) { Instance.m_autoLoadBindingConfigOnStart = value; } } }
        public static string bindingConfigFilePath { get { return Instance == null ? BINDING_CONFIG_FILE_PATH_DEFAULT_VALUE : s_instance.m_bindingConfigFilePath; } set { if (Instance != null) { Instance.m_bindingConfigFilePath = value; } } }
        public static bool enableBindingInterfaceSwitch { get { return Instance == null ? ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE : s_instance.m_enableBindingInterfaceSwitch; } set { if (Instance != null) { Instance.m_enableBindingInterfaceSwitch = value; } } }
        public static KeyCode bindingInterfaceSwitchKey { get { return Instance == null ? BINDING_INTERFACE_SWITCH_KEY_DEFAULT_VALUE : s_instance.m_bindingInterfaceSwitchKey; } set { if (Instance != null) { Instance.m_bindingInterfaceSwitchKey = value; } } }
        public static KeyCode bindingInterfaceSwitchKeyModifier { get { return Instance == null ? BINDING_INTERFACE_SWITCH_KEY_DEFAULT_VALUE : s_instance.m_bindingInterfaceSwitchKeyModifier; } set { if (Instance != null) { Instance.m_bindingInterfaceSwitchKeyModifier = value; } } }
        public static GameObject bindingInterfaceObjectSource { get { return Instance == null ? null : s_instance.m_bindingInterfaceObjectSource; } set { if (Instance != null) { Instance.m_bindingInterfaceObjectSource = value; } } }

        public static bool autoLoadExternalCameraConfigOnStart { get { return Instance == null ? AUTO_LOAD_EXTERNAL_CAMERA_CONFIG_ON_START_DEFAULT_VALUE : s_instance.m_autoLoadExternalCameraConfigOnStart; } set { if (Instance != null) { Instance.m_autoLoadExternalCameraConfigOnStart = value; } } }
        public static string externalCameraConfigFilePath { get { return Instance == null ? EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE : s_instance.m_externalCameraConfigFilePath; } set { if (Instance != null) { Instance.m_externalCameraConfigFilePath = value; } } }
        public static bool enableExternalCameraSwitch { get { return Instance == null ? ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE : s_instance.m_enableExternalCameraSwitch; } set { if (Instance != null) { Instance.m_enableExternalCameraSwitch = value; } } }
        public static KeyCode externalCameraSwitchKey { get { return Instance == null ? EXTERNAL_CAMERA_SWITCH_KEY_DEFAULT_VALUE : s_instance.m_externalCameraSwitchKey; } set { if (Instance != null) { Instance.m_externalCameraSwitchKey = value; } } }
        public static KeyCode externalCameraSwitchKeyModifier { get { return Instance == null ? EXTERNAL_CAMERA_SWITCH_KEY_MODIFIER_DEFAULT_VALUE : s_instance.m_externalCameraSwitchKeyModifier; } set { if (Instance != null) { Instance.m_externalCameraSwitchKeyModifier = value; } } }

        public static bool enableSimulatorKeyboardMouseControl { get { return Instance == null ? ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE : s_instance.m_enableSimulatorKeyboardMouseControl; } set { if (Instance != null) { Instance.m_enableSimulatorKeyboardMouseControl = value; } } }
        public static bool simulatorAutoTrackMainCamera { get { return Instance == null ? SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE : s_instance.m_simulatorAutoTrackMainCamera; } set { if (Instance != null) { Instance.m_simulatorAutoTrackMainCamera = value; } } }
        public static bool simulateTrackpadTouch { get { return Instance == null ? SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE : s_instance.m_simulateTrackpadTouch; } set { if (Instance != null) { Instance.m_simulateTrackpadTouch = value; } } }
        public static float simulatorKeyMoveSpeed { get { return Instance == null ? SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorKeyMoveSpeed; } set { if (Instance != null) { Instance.m_simulatorKeyMoveSpeed = value; } } }
        public static float simulatorMouseRotateSpeed { get { return Instance == null ? SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorMouseRotateSpeed; } set { if (Instance != null) { Instance.m_simulatorMouseRotateSpeed = value; } } }
        public static float simulatorKeyRotateSpeed { get { return Instance == null ? SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorKeyRotateSpeed; } set { if (Instance != null) { Instance.m_simulatorKeyRotateSpeed = value; } } }

        public static bool activateGoogleVRModule { get { return Instance == null ? ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateGoogleVRModule; } set { if (Instance != null) { Instance.m_activateGoogleVRModule = value; } } }
        public static bool daydreamSyncPadPressToTrigger { get { return Instance == null ? DAYDREAM_SYNC_PAD_PRESS_TO_TRIGGER_DEFAULT_VALUE : s_instance.m_daydreamSyncPadPressToTrigger; } set { if (Instance != null) { Instance.m_daydreamSyncPadPressToTrigger = value; } } }

        public static bool activateWaveVRModule { get { return Instance == null ? ACTIVATE_WAVE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateWaveVRModule; } set { if (Instance != null) { Instance.m_activateWaveVRModule = value; } } }
        public static bool simulateWaveVR6DofController { get { return Instance == null ? SIMULATE_WAVE_VR_6DOF_CONTROLLER_DEFAULT_VALUE : s_instance.m_simulateWaveVR6DoFController; } set { if (Instance != null) { Instance.m_simulateWaveVR6DoFController = value; } } }
        public static bool waveVRAddVirtualArmTo3DoFController { get { return Instance == null ? WAVE_VR_ADD_VIRTUAL_ARM_TO_3DOF_CONTROLLER : s_instance.m_waveVRAddVirtualArmTo3DoFController; } set { if (Instance != null) { Instance.m_waveVRAddVirtualArmTo3DoFController = value; } } }
        public static Vector3 waveVRVirtualNeckPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_NECK_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualNeckPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualNeckPosition = value; } } }
        public static Vector3 waveVRVirtualElbowRestPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_ELBOW_REST_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualElbowRestPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualElbowRestPosition = value; } } }
        public static Vector3 waveVRVirtualArmExtensionOffset { get { return Instance == null ? WAVE_VR_VIRTUAL_ARM_EXTENSION_OFFSET_DEFAULT_VALUE : s_instance.m_waveVRVirtualArmExtensionOffset; } set { if (Instance != null) { Instance.m_waveVRVirtualArmExtensionOffset = value; } } }
        public static Vector3 waveVRVirtualWristRestPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_WRIST_REST_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualWristRestPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualWristRestPosition = value; } } }
        public static Vector3 waveVRVirtualHandRestPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_HAND_REST_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualHandRestPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualHandRestPosition = value; } } }

        public static bool activateSimulatorModule { get { return Instance == null ? ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE : s_instance.m_activateSimulatorModule; } set { if (Instance != null) { Instance.m_activateSimulatorModule = value; } } }
        public static bool activateUnityNativeVRModule { get { return Instance == null ? ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateUnityNativeVRModule; } set { if (Instance != null) { Instance.m_activateUnityNativeVRModule = value; } } }
        public static bool activateSteamVRModule { get { return Instance == null ? ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateSteamVRModule; } set { if (Instance != null) { Instance.m_activateSteamVRModule = value; } } }
        public static bool activateOculusVRModule { get { return Instance == null ? ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateOculusVRModule; } set { if (Instance != null) { Instance.m_activateOculusVRModule = value; } } }

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