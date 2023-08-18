//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

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

#if VIU_OCULUSVR_20_0_OR_NEWER
            // Oculus Avatar SDK (legacy) is removed since Oculus Integration SDK v39
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Add Missing Assembly Definitions in Oculus SDK",
                skipCheckFunc = () => { return VIUSettingsEditor.oculusVRPlugin_warpperVersion >= VIUSettingsEditor.oculusVRPlugin_v39_warpperVersion; },
                currentValueFunc = () => { return VRModule.isOculusVRAvatarSupported; },
                setValueFunc = v =>
                {
                    if (v)
                    {
                        string asmdefFullPath = Path.GetFullPath(ASMDEFS_PATH);
                        if (!Directory.Exists(asmdefFullPath))
                        {
                            MonoScript script = MonoScript.FromScriptableObject(VIUProjectSettings.Instance);
                            asmdefFullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(script) + "/../../../.asmdefs/Oculus/");
                        }
                        string oculusFullPath = Path.GetFullPath(OCULUS_SDK_PATH);
                        SafeCopy(asmdefFullPath + AVATAR_ASMDEF_FILE_NAME, oculusFullPath + "Avatar/" + AVATAR_ASMDEF_FILE_NAME);
                        SafeCopy(asmdefFullPath + LIPSYNC_ASMDEF_FILE_NAME, oculusFullPath + "LipSync/" + LIPSYNC_ASMDEF_FILE_NAME);
                        SafeCopy(asmdefFullPath + LIPSYNC_EDITOR_ASMDEF_FILE_NAME, oculusFullPath + "LipSync/Editor/" + LIPSYNC_EDITOR_ASMDEF_FILE_NAME);
                        SafeCopy(asmdefFullPath + SPATIALIZER_ASMDEF_FILE_NAME, oculusFullPath + "Spatializer/" + SPATIALIZER_ASMDEF_FILE_NAME);
                        SafeCopy(asmdefFullPath + SPATIALIZER_EDITOR_ASMDEF_FILE_NAME, oculusFullPath + "Spatializer/Editor/" + SPATIALIZER_EDITOR_ASMDEF_FILE_NAME);
                        AssetDatabase.Refresh();
                    }
                },
                recommendedValue = true,
            });
#endif
        }

        private static bool SafeCopy(string src, string dst)
        {
            try
            {
                if (!File.Exists(dst))
                {
                    File.Copy(src, dst);
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            return false;
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
            private Foldouter m_foldouter = new Foldouter();

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
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_NAME, OculusVRModule.OCULUS_XR_LOADER_CLASS_NAME, requirdPlatform)) { return false; }
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
                    var shouldSupport = m_foldouter.ShowFoldoutButtonWithEnabledToggle(new GUIContent(title, "Oculus Rift, Oculus Rift S, Oculus Link"), wasSupported);
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

                if (support && m_foldouter.isExpended)
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        EditorGUI.indentLevel += 2;

                        // Hand tracking support
                        const string enableHandTrackingTitle = "Enable Oculus Hand Tracking";
                        const string enableHandRenderModelTitle = "Enable Oculus Tracked Hand Render Model";
#if VIU_OCULUSVR_20_0_OR_NEWER
                        {
                            var oldEnableHandTracking = VIUSettings.activateOculusVRModule && OculusGoSettings.oculusProjectConfig.handTrackingSupport != OVRProjectConfig.HandTrackingSupport.ControllersOnly;
                            var newEnableHandTracking = EditorGUILayout.ToggleLeft(enableHandTrackingTitle, oldEnableHandTracking);
                            if (newEnableHandTracking)
                            {
                                if (!oldEnableHandTracking)
                                {
                                    VIUSettings.activateOculusVRModule = true;
                                    OculusGoSettings.oculusProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
                                }
                            }
                            else
                            {
                                if (oldEnableHandTracking)
                                {
                                    OculusGoSettings.oculusProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;
                                }
                            }

                            if (newEnableHandTracking)
                            {
                                VIUSettings.EnableOculusSDKHandRenderModel = EditorGUILayout.ToggleLeft(new GUIContent(enableHandRenderModelTitle, VIUSettings.ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_TOOLTIP), VIUSettings.EnableOculusSDKHandRenderModel);
                            }
                            else
                            {
                                var wasGUIEnabled = GUI.enabled;
                                GUI.enabled = false;
                                EditorGUILayout.ToggleLeft(new GUIContent(enableHandRenderModelTitle, VIUSettings.ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_TOOLTIP), false);
                                GUI.enabled = wasGUIEnabled;
                            }
                        }
#else
                        {
                            var wasGUIEnabled = GUI.enabled;
                            GUI.enabled = false;

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ToggleLeft(new GUIContent(enableHandTrackingTitle, "Hand tracking not supported. Please import latest Oculus Integration."), false, GUILayout.Width(280f));
                            GUILayout.FlexibleSpace();
                            GUI.enabled = true;
                            ShowUrlLinkButton(URL_OCULUS_VR_PLUGIN, "Update Oculus Integration");
                            EditorGUILayout.EndHorizontal();

                            GUI.enabled = false;
                            EditorGUILayout.ToggleLeft(new GUIContent(enableHandRenderModelTitle, VIUSettings.ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_TOOLTIP), false);

                            GUI.enabled = wasGUIEnabled;
                        }
#endif

#pragma warning disable 0162
                        // Controller Render Model
                        const string enableControllerRenderModelTitle = "Enable Oculus Controller Render Model";
                        const string enableControllerRenderModelSkeletonTitle = "Enable Hand Attached to Oculus Controller Render Model";
                        if (OculusVRExtension.VIUOvrAvatar.SUPPORTED)
                        {
                            var oldValue = VIUSettings.activateOculusVRModule && VIUSettings.EnableOculusSDKControllerRenderModel;
                            var newValue = EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelTitle, VIUSettings.ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_TOOLTIP), oldValue);
                            if (newValue)
                            {
                                if (!oldValue)
                                {
                                    VIUSettings.activateOculusVRModule = true;
                                    VIUSettings.EnableOculusSDKControllerRenderModel = true;
                                }
                            }
                            else
                            {
                                if (oldValue)
                                {
                                    VIUSettings.EnableOculusSDKControllerRenderModel = false;
                                }
                            }

                            if (newValue)
                            {
                                VIUSettings.EnableOculusSDKControllerRenderModelSkeleton = EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelSkeletonTitle, VIUSettings.ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_TOOLTIP), VIUSettings.EnableOculusSDKControllerRenderModelSkeleton);
                            }
                            else
                            {
                                var wasGUIEnabled = GUI.enabled;
                                GUI.enabled = false;
                                EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelSkeletonTitle, VIUSettings.ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_TOOLTIP), false);
                                GUI.enabled = wasGUIEnabled;
                            }
                        }
                        else
                        {
                            var wasGUIEnabled = GUI.enabled;
                            GUI.enabled = false;

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelTitle, "OvrAvatar not found. Please import latest Oculus Integration."), false, GUILayout.Width(280f));
                            GUILayout.FlexibleSpace();
                            GUI.enabled = true;
                            ShowUrlLinkButton(URL_OCULUS_VR_PLUGIN, "Update Oculus Integration");
                            EditorGUILayout.EndHorizontal();

                            GUI.enabled = false;
                            EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelSkeletonTitle, VIUSettings.ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_TOOLTIP), false);

                            GUI.enabled = wasGUIEnabled;
                        }
#pragma warning restore 0162

                        EditorGUI.indentLevel -= 2;
                    }
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }
            }
        }
    }
}