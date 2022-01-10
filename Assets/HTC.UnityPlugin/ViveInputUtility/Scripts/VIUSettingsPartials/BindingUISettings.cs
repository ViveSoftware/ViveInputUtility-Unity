//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const string BIND_UI_SWITCH_TOOLTIP = "Enable this option to open binding interface by pressing the switch key in play mode.";

        public const bool AUTO_LOAD_BINDING_CONFIG_ON_START_DEFAULT_VALUE = true;
        public const string BINDING_CONFIG_FILE_PATH_DEFAULT_VALUE = "vive_role_bindings.cfg";
        public const bool ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE = true;
        public const KeyCode BINDING_INTERFACE_SWITCH_KEY_DEFAULT_VALUE = KeyCode.B;
        public const KeyCode BINDING_INTERFACE_SWITCH_KEY_MODIFIER_DEFAULT_VALUE = KeyCode.RightShift;
        public const string BINDING_INTERFACE_PREFAB_DEFAULT_RESOURCE_PATH = "VIUBindingInterface";

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

        public static bool autoLoadBindingConfigOnStart { get { return Instance == null ? AUTO_LOAD_BINDING_CONFIG_ON_START_DEFAULT_VALUE : s_instance.m_autoLoadBindingConfigOnStart; } set { if (Instance != null) { Instance.m_autoLoadBindingConfigOnStart = value; } } }
        public static string bindingConfigFilePath { get { return Instance == null ? BINDING_CONFIG_FILE_PATH_DEFAULT_VALUE : s_instance.m_bindingConfigFilePath; } set { if (Instance != null) { Instance.m_bindingConfigFilePath = value; } } }
        public static bool enableBindingInterfaceSwitch { get { return Instance == null ? ENABLE_BINDING_INTERFACE_SWITCH_DEFAULT_VALUE : s_instance.m_enableBindingInterfaceSwitch; } set { if (Instance != null) { Instance.m_enableBindingInterfaceSwitch = value; } } }
        public static KeyCode bindingInterfaceSwitchKey { get { return Instance == null ? BINDING_INTERFACE_SWITCH_KEY_DEFAULT_VALUE : s_instance.m_bindingInterfaceSwitchKey; } set { if (Instance != null) { Instance.m_bindingInterfaceSwitchKey = value; } } }
        public static KeyCode bindingInterfaceSwitchKeyModifier { get { return Instance == null ? BINDING_INTERFACE_SWITCH_KEY_DEFAULT_VALUE : s_instance.m_bindingInterfaceSwitchKeyModifier; } set { if (Instance != null) { Instance.m_bindingInterfaceSwitchKeyModifier = value; } } }
        public static GameObject bindingInterfaceObjectSource { get { return Instance == null ? null : s_instance.m_bindingInterfaceObjectSource; } set { if (Instance != null) { Instance.m_bindingInterfaceObjectSource = value; } } }
    }
}