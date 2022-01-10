//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public enum SteamVRSkeletonMode
    {
        Disabled = -1,
        WithController,
        WithoutController,
    }

    public partial class VIUSettings : ScriptableObject
    {
        public const bool ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE = true;
        public const SteamVRSkeletonMode STEAM_VR_LEFT_SKELETON_MODE_DEFAULT_VALUE = SteamVRSkeletonMode.Disabled;
        public const SteamVRSkeletonMode STEAM_VR_RIGHT_SKELETON_MODE_DEFAULT_VALUE = SteamVRSkeletonMode.Disabled;

        [SerializeField] private bool m_activateSteamVRModule = ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE;
        [SerializeField] private SteamVRSkeletonMode m_steamVRLeftSkeletonMode = STEAM_VR_LEFT_SKELETON_MODE_DEFAULT_VALUE;
        [SerializeField] private SteamVRSkeletonMode m_steamVRRightSkeletonMode = STEAM_VR_RIGHT_SKELETON_MODE_DEFAULT_VALUE;

        public static bool activateSteamVRModule { get { return Instance == null ? ACTIVATE_STEAM_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateSteamVRModule; } set { if (Instance != null) { Instance.m_activateSteamVRModule = value; } } }
        public static SteamVRSkeletonMode steamVRLeftSkeletonMode { get { return Instance == null ? STEAM_VR_LEFT_SKELETON_MODE_DEFAULT_VALUE : s_instance.m_steamVRLeftSkeletonMode; } set { if (Instance != null) { Instance.m_steamVRLeftSkeletonMode = value; } } }
        public static SteamVRSkeletonMode steamVRRightSkeletonMode { get { return Instance == null ? STEAM_VR_RIGHT_SKELETON_MODE_DEFAULT_VALUE : s_instance.m_steamVRRightSkeletonMode; } set { if (Instance != null) { Instance.m_steamVRRightSkeletonMode = value; } } }
    }
}