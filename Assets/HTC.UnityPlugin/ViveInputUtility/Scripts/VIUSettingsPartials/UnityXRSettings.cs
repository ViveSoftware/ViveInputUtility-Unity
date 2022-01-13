//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_UNITY_XR_MODULE_DEFAULT_VALUE = true;
        public const bool PREFER_UNITY_XR_POINTER_POSE_DEFAULT_VALUE = false;

        [SerializeField]
        private bool m_activateUnityXRModule = ACTIVATE_UNITY_XR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_preferUnityXRPointerPose = PREFER_UNITY_XR_POINTER_POSE_DEFAULT_VALUE;

        public static bool activateUnityXRModule { get { return Instance == null ? ACTIVATE_UNITY_XR_MODULE_DEFAULT_VALUE : s_instance.m_activateUnityXRModule; } set { if (Instance != null) { Instance.m_activateUnityXRModule = value; } } }
        public static bool preferUnityXRPointerPose { get { return Instance == null ? PREFER_UNITY_XR_POINTER_POSE_DEFAULT_VALUE : s_instance.m_preferUnityXRPointerPose; } set { if (Instance != null) { Instance.m_preferUnityXRPointerPose = value; } } }
    }
}