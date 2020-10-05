//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_WAVE_UNITY_XR_MODULE_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateWaveUnityXRModule = ACTIVATE_WAVE_UNITY_XR_MODULE_DEFAULT_VALUE;

        public static bool activateWaveUnityXRModule { get { return Instance == null ? ACTIVATE_WAVE_UNITY_XR_MODULE_DEFAULT_VALUE : s_instance.m_activateWaveUnityXRModule; } set { if (Instance != null) { Instance.m_activateWaveUnityXRModule = value; } } }
    }
}