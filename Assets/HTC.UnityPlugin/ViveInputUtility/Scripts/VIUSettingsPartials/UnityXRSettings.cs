//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_UNITY_XR_MODULE_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateUnityXRModule = ACTIVATE_UNITY_XR_MODULE_DEFAULT_VALUE;

        public static bool activateUnityXRModule
        {
            get
            {
                return Instance == null ? ACTIVATE_UNITY_XR_MODULE_DEFAULT_VALUE : s_instance.m_activateUnityXRModule;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_activateUnityXRModule = value;
                }
            }
        }
    }
}