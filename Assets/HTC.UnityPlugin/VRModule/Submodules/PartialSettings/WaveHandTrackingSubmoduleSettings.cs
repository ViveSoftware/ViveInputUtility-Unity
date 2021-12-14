//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public static readonly bool isWaveHandTrackingDetected =
#if VIU_WAVEVR_HAND_TRACKING
            true;
#else
            false;
#endif
    }

    public partial class VRModuleSettings : ScriptableObject
    {
        public const bool ACTIVATE_WAVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE = true;
        public const bool ENABLE_WAVE_HAND_GESTURE_DEFAULT_VALUE = true;
        public const bool ENABLE_WAVE_NATURAL_HAND_DEFAULT_VALUE = false;
        public const bool ENABLE_WAVE_ELECTRONIC_HAND_DEFAULT_VALUE = false;
        public const bool SHOW_WAVE_ELECTRONIC_HAND_WITH_CONTROLLER = false;

        [SerializeField]
        private bool m_activateWaveHandTrackingSubmodule = ACTIVATE_WAVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableWaveHandGesture = ENABLE_WAVE_HAND_GESTURE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableWaveNaturalHand = ENABLE_WAVE_NATURAL_HAND_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableWaveElectronicHand = ENABLE_WAVE_ELECTRONIC_HAND_DEFAULT_VALUE;
        [SerializeField]
        private bool m_showWaveElectronicHandWithController = SHOW_WAVE_ELECTRONIC_HAND_WITH_CONTROLLER;

        public static bool activateWaveHandTrackingSubmodule { get { return Instance == null ? ACTIVATE_WAVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE : s_instance.m_activateWaveHandTrackingSubmodule; } set { if (Instance != null) { Instance.m_activateWaveHandTrackingSubmodule = value; } } }
        public static bool enableWaveHandGesture { get { return Instance == null ? ENABLE_WAVE_HAND_GESTURE_DEFAULT_VALUE : s_instance.m_enableWaveHandGesture; } set { if (Instance != null) { Instance.m_enableWaveHandGesture = value; } } }
        public static bool enableWaveNaturalHand { get { return Instance == null ? ENABLE_WAVE_NATURAL_HAND_DEFAULT_VALUE : s_instance.m_enableWaveNaturalHand; } set { if (Instance != null) { Instance.m_enableWaveNaturalHand = value; } } }
        public static bool enableWaveElectronicHand { get { return Instance == null ? ENABLE_WAVE_ELECTRONIC_HAND_DEFAULT_VALUE : s_instance.m_enableWaveElectronicHand; } set { if (Instance != null) { Instance.m_enableWaveElectronicHand = value; } } }
        public static bool showWaveElectronicHandWithController { get { return Instance == null ? SHOW_WAVE_ELECTRONIC_HAND_WITH_CONTROLLER : s_instance.m_showWaveElectronicHandWithController; } set { if (Instance != null) { Instance.m_showWaveElectronicHandWithController = value; } } }
    }
}