//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class VIUSettings : ScriptableObject
    {
        public const string DEFAULT_RESOURCE_PATH = "VIUSettings";

        public const string BIND_UI_SWITCH_TOOLTIP = "When enabled, pressing RightShift + B to open the binding interface in play mode.";
        public const string EX_CAM_UI_SWITCH_TOOLTIP = "When enabled, pressing RightShift + M to toggle the quad view while external camera config file exist.";
        public const string SIMULATE_TRACKPAD_TOUCH_TOOLTIP = "Hold Shift key and move the mouse to simulate trackpad touching event";
        public const string SIMULATOR_KEY_MOVE_SPEED_TOOLTIP = "W/A/S/D";
        public const string SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP = "Arrow Up/Down/Left/Right";

        public const bool ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE = false;
        public const bool ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE = false;
        public const bool ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE = true;
        public const bool ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE = true;
        public const bool ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE = true;
        public const bool ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE = true;
        public const bool ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE = true;
        public const bool SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE = true;
        public const bool ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE = true;
        public const bool SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE = true;
        public const float SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE = 1.5f;
        public const float SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE = 90f;
        public const float SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE = 90f;

        private static VIUSettings s_instance = null;

        [SerializeField, Tooltip(BIND_UI_SWITCH_TOOLTIP)]
        private bool m_enableBindingInterfaceSwitch = ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE;
        [SerializeField, Tooltip(EX_CAM_UI_SWITCH_TOOLTIP)]
        private bool m_enableExternalCameraSwitch = ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateSimulatorModule = ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateUnityNativeVRModule = ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateSteamVRModule = ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateOculusVRModule = ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_activateGoogleVRModule = ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableSimulatorKeyboardMouseControl = ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE;
        [SerializeField]
        private bool m_simulatorAutoTrackMainCamera = SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATE_TRACKPAD_TOUCH_TOOLTIP)]
        private bool m_simulateTrackpadTouch = SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATOR_KEY_MOVE_SPEED_TOOLTIP)]
        private float m_simulatorKeyMoveSpeed = SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE;
        [SerializeField]
        private float m_simulatorMouseRotateSpeed = SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATOR_KEY_MOVE_SPEED_TOOLTIP)]
        private float m_simulatorKeyRotateSpeed = SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE;

        public static bool enableBindingInterfaceSwitch { get { return Instance == null ? ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE : s_instance.m_enableBindingInterfaceSwitch; } set { if (Instance != null) { Instance.m_enableBindingInterfaceSwitch = value; } } }

        public static bool enableExternalCameraSwitch { get { return Instance == null ? ENABLE_EXTERNAL_CAMERA_SWITCH_DEFAULT_VALUE : s_instance.m_enableExternalCameraSwitch; } set { if (Instance != null) { Instance.m_enableExternalCameraSwitch = value; } } }

        public static bool activateSimulatorModule { get { return Instance == null ? ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE : s_instance.m_activateSimulatorModule; } set { if (Instance != null) { Instance.m_activateSimulatorModule = value; } } }

        public static bool activateUnityNativeVRModule { get { return Instance == null ? ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateUnityNativeVRModule; } set { if (Instance != null) { Instance.m_activateUnityNativeVRModule = value; } } }

        public static bool activateSteamVRModule { get { return Instance == null ? ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateSteamVRModule; } set { if (Instance != null) { Instance.m_activateSteamVRModule = value; } } }

        public static bool activateOculusVRModule { get { return Instance == null ? ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateOculusVRModule; } set { if (Instance != null) { Instance.m_activateOculusVRModule = value; } } }

        public static bool activateGoogleVRModule { get { return Instance == null ? ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateGoogleVRModule; } set { if (Instance != null) { Instance.m_activateGoogleVRModule = value; } } }

        public static bool enableSimulatorKeyboardMouseControl { get { return Instance == null ? ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE : s_instance.m_enableSimulatorKeyboardMouseControl; } set { if (Instance != null) { Instance.m_enableSimulatorKeyboardMouseControl = value; } } }

        public static bool simulatorAutoTrackMainCamera { get { return Instance == null ? SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE : s_instance.m_simulatorAutoTrackMainCamera; } set { if (Instance != null) { Instance.m_simulatorAutoTrackMainCamera = value; } } }

        public static bool simulateTrackpadTouch { get { return Instance == null ? SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE : s_instance.m_simulateTrackpadTouch; } set { if (Instance != null) { Instance.m_simulateTrackpadTouch = value; } } }

        public static float simulatorKeyMoveSpeed { get { return Instance == null ? SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorKeyMoveSpeed; } set { if (Instance != null) { Instance.m_simulatorKeyMoveSpeed = value; } } }

        public static float simulatorMouseRotateSpeed { get { return Instance == null ? SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorMouseRotateSpeed; } set { if (Instance != null) { Instance.m_simulatorMouseRotateSpeed = value; } } }

        public static float simulatorKeyRotateSpeed { get { return Instance == null ? SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorKeyRotateSpeed; } set { if (Instance != null) { Instance.m_simulatorKeyRotateSpeed = value; } } }

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