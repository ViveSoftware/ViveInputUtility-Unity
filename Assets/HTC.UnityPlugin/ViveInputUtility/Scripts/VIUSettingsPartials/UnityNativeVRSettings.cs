//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateUnityNativeVRModule = ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE;

        public static bool activateUnityNativeVRModule { get { return Instance == null ? ACTIVATE_UNITY_NATIVE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateUnityNativeVRModule; } set { if (Instance != null) { Instance.m_activateUnityNativeVRModule = value; } } }
    }
}