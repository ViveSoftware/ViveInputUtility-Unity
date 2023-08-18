using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_2018_1_OR_NEWER
using HTC.UnityPlugin.UPMRegistryTool.Editor.Utils;
using HTC.UnityPlugin.UPMRegistryTool.Editor.Configs;
#endif

#if VIU_XR_GENERAL_SETTINGS
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif

#if VIU_OPENXR
using UnityEngine.XR.OpenXR;
using System.Reflection;
using System;
#endif

#if VIU_WAVEXR_OPENXR
using Wave.OpenXR;
#endif

namespace HTC.UnityPlugin.Vive
{
#if VIU_OPENXR && VIU_WAVEXR_OPENXR
    public class OpenXRAndroidWaveRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
        public OpenXRAndroidWaveRecommendedSettings()
        {
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Add HTC Vive Focus3 Profile",
                skipCheckFunc = () => !VIUSettingsEditor.supportOpenXRAndroidWave,
                currentValueFunc = () =>
                {
                    var feature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<HTCViveFocus3Profile>();
                    return feature != null && feature.enabled;
                },
                setValueFunc = v =>
                {
                    var feature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<HTCViveFocus3Profile>();
                    if (feature != null) { feature.enabled = true; }
                },
                recommendedValue = true,
            });
        }
    }
#endif

    public static partial class VIUSettingsEditor
    {
        public const string WAVE_XR_OPENXR_PACKAGE = "com.htc.upm.wave.openxr";
        public const string WAVE_XR_OPENXR_FEATURE_ID = "com.unity.openxr.feature.vivefocus3";

        public static bool canSupportOpenXRAndroidWave
        {
            get { return OpenXRAndroidWaveSettings.instance.canSupport; }
        }

        public static bool supportOpenXRAndroidWave
        {
            get { return OpenXRAndroidWaveSettings.instance.support; }
            set { OpenXRAndroidWaveSettings.instance.support = value; }
        }

        private class OpenXRAndroidWaveSettings : VRPlatformSetting
        {

            public static OpenXRAndroidWaveSettings instance { get; private set; }

            public OpenXRAndroidWaveSettings() { instance = this; }

            public override int order { get { return 104; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

            public override bool canSupport
            {
                get
                {
                    if (activeBuildTargetGroup != requirdPlatform) { return false; }
                    if (PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME)) { return false; }
                    if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME)) { return false; }
                    if (!PackageManagerHelper.IsPackageInList(WAVE_XR_OPENXR_PACKAGE)) { return false; }
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, OPENXR_PLUGIN_LOADER_TYPE, requirdPlatform)) { return false; }
                    return true;
                }
            }

            public override bool support
            {
                get
                {
                    if (!canSupport) { return false; }
                    if (!VIUSettings.activateUnityXRModule) { return false; }
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, OPENXR_PLUGIN_LOADER_TYPE, requirdPlatform)) { return false; }
                    if (IsOpenXRFeatureGroupEnabled(requirdPlatform, OCULUS_QUEST_OPENXR_FEATURE_ID)) { return false; }
                    if (!IsOpenXRFeatureGroupEnabled(requirdPlatform, WAVE_XR_OPENXR_FEATURE_ID)) { return false; }
                    return true;
                }
                set
                {
                    if (value) { VIUSettings.activateUnityXRModule = true; }
                    if (value) { XRPluginManagementUtils.SetXRLoaderEnabled(OPENXR_PLUGIN_LOADER_TYPE, requirdPlatform, true); }
                    if (value) { SetOpenXRFeatureGroupEnable(requirdPlatform, OCULUS_QUEST_OPENXR_FEATURE_ID, false); }
                    SetOpenXRFeatureGroupEnable(requirdPlatform, WAVE_XR_OPENXR_FEATURE_ID, value);
                }
            }

            private static GUIContent s_title = new GUIContent("OpenXR Android for WaveXR (Experimental)", "VIVE Focus 3");
            public override void OnPreferenceGUI()
            {
                const float toggleWidth = 263f;
                if (canSupport)
                {
                    var wasSupported = support;
                    var shouldSupport = Foldouter.ShowFoldoutBlankWithEnabledToggle(s_title, wasSupported);
                    if (wasSupported != shouldSupport)
                    {
                        support = shouldSupport;
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();

#if !UNITY_2020_2_OR_NEWER
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(s_title.text, s_title.tooltip + ". Unity 2020.2 or later version required."), false, GUILayout.Width(toggleWidth));
                    GUI.enabled = true;
#else
                    if (activeBuildTargetGroup != requirdPlatform)
                    {
                        GUI.enabled = false;
                        s_title.tooltip += ". " + requirdPlatform + " platform required.";
                        ShowToggle(s_title, false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(requirdPlatform, BuildTarget.Android);
                    }
                    else if (PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(s_title, false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Remove Oculus XR Plugin", "Conflict package found. Remove " + OCULUS_XR_PACKAGE_NAME + " from Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            PackageManagerHelper.RemovePackage(OCULUS_XR_PACKAGE_NAME);
                        }
                    }
                    else if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(s_title, false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        var btnLabel = new GUIContent();
                        string tmPkgName;
                        if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_ESSENCE_NAME))
                        {
                            btnLabel.text = "Remove Wave XR Plugin - Essence";
                            btnLabel.tooltip = "Conflict package found. Remove " + WAVE_XR_PACKAGE_ESSENCE_NAME + " from Package Manager";
                            tmPkgName = WAVE_XR_PACKAGE_ESSENCE_NAME;
                        }
                        else if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NATIVE_NAME))
                        {
                            btnLabel.text = "Remove Wave XR Plugin - Native";
                            btnLabel.tooltip = "Conflict package found. Remove " + WAVE_XR_PACKAGE_NATIVE_NAME + " from Package Manager";
                            tmPkgName = WAVE_XR_PACKAGE_NATIVE_NAME;
                        }
                        else
                        {
                            btnLabel.text = "Remove Wave XR Plugin";
                            btnLabel.tooltip = "Conflict package found. Remove " + WAVE_XR_PACKAGE_NAME + " from Package Manager";
                            tmPkgName = WAVE_XR_PACKAGE_NAME;
                        }

                        if (GUILayout.Button(btnLabel, GUILayout.ExpandWidth(false)))
                        {
                            PackageManagerHelper.RemovePackage(tmPkgName);
                        }
                    }
                    else if (!PackageManagerHelper.IsPackageInList(WAVE_XR_OPENXR_PACKAGE))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(s_title), false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Add Wave XR Plugin - OpenXR", "Add " + WAVE_XR_OPENXR_PACKAGE + " to Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            if (!CheckScopeExists(RegistryToolSettings.Instance().Registry.Scopes))
                            {
                                ManifestUtils.AddRegistry(RegistryToolSettings.Instance().Registry);
                            }

                            PackageManagerHelper.AddToPackageList(WAVE_XR_OPENXR_PACKAGE);
                        }
                    }
                    else if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, OPENXR_PLUGIN_LOADER_TYPE, requirdPlatform))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(s_title), false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Enable OpenXR Loader", "Enable OpenXR Loader in XR Plug-in Management"), GUILayout.ExpandWidth(false)))
                        {
                            if (!ShowXRPluginManagementSection())
                            {
                                Debug.LogError("Fail opening XR Plug-in Management page, please enable OpenXR Loader manually.");
                            }
                        }
                    }
#endif
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}