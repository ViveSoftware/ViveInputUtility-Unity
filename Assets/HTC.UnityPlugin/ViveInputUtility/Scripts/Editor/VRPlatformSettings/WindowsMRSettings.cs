//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace HTC.UnityPlugin.Vive
{
    public class WindowsMRRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
        public WindowsMRRecommendedSettings()
        {
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Virtual Reality Supported with Windows XR",
                skipCheckFunc = () => !VIUSettingsEditor.canSupportWindowsMR,
                currentValueFunc = () => VIUSettingsEditor.supportWindowsMR,
                setValueFunc = v => VIUSettingsEditor.supportWindowsMR = v,
                recommendedValue = true,
            });
        }
    }

    public static partial class VIUSettingsEditor
    {
        private const string WINDOWSMR_PACKAGE_NAME = "com.unity.xr.windowsmr.metro";
        private const string WINDOWSMR_XR_PACKAGE_NAME = "com.unity.xr.windowsmr";
        public const string WINDOWSMR_XR_LOADER_NAME = "Windows MR Loader";
        public const string WINDOWSMR_XR_LOADER_CLASS_NAME = "WindowsMRLoader";

        public static bool canSupportWindowsMR
        {
            get { return WindowsMRSettings.instance.canSupport; }
        }

        public static bool supportWindowsMR
        {
            get { return WindowsMRSettings.instance.support; }
            set { WindowsMRSettings.instance.support = value; }
        }

        private class WindowsMRSettings : VRPlatformSetting
        {
            private Foldouter m_foldouter = new Foldouter();

            public static WindowsMRSettings instance { get; private set; }

            public WindowsMRSettings() { instance = this; }

            public override int order { get { return 3; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Standalone; } }

            public override bool canSupport
            {
                get
                {
#if UNITY_2019_3_OR_NEWER
                    return activeBuildTargetGroup == BuildTargetGroup.Standalone && (PackageManagerHelper.IsPackageInList(WINDOWSMR_XR_PACKAGE_NAME) || PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME));
#elif UNITY_2018_2_OR_NEWER
                    return activeBuildTargetGroup == BuildTargetGroup.Standalone && PackageManagerHelper.IsPackageInList(WINDOWSMR_PACKAGE_NAME);
#else
                    return false;
#endif
                }
            }

            public override bool support
            {
                get
                {
#if UNITY_2019_3_OR_NEWER
                    return canSupport && VIUSettings.activateUnityXRModule && (XRPluginManagementUtils.IsXRLoaderEnabled(UnityXRModule.OPENXR_LOADER_NAME, requirdPlatform) || XRPluginManagementUtils.IsXRLoaderEnabled(WINDOWSMR_XR_LOADER_NAME, requirdPlatform));
#elif UNITY_2018_2_OR_NEWER
                    return canSupport && VIUSettings.activateUnityNativeVRModule && WindowsMRSDK.enabled;
#else
                    return false;
#endif
                }
                set
                {
                    if (support == value) { return; }
#if UNITY_2019_3_OR_NEWER
                    if (PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME))
                    {
                        XRPluginManagementUtils.SetXRLoaderEnabled(UnityXRModule.OPENXR_LOADER_CLASS_NAME, requirdPlatform, value);
                    }
                    else if (PackageManagerHelper.IsPackageInList(WINDOWSMR_PACKAGE_NAME))
                    {
                        XRPluginManagementUtils.SetXRLoaderEnabled(WINDOWSMR_XR_LOADER_CLASS_NAME, requirdPlatform, value);
                    }

                    VIUSettings.activateUnityXRModule = XRPluginManagementUtils.IsAnyXRLoaderEnabled(requirdPlatform);
#elif UNITY_2018_2_OR_NEWER
                    WindowsMRSDK.enabled = value;
                    VIUSettings.activateUnityNativeVRModule = value;
#endif
                }
            }

            public override void OnPreferenceGUI()
            {
                const string title = "Windows MR";
                if (canSupport)
                {
                    var wasSupported = support;
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title, "Windows MR"), wasSupported);
                    s_symbolChanged |= wasSupported != support;
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();

                    if (activeBuildTargetGroup != BuildTargetGroup.Standalone)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Standalone platform required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    }
#if UNITY_2020_2_OR_NEWER && FALSE // openxr not fully supported yet
                    else if (!PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "OpenXR Plugin package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("OpenXR Plugin", OPENXR_PLUGIN_PACKAGE_NAME);
                    }
#elif UNITY_2019_3_OR_NEWER
                    else if (!PackageManagerHelper.IsPackageInList(WINDOWSMR_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Windows XR Plugin package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("Windows XR Plugin", WINDOWSMR_XR_PACKAGE_NAME);
                    }
#elif UNITY_2018_2_OR_NEWER
                    else if (!PackageManagerHelper.IsPackageInList(WINDOWSMR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Windows Mixed Reality package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("Windows Mixed Reality", WINDOWSMR_PACKAGE_NAME);
                    }
#endif

                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}