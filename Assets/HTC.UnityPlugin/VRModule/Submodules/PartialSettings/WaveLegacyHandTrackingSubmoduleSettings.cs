//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModuleSettings : ScriptableObject
    {
        public const bool ACTIVATE_WAVE_LEGACY_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE = true;
        public const bool ENABLE_WAVE_LEGACY_HAND_GESTURE_DEFAULT_VALUE = true;
        public const bool ENABLE_WAVE_LEGACY_HAND_TRACKING_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateWaveLegacyHandTrackingSubmodule = ACTIVATE_WAVE_LEGACY_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableWaveLegacyHandGesture = ENABLE_WAVE_LEGACY_HAND_GESTURE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableWaveLegacyHandTracking = ENABLE_WAVE_LEGACY_HAND_TRACKING_DEFAULT_VALUE;

        public static bool activateWaveLegacyHandTrackingSubmodule { get { return Instance == null ? ACTIVATE_WAVE_LEGACY_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE : s_instance.m_activateWaveLegacyHandTrackingSubmodule; } set { if (Instance != null) { Instance.m_activateWaveLegacyHandTrackingSubmodule = value; } } }
        public static bool enableWaveLegacyHandGesture { get { return Instance == null ? ENABLE_WAVE_LEGACY_HAND_GESTURE_DEFAULT_VALUE : s_instance.m_enableWaveLegacyHandGesture; } set { if (Instance != null) { Instance.m_enableWaveLegacyHandGesture = value; } } }
        public static bool enableWaveLegacyHandTracking { get { return Instance == null ? ENABLE_WAVE_LEGACY_HAND_TRACKING_DEFAULT_VALUE : s_instance.m_enableWaveLegacyHandTracking; } set { if (Instance != null) { Instance.m_enableWaveLegacyHandTracking = value; } } }
    }
}