//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public enum SteamVRSkeletonType
    {
        WithController,
        WithoutController,
    }

    [Serializable]
    public class SteamVRSkeletonSetting
    {
        public SteamVRSkeletonType type = SteamVRSkeletonType.WithController;
        public bool showController = true;
        public bool showHand = false;
    }

    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE = true;

        [SerializeField] private bool m_activateSteamVRModule = ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE;
        [SerializeField] private SteamVRSkeletonSetting m_steamVRLeftSkeletonSetting;
        [SerializeField] private SteamVRSkeletonSetting m_steamVRRightSkeletonSetting;

        public static bool activateSteamVRModule { get { return Instance == null ? ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateSteamVRModule; } set { if (Instance != null) { Instance.m_activateSteamVRModule = value; } } }

        public static SteamVRSkeletonSetting steamVRLeftSkeletonSetting
        {
            get
            {
                if (Instance == null)
                {
                    return default(SteamVRSkeletonSetting);
                }

                if (Instance.m_steamVRLeftSkeletonSetting == null)
                {
                    Instance.m_steamVRLeftSkeletonSetting = new SteamVRSkeletonSetting();
                }

                return Instance.m_steamVRLeftSkeletonSetting;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_steamVRLeftSkeletonSetting = value;
                }
            }
        }

        public static SteamVRSkeletonSetting steamVRRightSkeletonSetting
        {
            get 
            {
                if (Instance == null)
                {
                    return default(SteamVRSkeletonSetting);
                }

                if (Instance.m_steamVRRightSkeletonSetting == null)
                {
                    Instance.m_steamVRRightSkeletonSetting = new SteamVRSkeletonSetting();
                }

                return Instance.m_steamVRRightSkeletonSetting;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_steamVRRightSkeletonSetting = value;
                }
            }
        }
    }
}