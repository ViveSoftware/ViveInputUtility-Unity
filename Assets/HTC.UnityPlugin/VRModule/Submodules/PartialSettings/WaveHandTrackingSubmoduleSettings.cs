//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModuleSettings : ScriptableObject
    {
        public const bool ACTIVATE_WAVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE = true;
        public const bool ENABLE_WAVE_HAND_GESTURE_DEFAULT_VALUE = true;
        public const bool ENABLE_WAVE_HAND_TRACKING_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateWaveHandTrackingSubmodule = ACTIVATE_WAVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableWaveHandGesture = ENABLE_WAVE_HAND_GESTURE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_enableWaveHandTracking = ENABLE_WAVE_HAND_TRACKING_DEFAULT_VALUE;

        public static bool activateWaveHandTrackingSubmodule { get { return Instance == null ? ACTIVATE_WAVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE : s_instance.m_activateWaveHandTrackingSubmodule; } set { if (Instance != null) { Instance.m_activateWaveHandTrackingSubmodule = value; } } }
        public static bool enableWaveHandGesture { get { return Instance == null ? ENABLE_WAVE_HAND_GESTURE_DEFAULT_VALUE : s_instance.m_enableWaveHandGesture; } set { if (Instance != null) { Instance.m_enableWaveHandGesture = value; } } }
        public static bool enableWaveHandTracking { get { return Instance == null ? ENABLE_WAVE_HAND_TRACKING_DEFAULT_VALUE : s_instance.m_enableWaveHandTracking; } set { if (Instance != null) { Instance.m_enableWaveHandTracking = value; } } }
    }
}