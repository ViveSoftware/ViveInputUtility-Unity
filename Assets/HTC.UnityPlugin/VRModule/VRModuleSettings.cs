//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModuleSettings : ScriptableObject
    {
        public const string DEFAULT_RESOURCE_PATH = "VRModuleSettings";
        public const string INITIALIZE_ON_STARTUP_TOOLTIP = "Auto initialize VIU core manager at the run time. If disabled and no VIU component used in the scene, manually calling VRModule.Initialize() is required if tempting to use VIUSyntheticDevice as Input System device or binding source.";
        public const bool INITIALIZE_ON_STARTUP_DEFAULT_VALUE = false;

        [SerializeField, Tooltip(INITIALIZE_ON_STARTUP_TOOLTIP)]
        private bool m_initializeOnStartup = INITIALIZE_ON_STARTUP_DEFAULT_VALUE;
        public static bool initializeOnStartup { get { return Instance == null ? INITIALIZE_ON_STARTUP_DEFAULT_VALUE : s_instance.m_initializeOnStartup; } set { if (Instance != null) { Instance.m_initializeOnStartup = value; } } }

        private static VRModuleSettings s_instance = null;

        public static VRModuleSettings Instance
        {
            get
            {
                if (s_instance == null)
                {
                    LoadFromResource();
                }

                return s_instance;
            }
        }

        public static void LoadFromResource(string path = null)
        {
            if (path == null)
            {
                path = DEFAULT_RESOURCE_PATH;
            }

            if ((s_instance = Resources.Load<VRModuleSettings>(path)) == null)
            {
                s_instance = CreateInstance<VRModuleSettings>();
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}