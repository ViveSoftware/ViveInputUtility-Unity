//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE = true;
        public const bool DAYDREAM_SYNC_PAD_PRESS_TO_TRIGGER_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateGoogleVRModule = ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_daydreamSyncPadPressToTrigger = DAYDREAM_SYNC_PAD_PRESS_TO_TRIGGER_DEFAULT_VALUE;

        public static bool activateGoogleVRModule { get { return Instance == null ? ACTIVATE_GOOGLE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateGoogleVRModule; } set { if (Instance != null) { Instance.m_activateGoogleVRModule = value; } } }
        public static bool daydreamSyncPadPressToTrigger { get { return Instance == null ? DAYDREAM_SYNC_PAD_PRESS_TO_TRIGGER_DEFAULT_VALUE : s_instance.m_daydreamSyncPadPressToTrigger; } set { if (Instance != null) { Instance.m_daydreamSyncPadPressToTrigger = value; } } }
    }
}