//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateOculusVRModule = ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE;

        public static bool activateOculusVRModule { get { return Instance == null ? ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateOculusVRModule; } set { if (Instance != null) { Instance.m_activateOculusVRModule = value; } } }
    }
}