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
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

namespace HTC.UnityPlugin.Vive
{
#if VIU_OPENXR
    public class OpenXRAndroidOculusRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
        public OpenXRAndroidOculusRecommendedSettings()
        {
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Add Oculus Touch Controller Profile",
                skipCheckFunc = () => !VIUSettingsEditor.supportOpenXRAndroidOculus,
                currentValueFunc = () =>
                {
                    var feature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<OculusTouchControllerProfile>();
                    return feature != null && feature.enabled;
                },
                setValueFunc = v =>
                {
                    var feature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<OculusTouchControllerProfile>();
                    if (feature != null) { feature.enabled = true; }
                },
                recommendedValue = true,
            });
        }
    }
#endif

    public static partial class VIUSettingsEditor
    {
        public const string OCULUS_QUEST_OPENXR_FEATURE_ID = "com.unity.openxr.feature.oculusquest";

        public static bool canSupportOpenXRAndroidOculus
        {
            get { return OpenXRAndroidOculusSettings.instance.canSupport; }
        }

        public static bool supportOpenXRAndroidOculus
        {
            get { return OpenXRAndroidOculusSettings.instance.support; }
            set { OpenXRAndroidOculusSettings.instance.support = value; }
        }

        private class OpenXRAndroidOculusSettings : VRPlatformSetting
        {
            public static OpenXRAndroidOculusSettings instance { get; private set; }

            public OpenXRAndroidOculusSettings() { instance = this; }

            public override int order { get { return 105; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

            public override bool canSupport
            {
                get
                {
                    if (activeBuildTargetGroup != requirdPlatform) { return false; }
                    if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME)) { return false; }
                    if (PackageManagerHelper.IsPackageInList(WAVE_XR_OPENXR_PACKAGE)) { return false; }
                    if (PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME)) { return false; }
                    if (!PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME)) { return false; }
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, requirdPlatform)) { return false; }
                    return true;
                }
            }

            public override bool support
            {
                get
                {
                    if (!canSupport) { return false; }
                    if (!VIUSettings.activateUnityXRModule) { return false; }
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, requirdPlatform)) { return false; }
                    if (IsOpenXRFeatureGroupEnabled(requirdPlatform, WAVE_XR_OPENXR_FEATURE_ID)) { return false; }
                    if (!IsOpenXRFeatureGroupEnabled(requirdPlatform, OCULUS_QUEST_OPENXR_FEATURE_ID)) { return false; }
                    return true;
                }
                set
                {
                    if (value) { VIUSettings.activateUnityXRModule = true; }
                    if (value) { XRPluginManagementUtils.SetXRLoaderEnabled(OPENXR_PLUGIN_LOADER_TYPE, requirdPlatform, true); }
                    if (value) { SetOpenXRFeatureGroupEnable(requirdPlatform, WAVE_XR_OPENXR_FEATURE_ID, false); }
                    SetOpenXRFeatureGroupEnable(requirdPlatform, OCULUS_QUEST_OPENXR_FEATURE_ID, value);
                }
            }

            private static GUIContent s_title = new GUIContent("OpenXR Android for Oculus (Experimental)", "Oculus Quest");
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
                        ShowToggle(s_title, false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(requirdPlatform, BuildTarget.Android);
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
                    else if (PackageManagerHelper.IsPackageInList(WAVE_XR_OPENXR_PACKAGE))
                    {
                        GUI.enabled = false;
                        ShowToggle(s_title, false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Remove Wave XR Plugin - OpenXR", "Conflict package found. Remove " + WAVE_XR_OPENXR_PACKAGE + " from Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            PackageManagerHelper.RemovePackage(WAVE_XR_OPENXR_PACKAGE);
                        }
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
                    else if (!PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(s_title), false, GUILayout.Width(toggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Add OpenXR Plugin", "Add " + OPENXR_PLUGIN_PACKAGE_NAME + " to Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            PackageManagerHelper.AddToPackageList(OPENXR_PLUGIN_PACKAGE_NAME);
                        }
                    }
                    else if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, requirdPlatform))
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