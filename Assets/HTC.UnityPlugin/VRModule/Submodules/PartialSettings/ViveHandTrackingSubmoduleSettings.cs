//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public static readonly bool isViveHandTrackingDetected =
#if VIU_VIVE_HAND_TRACKING
            true;
#else
            false;
#endif
    }

    public partial class VRModuleSettings : ScriptableObject
    {
        public const bool ACTIVATE_VIVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateViveHandTrackingSubmodule = ACTIVATE_VIVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE;

        public static bool activateViveHandTrackingSubmodule { get { return Instance == null ? ACTIVATE_VIVE_HAND_TRACKING_SUBMODULE_DEFAULT_VALUE : s_instance.m_activateViveHandTrackingSubmodule; } set { if (Instance != null) { Instance.m_activateViveHandTrackingSubmodule = value; } } }
    }
}