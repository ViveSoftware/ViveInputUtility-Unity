//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using HTC.UnityPlugin.VRModuleManagement;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const string SIMULATE_TRACKPAD_TOUCH_TOOLTIP = "Hold Shift key and move the mouse to simulate trackpad touching event";
        public const string SIMULATOR_KEY_MOVE_SPEED_TOOLTIP = "W/A/S/D";
        public const string SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP = "Arrow Up/Down/Left/Right";

        public const bool ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE = false;
        public const bool SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE = true;
        public const bool ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE = true;
        public const bool SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE = true;
        public const float SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE = 1.5f;
        public const float SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE = 90f;
        public const float SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE = 90f;
        public const VRModuleDeviceModel SIMULATOR_CONTROLLER_MODEL_DEFAULT_VALUE = VRModuleDeviceModel.ViveController;
        public const VRModuleDeviceModel SIMULATOR_OTHER_MODEL_DEFAULT_VALUE = VRModuleDeviceModel.ViveTracker;

        [SerializeField]
        private bool m_activateSimulatorModule = ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE;
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
        [SerializeField, Tooltip(SIMULATOR_KEY_MOVE_SPEED_TOOLTIP)]
        private VRModuleDeviceModel m_simulatorLeftControllerModel = SIMULATOR_CONTROLLER_MODEL_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATOR_KEY_MOVE_SPEED_TOOLTIP)]
        private VRModuleDeviceModel m_simulatorRightControllerModel = SIMULATOR_CONTROLLER_MODEL_DEFAULT_VALUE;
        [SerializeField, Tooltip(SIMULATOR_KEY_MOVE_SPEED_TOOLTIP)]
        private VRModuleDeviceModel m_simulatorOtherModel = SIMULATOR_OTHER_MODEL_DEFAULT_VALUE;

        public static bool activateSimulatorModule { get { return Instance == null ? ACTIVATE_SIMULATOR_MODULE_DEFAULT_VALUE : s_instance.m_activateSimulatorModule; } set { if (Instance != null) { Instance.m_activateSimulatorModule = value; } } }
        public static bool enableSimulatorKeyboardMouseControl { get { return Instance == null ? ENABLE_SIMULATOR_KEYBOARD_MOUSE_CONTROL_DEFAULT_VALUE : s_instance.m_enableSimulatorKeyboardMouseControl; } set { if (Instance != null) { Instance.m_enableSimulatorKeyboardMouseControl = value; } } }
        public static bool simulatorAutoTrackMainCamera { get { return Instance == null ? SIMULATOR_AUTO_TRACK_MAIN_CAMERA_DEFAULT_VALUE : s_instance.m_simulatorAutoTrackMainCamera; } set { if (Instance != null) { Instance.m_simulatorAutoTrackMainCamera = value; } } }
        public static bool simulateTrackpadTouch { get { return Instance == null ? SIMULATE_TRACKPAD_TOUCH_DEFAULT_VALUE : s_instance.m_simulateTrackpadTouch; } set { if (Instance != null) { Instance.m_simulateTrackpadTouch = value; } } }
        public static float simulatorKeyMoveSpeed { get { return Instance == null ? SIMULATOR_KEY_MOVE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorKeyMoveSpeed; } set { if (Instance != null) { Instance.m_simulatorKeyMoveSpeed = value; } } }
        public static float simulatorMouseRotateSpeed { get { return Instance == null ? SIMULATOR_MOUSE_ROTATE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorMouseRotateSpeed; } set { if (Instance != null) { Instance.m_simulatorMouseRotateSpeed = value; } } }
        public static float simulatorKeyRotateSpeed { get { return Instance == null ? SIMULATOR_KEY_ROTATE_SPEED_DEFAULT_VALUE : s_instance.m_simulatorKeyRotateSpeed; } set { if (Instance != null) { Instance.m_simulatorKeyRotateSpeed = value; } } }
        public static VRModuleDeviceModel simulatorLeftControllerModel { get { return Instance == null ? SIMULATOR_CONTROLLER_MODEL_DEFAULT_VALUE : s_instance.m_simulatorLeftControllerModel; } set { if (Instance != null) { Instance.m_simulatorLeftControllerModel = value; } } }
        public static VRModuleDeviceModel simulatorRightControllerModel { get { return Instance == null ? SIMULATOR_CONTROLLER_MODEL_DEFAULT_VALUE : s_instance.m_simulatorRightControllerModel; } set { if (Instance != null) { Instance.m_simulatorRightControllerModel = value; } } }
        public static VRModuleDeviceModel simulatorOtherModel { get { return Instance == null ? SIMULATOR_CONTROLLER_MODEL_DEFAULT_VALUE : s_instance.m_simulatorOtherModel; } set { if (Instance != null) { Instance.m_simulatorOtherModel = value; } } }
    }
}