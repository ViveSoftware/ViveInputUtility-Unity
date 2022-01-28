﻿//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System.IO;
using UnityEditor;
using UnityEngine;
using HTC.UnityPlugin.VRModuleManagement;

namespace HTC.UnityPlugin.Vive
{
    public class OculusRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
        private const string OCULUS_SDK_PATH = "Assets/Oculus/";
        private const string AVATAR_ASMDEF_FILE_NAME = "Oculus.Avatar.asmdef";
        private const string LIPSYNC_ASMDEF_FILE_NAME = "Oculus.LipSync.asmdef";
        private const string LIPSYNC_EDITOR_ASMDEF_FILE_NAME = "Oculus.LipSync.Editor.asmdef";
        private const string SPATIALIZER_ASMDEF_FILE_NAME = "Oculus.Spatializer.asmdef";
        private const string SPATIALIZER_EDITOR_ASMDEF_FILE_NAME = "Oculus.Spatializer.Editor.asmdef";

        private static readonly string ASMDEFS_PATH = "Packages/" + VIUSettingsEditor.VIUPackageName + "/ViveInputUtility/.asmdefs/Oculus/";

        public OculusRecommendedSettings()
        {
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Virtual Reality Supported with Oculus",
                skipCheckFunc = () => !VIUSettingsEditor.canSupportOculus,
                currentValueFunc = () => VIUSettingsEditor.supportOculus,
                setValueFunc = v => VIUSettingsEditor.supportOculus = v,
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Multithreaded Rendering",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
#if UNITY_2017_2_OR_NEWER
                currentValueFunc = () => PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android),
                setValueFunc = v => PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, v),
#else
                currentValueFunc = () => PlayerSettings.mobileMTRendering,
                setValueFunc = v => PlayerSettings.mobileMTRendering = v,
#endif
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Add Missing Assembly Definitions in Oculus SDK",
                skipCheckFunc = () => { return !VRModule.isOculusVRPluginDetected; },
                currentValueFunc = () => { return VRModule.isOculusVRAvatarSupported; },
                setValueFunc = v =>
                {
                    if (v)
                    {
                        try
                        {
                            string asmdefFullPath = Path.GetFullPath(ASMDEFS_PATH);
                            if (!Directory.Exists(asmdefFullPath))
                            {
                                MonoScript script = MonoScript.FromScriptableObject(VIUProjectSettings.Instance);
                                string path = AssetDatabase.GetAssetPath(script);
                                asmdefFullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(script) + "/../../../.asmdefs/Oculus/");
                                Debug.Log("asmdefFullPath=" + asmdefFullPath);
                            }
                            string oculusFullPath = Path.GetFullPath(OCULUS_SDK_PATH);
                            File.Copy(asmdefFullPath + AVATAR_ASMDEF_FILE_NAME, oculusFullPath + "Avatar/" + AVATAR_ASMDEF_FILE_NAME);
                            File.Copy(asmdefFullPath + LIPSYNC_ASMDEF_FILE_NAME, oculusFullPath + "LipSync/" + LIPSYNC_ASMDEF_FILE_NAME);
                            File.Copy(asmdefFullPath + LIPSYNC_EDITOR_ASMDEF_FILE_NAME, oculusFullPath + "LipSync/Editor/" + LIPSYNC_EDITOR_ASMDEF_FILE_NAME);
                            File.Copy(asmdefFullPath + SPATIALIZER_ASMDEF_FILE_NAME, oculusFullPath + "Spatializer/" + SPATIALIZER_ASMDEF_FILE_NAME);
                            File.Copy(asmdefFullPath + SPATIALIZER_EDITOR_ASMDEF_FILE_NAME, oculusFullPath + "Spatializer/Editor/" + SPATIALIZER_EDITOR_ASMDEF_FILE_NAME);
                            AssetDatabase.Refresh();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                },
                recommendedValue = true,
            });
        }
    }

    public static partial class VIUSettingsEditor
    {
        private const string OCULUS_DESKTOP_PACKAGE_NAME = "com.unity.xr.oculus.standalone";
        private const string OCULUS_XR_PACKAGE_NAME = "com.unity.xr.oculus";

        public static bool canSupportOculus
        {
            get { return OculusSettings.instance.canSupport; }
        }

        public static bool supportOculus
        {
            get { return OculusSettings.instance.support; }
            set { OculusSettings.instance.support = value; }
        }

        private class OculusSettings : VRPlatformSetting
        {
            public static OculusSettings instance { get; private set; }

            public OculusSettings() { instance = this; }

            public override int order { get { return 2; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Standalone; } }

            public override bool canSupport
            {
                get
                {
                    if (activeBuildTargetGroup != requirdPlatform) { return false; }
#if UNITY_2019_3_OR_NEWER
                    if (!PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME)) { return false; }
#elif UNITY_2018_1_OR_NEWER
                    if (!PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME) && !PackageManagerHelper.IsPackageInList(OCULUS_DESKTOP_PACKAGE_NAME)) { return false; }
#elif UNITY_5_6_OR_NEWER
                    if (!VRModule.isOculusVRPluginDetected) { return false; }
#endif
                    return true;
                }
            }

            public override bool support
            {
                get
                {
                    if (!canSupport) { return false; }
#if UNITY_2019_3_OR_NEWER
                    if (!VIUSettings.activateOculusVRModule && !VIUSettings.activateUnityXRModule) { return false; }
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_NAME, requirdPlatform)) { return false; }
#elif UNITY_5_5_OR_NEWER
                    if (!VIUSettings.activateOculusVRModule && !VIUSettings.activateUnityNativeVRModule) { return false; }
                    if (!OculusSDK.enabled) { return false; }
#elif UNITY_5_4_OR_NEWER
                    if (!VIUSettings.activateOculusVRModule) { return false; }
                    if (!OculusSDK.enabled) { return false; }
#else
                    if (!VIUSettings.activateOculusVRModule) { return false; }
                    if (!virtualRealitySupported) { return false; }
#endif
                    return true;
                }
                set
                {
                    if (value)
                    {
                        VIUSettings.activateOculusVRModule = true;
                        VIUSettings.activateUnityXRModule = true;
                        VIUSettings.activateUnityNativeVRModule = true;
#if UNITY_2019_3_OR_NEWER
                        VRSDKSettings.vrEnabled = false;
                        XRPluginManagementUtils.SetXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_CLASS_NAME, requirdPlatform, true);
#elif UNITY_5_4_OR_NEWER
                        OculusSDK.enabled = true;
#else
                        virtualRealitySupported = true;
#endif
                    }
                    else
                    {
                        VIUSettings.activateOculusVRModule = false;
#if UNITY_2019_3_OR_NEWER
                        XRPluginManagementUtils.SetXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_CLASS_NAME, requirdPlatform, false);
#elif UNITY_5_4_OR_NEWER
                        OculusSDK.enabled = false;
#else
                        virtualRealitySupported = false;
#endif
                    }
                }
            }

            public override void OnPreferenceGUI()
            {
                const string title = "Oculus Desktop";
                if (canSupport)
                {
                    var wasSupported = support;
                    var shouldSupport = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(title, "Oculus Rift, Oculus Rift S"), wasSupported);
                    if (wasSupported != shouldSupport)
                    {
                        support = shouldSupport;
                        s_symbolChanged = true;
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();

                    if (activeBuildTargetGroup != BuildTargetGroup.Standalone)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Standalone platform required."), false, GUILayout.Width(150f));
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
                    else if (!PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Oculus XR Plugin package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("Oculus XR Plugin", OCULUS_XR_PACKAGE_NAME);
                    }
#elif UNITY_2018_2_OR_NEWER
                    else if (!PackageManagerHelper.IsPackageInList(OCULUS_DESKTOP_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Oculus (Desktop) package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("Oculus (Desktop)", OCULUS_DESKTOP_PACKAGE_NAME);
                    }
#endif
                    else if (!VRModule.isOculusVRPluginDetected)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Oculus VR Plugin required."), false, GUILayout.Width(150f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowUrlLinkButton(URL_OCULUS_VR_PLUGIN);
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}