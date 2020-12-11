//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public enum SteamVRSkeletonType
        {
            WithController,
            WithoutController,
        }

        public const bool ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE = true;

        [SerializeField] private bool m_activateSteamVRModule = ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE;
        [SerializeField] private SteamVRSkeletonType m_steamVRSkeletonType;

        public static bool activateSteamVRModule { get { return Instance == null ? ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateSteamVRModule; } set { if (Instance != null) { Instance.m_activateSteamVRModule = value; } } }

        public static SteamVRSkeletonType steamVRSkeletonType
        {
            get { return Instance != null ? Instance.m_steamVRSkeletonType : default(SteamVRSkeletonType); }
            set
            {
                if (Instance != null)
                {
                    Instance.m_steamVRSkeletonType = value;
                }
            }
        }
    }
}