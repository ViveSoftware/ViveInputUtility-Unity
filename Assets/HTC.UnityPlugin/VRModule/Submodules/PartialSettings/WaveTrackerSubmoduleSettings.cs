//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModuleSettings : ScriptableObject
    {
        public const bool ACTIVATE_WAVE_TRACKER_SUBMODULE_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateWaveTrackerSubmodule = ACTIVATE_WAVE_TRACKER_SUBMODULE_DEFAULT_VALUE;

        public static bool activateWaveTrackerSubmodule { get { return Instance == null ? ACTIVATE_WAVE_TRACKER_SUBMODULE_DEFAULT_VALUE : s_instance.m_activateWaveTrackerSubmodule; } set { if (Instance != null) { Instance.m_activateWaveTrackerSubmodule = value; } } }
    }
}