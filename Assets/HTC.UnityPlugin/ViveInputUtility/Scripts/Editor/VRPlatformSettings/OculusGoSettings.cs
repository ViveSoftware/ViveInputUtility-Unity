//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;
using System;
#if UNITY_5_6_OR_NEWER
using UnityEditor.Build;
#endif
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
#if UNITY_5_6_OR_NEWER
using UnityEditor.Rendering;
#endif

namespace HTC.UnityPlugin.Vive
{
    public class OculusGoRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
        private SerializedObject projectSettingsAsset;
        private SerializedObject qualitySettingsAsset;

        private SerializedObject GetPlayerSettings()
        {
            if (projectSettingsAsset == null)
            {
                projectSettingsAsset = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            }

            return projectSettingsAsset;
        }

        private SerializedObject GetQualitySettingsAsset()
        {
            if (qualitySettingsAsset == null)
            {
                qualitySettingsAsset = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            }

            return qualitySettingsAsset;
        }

        public OculusGoRecommendedSettings()
        {
#if UNITY_5_4_OR_NEWER
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Graphic Jobs for Oculus Go",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.graphicsJobs,
                setValueFunc = v => PlayerSettings.graphicsJobs = v,
                recommendedValue = false,
            });
#endif
            // Oculus mobile recommended settings
            // https://developer.oculus.com/blog/tech-note-unity-settings-for-mobile-vr/
            Add(new VIUVersionCheck.RecommendedSetting<MobileTextureSubtarget>()
            {
                settingTitle = "Texture Compression",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => EditorUserBuildSettings.androidBuildSubtarget,
                setValueFunc = v => EditorUserBuildSettings.androidBuildSubtarget = v,
                recommendedValue = MobileTextureSubtarget.ASTC,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Mobile Multithreaded Rendering",
                skipCheckFunc = () => !VIUSettingsEditor.supportWaveVR || !VIUSettingsEditor.supportOculusGo,
#if UNITY_2017_2_OR_NEWER
                currentValueFunc = () => PlayerSettings.MTRendering,
                setValueFunc = v => PlayerSettings.MTRendering = v,
#else
                currentValueFunc = () => PlayerSettings.mobileMTRendering,
                setValueFunc = v => PlayerSettings.mobileMTRendering = v,
#endif
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Static Batching",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () =>
                {
                    var playerSetting = GetPlayerSettings();
                    playerSetting.Update();

                    var batchingArrayProp = playerSetting.FindProperty("m_BuildTargetBatching");
                    var batchingProp = default(SerializedProperty);
                    for (int i = 0, imax = batchingArrayProp.arraySize; i < imax; ++i)
                    {
                        var element = batchingArrayProp.GetArrayElementAtIndex(i);
                        if (element.FindPropertyRelative("m_BuildTarget").stringValue == "Android")
                        {
                            batchingProp = element;
                            break;
                        }
                    }
                    if (batchingProp == null) { return false; }

                    var staticBatchingProp = batchingProp.FindPropertyRelative("m_StaticBatching");
                    if (staticBatchingProp == null) { return false; }

                    return staticBatchingProp.boolValue;
                },
                setValueFunc = v =>
                {
                    var playerSetting = GetPlayerSettings();
                    playerSetting.Update();

                    var batchingArrayProp = playerSetting.FindProperty("m_BuildTargetBatching");
                    var batchingProp = default(SerializedProperty);
                    for (int i = 0, imax = batchingArrayProp.arraySize; i < imax; ++i)
                    {
                        var element = batchingArrayProp.GetArrayElementAtIndex(i);
                        if (element.FindPropertyRelative("m_BuildTarget").stringValue == "Android")
                        {
                            batchingProp = element;
                            break;
                        }
                    }
                    if (batchingProp == null)
                    {
                        batchingArrayProp.arraySize += 1;
                        batchingProp = batchingArrayProp.GetArrayElementAtIndex(batchingArrayProp.arraySize - 1);
                        batchingProp.FindPropertyRelative("m_BuildTarget").stringValue = "Android";
                    }

                    batchingProp.FindPropertyRelative("m_StaticBatching").boolValue = v;
                    playerSetting.ApplyModifiedProperties();
                },
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Dynamic Batching",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () =>
                {
                    var settingObj = GetPlayerSettings();
                    settingObj.Update();

                    var batchingArrayProp = settingObj.FindProperty("m_BuildTargetBatching");
                    var batchingProp = default(SerializedProperty);
                    for (int i = 0, imax = batchingArrayProp.arraySize; i < imax; ++i)
                    {
                        var element = batchingArrayProp.GetArrayElementAtIndex(i);
                        if (element.FindPropertyRelative("m_BuildTarget").stringValue == "Android")
                        {
                            batchingProp = element;
                            break;
                        }
                    }
                    if (batchingProp == null) { return false; }

                    var staticBatchingProp = batchingProp.FindPropertyRelative("m_DynamicBatching");
                    if (staticBatchingProp == null) { return false; }

                    return staticBatchingProp.boolValue;
                },
                setValueFunc = v =>
                {
                    var settingObj = GetPlayerSettings();
                    settingObj.Update();

                    var batchingArrayProp = settingObj.FindProperty("m_BuildTargetBatching");
                    var batchingProp = default(SerializedProperty);
                    for (int i = 0, imax = batchingArrayProp.arraySize; i < imax; ++i)
                    {
                        var element = batchingArrayProp.GetArrayElementAtIndex(i);
                        if (element.FindPropertyRelative("m_BuildTarget").stringValue == "Android")
                        {
                            batchingProp = element;
                            break;
                        }
                    }
                    if (batchingProp == null)
                    {
                        batchingArrayProp.arraySize += 1;
                        batchingProp = batchingArrayProp.GetArrayElementAtIndex(batchingArrayProp.arraySize - 1);
                        batchingProp.FindPropertyRelative("m_BuildTarget").stringValue = "Android";
                    }

                    batchingProp.FindPropertyRelative("m_DynamicBatching").boolValue = v;
                    settingObj.ApplyModifiedProperties();
                },
                recommendedValue = true,
            });

#if UNITY_5_5_OR_NEWER
            Add(new VIUVersionCheck.RecommendedSetting<StereoRenderingPath>()
            {
                settingTitle = "Stereo Rendering Method",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.stereoRenderingPath,
                setValueFunc = v => PlayerSettings.stereoRenderingPath = v,
                recommendedValue = StereoRenderingPath.SinglePass,
            });
#endif

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Prebake Collision Meshes",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.bakeCollisionMeshes,
                setValueFunc = v => PlayerSettings.bakeCollisionMeshes = v,
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Keep Loaded Shaders Alive",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () =>
                {
                    var settingObj = GetPlayerSettings();
                    settingObj.Update();

                    return settingObj.FindProperty("keepLoadedShadersAlive").boolValue;
                },
                setValueFunc = v =>
                {
                    var settingObj = GetPlayerSettings();
                    settingObj.Update();

                    settingObj.FindProperty("keepLoadedShadersAlive").boolValue = v;
                    settingObj.ApplyModifiedProperties();
                },
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Optimize Mesh Data",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.stripUnusedMeshComponents,
                setValueFunc = v => PlayerSettings.stripUnusedMeshComponents = v,
                recommendedValue = true,
            });

#if UNITY_5_5_OR_NEWER && !UNITY_2019_4_OR_NEWER
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Use Oculus Mobile recommended Quality Settings",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () =>
                {
                    var settingObj = GetQualitySettingsAsset();
                    settingObj.Update();

                    var qualitySettingsArray = settingObj.FindProperty("m_QualitySettings");
                    // try to apply on default, unchanged, lagecy quality settings only
                    if (qualitySettingsArray.arraySize < 3) { return true; }
                    var simpleQualitySetting = qualitySettingsArray.GetArrayElementAtIndex(2);

                    var nameProp = simpleQualitySetting.FindPropertyRelative("name"); if (nameProp == null) { return true; }
                    var simpleQualitySettingName = nameProp.stringValue;
                    if (simpleQualitySettingName != "Simple" || simpleQualitySettingName != "Medium") { return true; }

                    var excludedPlatforms = simpleQualitySetting.FindPropertyRelative("excludedTargetPlatforms"); if (excludedPlatforms == null) { return true; }
                    var pixelLightCount = simpleQualitySetting.FindPropertyRelative("pixelLightCount"); if (pixelLightCount == null) { return true; }
                    var anisotropicTextures = simpleQualitySetting.FindPropertyRelative("anisotropicTextures"); if (anisotropicTextures == null) { return true; }
                    var antiAliasing = simpleQualitySetting.FindPropertyRelative("antiAliasing"); if (antiAliasing == null) { return true; }
                    var shadows = simpleQualitySetting.FindPropertyRelative("shadows"); if (shadows == null) { return true; }
                    var vSyncCount = simpleQualitySetting.FindPropertyRelative("vSyncCount"); if (vSyncCount == null) { return true; }
#if UNITY_2019_1_OR_NEWER
                    var weights = simpleQualitySetting.FindPropertyRelative("skinWeights"); if (weights == null) { return true; }
#else
                    var weights = simpleQualitySetting.FindPropertyRelative("blendWeights"); if (weights == null) { return true; }
#endif

                    for (int i = 0, imax = excludedPlatforms.arraySize; i < imax; ++i)
                    {
                        if (excludedPlatforms.GetArrayElementAtIndex(i).stringValue == "Android")
                        {
                            return false;
                        }
                    }
                    if (pixelLightCount.intValue > 1) { return false; }
                    if (anisotropicTextures.intValue != (int)AnisotropicFiltering.Disable) { return false; }
                    if (antiAliasing.intValue > 4 || antiAliasing.intValue < 2) { return false; }
                    if (shadows.intValue >= (int)ShadowQuality.All) { return false; }
                    if (vSyncCount.intValue != 0) { return false; }
                    if (weights.intValue > 2) { return false; }
                    return true;
                },
                setValueFunc = v =>
                {
                    if (!v) { return; }

                    var settingObj = GetQualitySettingsAsset();
                    settingObj.Update();

                    // Simple(level 2) is a good one to start from, it should be the only level that is checked.
                    var qualitySettingsArray = settingObj.FindProperty("m_QualitySettings");
                    var simpleQualitySetting = qualitySettingsArray.GetArrayElementAtIndex(2);
                    var excludedPlatforms = simpleQualitySetting.FindPropertyRelative("excludedTargetPlatforms");
                    var pixelLightCount = simpleQualitySetting.FindPropertyRelative("pixelLightCount");
                    var anisotropicTextures = simpleQualitySetting.FindPropertyRelative("anisotropicTextures");
                    var antiAliasing = simpleQualitySetting.FindPropertyRelative("antiAliasing");
                    var shadows = simpleQualitySetting.FindPropertyRelative("shadows");
                    var vSyncCount = simpleQualitySetting.FindPropertyRelative("vSyncCount");
#if UNITY_2019_1_OR_NEWER
                    var weights = simpleQualitySetting.FindPropertyRelative("skinWeights");
#else
                    var weights = simpleQualitySetting.FindPropertyRelative("blendWeights");
#endif

                    for (int i = 0, imax = excludedPlatforms.arraySize; i < imax; ++i)
                    {
                        if (excludedPlatforms.GetArrayElementAtIndex(i).stringValue == "Android")
                        {
                            excludedPlatforms.DeleteArrayElementAtIndex(i); break;
                        }
                    }
                    pixelLightCount.intValue = 1;
                    anisotropicTextures.intValue = (int)AnisotropicFiltering.Disable;
                    if (antiAliasing.intValue != 2 || antiAliasing.intValue != 4) { antiAliasing.intValue = 4; }
                    if (shadows.intValue >= (int)ShadowQuality.All) { shadows.intValue = (int)ShadowQuality.HardOnly; }
                    vSyncCount.intValue = 0;
                    if (weights.intValue > 2) { weights.intValue = 2; }

                    settingObj.ApplyModifiedProperties();
                },
                recommendedValue = true,
            });
#endif

#if UNITY_5_6_OR_NEWER && !UNITY_2019_4_OR_NEWER
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Use Oculus Mobile recommended Graphics Tier Settings",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () =>
                {
                    var tierSettings = default(TierSettings);

                    tierSettings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier1);
                    if (tierSettings.standardShaderQuality != ShaderQuality.Low) { return false; }
                    if (tierSettings.renderingPath != RenderingPath.Forward) { return false; }
                    if (tierSettings.realtimeGICPUUsage != RealtimeGICPUUsage.Low) { return false; }

                    tierSettings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier2);
                    if (tierSettings.standardShaderQuality != ShaderQuality.Low) { return false; }
                    if (tierSettings.renderingPath != RenderingPath.Forward) { return false; }
                    if (tierSettings.realtimeGICPUUsage != RealtimeGICPUUsage.Low) { return false; }

                    tierSettings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier3);
                    if (tierSettings.standardShaderQuality != ShaderQuality.Low) { return false; }
                    if (tierSettings.renderingPath != RenderingPath.Forward) { return false; }
                    if (tierSettings.realtimeGICPUUsage != RealtimeGICPUUsage.Low) { return false; }
                    return true;
                },
                setValueFunc = v =>
                {
                    if (!v) { return; }

                    var tierSettings = default(TierSettings);

                    tierSettings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier1);
                    tierSettings.standardShaderQuality = ShaderQuality.Low;
                    tierSettings.renderingPath = RenderingPath.Forward;
                    tierSettings.realtimeGICPUUsage = RealtimeGICPUUsage.Low;
                    EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier1, tierSettings);

                    tierSettings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier2);
                    tierSettings.standardShaderQuality = ShaderQuality.Low;
                    tierSettings.renderingPath = RenderingPath.Forward;
                    tierSettings.realtimeGICPUUsage = RealtimeGICPUUsage.Low;
                    EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier2, tierSettings);

                    tierSettings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier3);
                    tierSettings.standardShaderQuality = ShaderQuality.Low;
                    tierSettings.renderingPath = RenderingPath.Forward;
                    tierSettings.realtimeGICPUUsage = RealtimeGICPUUsage.Low;
                    EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Android, GraphicsTier.Tier3, tierSettings);
                },
                recommendedValue = true,
            });
#endif
        }
    }

    public static partial class VIUSettingsEditor
    {
        public const string URL_OCULUS_VR_PLUGIN = "https://assetstore.unity.com/packages/slug/82022?";
        private const string OCULUS_ANDROID_PACKAGE_NAME = "com.unity.xr.oculus.android";
        public const AndroidSdkVersions MIN_SUPPORTED_ANDROID_SDK_VERSION =
#if UNITY_2020_1_OR_NEWER
            AndroidSdkVersions.AndroidApiLevel22;
#else
            AndroidSdkVersions.AndroidApiLevel21;
#endif
        public static readonly Version oculusVRPlugin_warpperVersion =
#if VIU_OCULUSVR
            OVRPlugin.wrapperVersion;
#else
            new System.Version(0, 0, 0);
#endif
        public static readonly Version oculusVRPlugin_v39_warpperVersion = new Version(1, 71, 0);

        public static bool canSupportOculusGo
        {
            get { return OculusGoSettings.instance.canSupport; }
        }

        public static bool supportOculusGo
        {
            get { return OculusGoSettings.instance.support; }
            set { OculusGoSettings.instance.support = value; }
        }

        private class OculusGoSettings : VRPlatformSetting
#if UNITY_2018_1_OR_NEWER
        , IPreprocessBuildWithReport
#elif UNITY_5_6_OR_NEWER
		, IPreprocessBuild
#endif
        {
            private Foldouter m_foldouter = new Foldouter();

#if VIU_OCULUSVR_20_0_OR_NEWER
            private static OVRProjectConfig s_oculusProjectConfig;

            public static OVRProjectConfig oculusProjectConfig
            {
                get
                {
                    if (s_oculusProjectConfig == null)
                    {
                        s_oculusProjectConfig = OVRProjectConfig.GetProjectConfig();
                    }

                    return s_oculusProjectConfig;
                }
            }
#endif

            public static OculusGoSettings instance { get; private set; }

            public OculusGoSettings() { instance = this; }

            public override int order { get { return 103; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

            private static bool editorGraphicsJobs
            {
#if UNITY_5_4_OR_NEWER
                get { return PlayerSettings.graphicsJobs; }
                set { PlayerSettings.graphicsJobs = value; }
#else
                get { return false; }
                set { }
#endif
            }

            private string defaultAndroidManifestPath
            {
                get
                {
#if VIU_OCULUSVR
                    var monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
                    var monoScript = monoScripts.FirstOrDefault(script => script.GetClass() == typeof(OVRInput));
                    var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(monoScript));
                    var fullPath = Path.GetFullPath((path.Substring(0, path.Length - "Scripts".Length) + "Editor/AndroidManifest.OVRSubmission.xml").Replace("\\", "/"));

                    return fullPath.Substring(fullPath.IndexOf("Assets"), fullPath.Length - fullPath.IndexOf("Assets"));
#else
                    return string.Empty;
#endif
                }
            }

            public override bool canSupport
            {
                get
                {
                    if (activeBuildTargetGroup != requirdPlatform) { return false; }
#if UNITY_2019_3_OR_NEWER
                    if (!PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME)) { return false; }
                    if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME)) { return false; }
                    if (PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME)) { return false; }
#elif UNITY_2018_1_OR_NEWER
                    if (!PackageManagerHelper.IsPackageInList(OCULUS_ANDROID_PACKAGE_NAME) && !PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME)) { return false; }
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
                    if (PlayerSettings.Android.minSdkVersion < MIN_SUPPORTED_ANDROID_SDK_VERSION) { return false; }
                    if ((PlayerSettings.colorSpace == ColorSpace.Linear || PlayerSettings.gpuSkinning) && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                    if (editorGraphicsJobs) { return false; }
#if UNITY_2019_3_OR_NEWER
                    if (!VIUSettings.activateOculusVRModule && !VIUSettings.activateUnityXRModule) { return false; }
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_NAME, OculusVRModule.OCULUS_XR_LOADER_CLASS_NAME, requirdPlatform)) { return false; }
#else
                    if (!VIUSettings.activateOculusVRModule && !VIUSettings.activateUnityNativeVRModule) { return false; }
                    if (!OculusSDK.enabled) { return false; }
#endif
                    return true;
                }
                set
                {
                    if (value)
                    {
                        if (PlayerSettings.Android.minSdkVersion < MIN_SUPPORTED_ANDROID_SDK_VERSION)
                        {
                            PlayerSettings.Android.minSdkVersion = MIN_SUPPORTED_ANDROID_SDK_VERSION;
                        }

                        if (PlayerSettings.colorSpace == ColorSpace.Linear || PlayerSettings.gpuSkinning)
                        {
                            SetGraphicsAPI(BuildTarget.Android, GraphicsDeviceType.OpenGLES3);
                        }

                        editorGraphicsJobs = false;
                        VIUSettings.activateOculusVRModule = true;
                        VIUSettings.activateUnityXRModule = true;

#if UNITY_2019_3_OR_NEWER
                        VRSDKSettings.vrEnabled = false;
                        XRPluginManagementUtils.SetXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_CLASS_NAME, requirdPlatform, true);
#else
                        OculusSDK.enabled = true;
                        VIUSettings.activateUnityNativeVRModule = true;
#endif
                    }
                    else
                    {
                        VIUSettings.activateOculusVRModule = false;
#if UNITY_2019_3_OR_NEWER
                        XRPluginManagementUtils.SetXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_CLASS_NAME, requirdPlatform, false);
#else
                        OculusSDK.enabled = false;
#endif
                    }
                }
            }

            public int callbackOrder { get { return 0; } }

            public override void OnPreferenceGUI()
            {
                const string title = "Oculus Android";
                if (canSupport)
                {
                    var wasSupported = support;
                    var shouldSupport = m_foldouter.ShowFoldoutButtonWithEnabledToggle(new GUIContent(title, "Oculus Go, Oculus Quest"), wasSupported);
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

                    if (activeBuildTargetGroup != requirdPlatform)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, requirdPlatform + " platform required."), false, GUILayout.Width(150f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(requirdPlatform, BuildTarget.Android);
                    }
#if UNITY_2019_3_OR_NEWER
                    else if (!PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Oculus XR Plugin package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("Oculus XR Plugin", OCULUS_XR_PACKAGE_NAME);
                    }
                    else if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Conflict package found."), false, GUILayout.Width(230f));
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
                    else if (PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Conflict package found."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Remove OpenXR Plugin", "Conflict package found. Remove " + OPENXR_PLUGIN_PACKAGE_NAME + " from Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            PackageManagerHelper.RemovePackage(OPENXR_PLUGIN_PACKAGE_NAME);
                        }
                    }
#elif UNITY_2018_2_OR_NEWER
                    else if (!PackageManagerHelper.IsPackageInList(OCULUS_ANDROID_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Oculus Android package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("Oculus Android", OCULUS_ANDROID_PACKAGE_NAME);
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
                            var oldEnableHandTracking = VIUSettings.activateOculusVRModule && oculusProjectConfig.handTrackingSupport != OVRProjectConfig.HandTrackingSupport.ControllersOnly;
                            var newEnableHandTracking = EditorGUILayout.ToggleLeft(enableHandTrackingTitle, oldEnableHandTracking);
                            if (newEnableHandTracking)
                            {
                                if (!oldEnableHandTracking)
                                {
                                    VIUSettings.activateOculusVRModule = true;
                                    oculusProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
                                }
                            }
                            else
                            {
                                if (oldEnableHandTracking)
                                {
                                    oculusProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;
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

                        // Controller Render Model
                        const string enableControllerRenderModelTitle = "Enable Oculus Controller Render Model";
                        const string enableControllerRenderModelSkeletonTitle = "Enable Hand Attached to Oculus Controller Render Model";

                        if (OculusVRExtension.VIUOvrAvatar.SUPPORTED || oculusVRPlugin_warpperVersion >= oculusVRPlugin_v39_warpperVersion)
                        {
                            var oldValue = VIUSettings.activateOculusVRModule && VIUSettings.EnableOculusSDKControllerRenderModel;
                            var newValue = EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelTitle, VIUSettings.ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_TOOLTIP), oldValue);

                            if (!oldValue && newValue && !VRModule.isOculusVRAvatarSupported && FindControllerRenderModelPrefab() == null)
                            {
                                Debug.LogError("missing \"OVRControllerPrefab\"");
                                newValue = false;
                            }

                            if (newValue)
                            {
                                if (!oldValue)
                                {
                                    VIUSettings.activateOculusVRModule = true;
                                    VIUSettings.EnableOculusSDKControllerRenderModel = true;
                                    VIUSettings.oculusVRControllerPrefab = FindControllerRenderModelPrefab();
                                }
                            }
                            else
                            {
                                if (oldValue)
                                {
                                    VIUSettings.EnableOculusSDKControllerRenderModel = false;
                                }
                            }

                            if (newValue && VRModule.isOculusVRAvatarSupported)
                            {
                                VIUSettings.EnableOculusSDKControllerRenderModelSkeleton = EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelSkeletonTitle, VIUSettings.ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_TOOLTIP), VIUSettings.EnableOculusSDKControllerRenderModelSkeleton);
                            }
                            else
                            {
                                var tooltip = VRModule.isOculusVRAvatarSupported ? VIUSettings.ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_TOOLTIP : "Currently only support lagacy OVR Avatar in Oculus Integration SDK v38 or before";

                                var wasGUIEnabled = GUI.enabled;
                                GUI.enabled = false;
                                EditorGUILayout.ToggleLeft(new GUIContent(enableControllerRenderModelSkeletonTitle, tooltip), false);
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

                        // Custom Android manifest
                        EditorGUILayout.BeginHorizontal();

                        EditorGUIUtility.labelWidth = 230;
                        var style = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleLeft };
                        VIUSettings.oculusVRAndroidManifestPath = EditorGUILayout.DelayedTextField(new GUIContent("Customized AndroidManifest Path:", "Default path: " + defaultAndroidManifestPath), VIUSettings.oculusVRAndroidManifestPath, style);
                        if (GUILayout.Button("Open", new GUILayoutOption[] { GUILayout.Width(44), GUILayout.Height(18) }))
                        {
                            var path = EditorUtility.OpenFilePanel("Select AndroidManifest.xml", string.Empty, "xml");
                            if (path.Length != 0)
                            {
                                // make relative path if it is under Assets folder.
                                if (path.StartsWith(Application.dataPath))
                                {
                                    path = "Assets" + path.Substring(Application.dataPath.Length);
                                }
                                VIUSettings.oculusVRAndroidManifestPath = path;
                            }
                        }

                        EditorGUILayout.EndHorizontal();

                        // Custom Android manifest warnings
                        EditorGUILayout.BeginHorizontal();

                        if (!string.IsNullOrEmpty(VIUSettings.oculusVRAndroidManifestPath) && !File.Exists(VIUSettings.oculusVRAndroidManifestPath))
                        {
                            EditorGUILayout.HelpBox("File does not existed!", MessageType.Warning);
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel -= 2;
                    }
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }

                if (!support || !VIUSettings.EnableOculusSDKControllerRenderModel)
                {
                    VIUSettings.oculusVRControllerPrefab = null;
                }
            }

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                VIUSettings.oculusVRControllerPrefab = support && VIUSettings.EnableOculusSDKControllerRenderModel ? FindControllerRenderModelPrefab() : null;

                if (!support) { return; }

                if (File.Exists(VIUSettings.oculusVRAndroidManifestPath))
                {
                    File.Copy(VIUSettings.oculusVRAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                }
                else if (File.Exists(defaultAndroidManifestPath))
                {
                    File.Copy(defaultAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                }
            }

#if UNITY_2018_1_OR_NEWER
            public void OnPreprocessBuild(BuildReport report)
            {
                VIUSettings.oculusVRControllerPrefab = support && VIUSettings.EnableOculusSDKControllerRenderModel ? FindControllerRenderModelPrefab() : null;

                if (!support) { return; }

                if (File.Exists(VIUSettings.oculusVRAndroidManifestPath))
                {
                    if (Directory.Exists("Assets/Plugins/Android/AndroidManifest.xml"))
                    {
                        File.Copy(VIUSettings.oculusVRAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                    }
                    else
                    {
                        Directory.CreateDirectory("Assets/Plugins/Android/");
                        File.Copy(VIUSettings.oculusVRAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                    }
                }
                else if (File.Exists(defaultAndroidManifestPath))
                {
                    if (Directory.Exists("Assets/Plugins/Android/AndroidManifest.xml"))
                    {
                        File.Copy(defaultAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                    }
                    else
                    {
                        Directory.CreateDirectory("Assets/Plugins/Android/");
                        File.Copy(defaultAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                    }
                }
            }
#endif
#if UNITY_2020_3_OR_NEWER && VIU_OCULUSVR_20_0_OR_NEWER
            private GameObject FindControllerRenderModelPrefab()
            {
                var results = AssetDatabase.FindAssets("OVRControllerPrefab t:Prefab");
                if (results != null || results.Length > 0)
                {
                    foreach (var result in results)
                    {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(result));
                        if (go != null && go.TryGetComponent<OVRControllerHelper>(out _))
                        {
                            VIUSettings.oculusVRControllerPrefab = go;
                            return go;
                        }
                    }
                }
                return null;
            }
#else
            private GameObject FindControllerRenderModelPrefab() { return null; }
#endif
        }
    }
}