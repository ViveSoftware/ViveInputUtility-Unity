//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

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

#if UNITY_5_5_OR_NEWER
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Use Oculus Mobile recommended Quality Settings",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () =>
                {
                    var settingObj = GetQualitySettingsAsset();
                    settingObj.Update();

                    var qualitySettingsArray = settingObj.FindProperty("m_QualitySettings");
                    for (int i = 0, imax = qualitySettingsArray.arraySize; i < imax; ++i)
                    {
                        // Simple(level 2) is a good one to start from, it should be the only level that is checked.
                        var element = qualitySettingsArray.GetArrayElementAtIndex(i);
                        var excludedArray = element.FindPropertyRelative("excludedTargetPlatforms");

                        var foundExcludeAndroidPlatform = false;
                        for (int j = 0, jmax = excludedArray.arraySize; j < jmax; ++j)
                        {
                            if (excludedArray.GetArrayElementAtIndex(j).stringValue == "Android")
                            {
                                foundExcludeAndroidPlatform = true;
                                break;
                            }
                        }

                        if (i == 2) { if (foundExcludeAndroidPlatform) { return false; } }
                        else if (!foundExcludeAndroidPlatform) { return false; }
                    }

                    var lv2qualitySetting = qualitySettingsArray.GetArrayElementAtIndex(2);
                    if (lv2qualitySetting.FindPropertyRelative("pixelLightCount").intValue > 1) { return false; }
                    if (lv2qualitySetting.FindPropertyRelative("anisotropicTextures").intValue != (int)AnisotropicFiltering.Disable) { return false; }
                    var antiAliasingLevel = lv2qualitySetting.FindPropertyRelative("antiAliasing").intValue; if (antiAliasingLevel > 4 || antiAliasingLevel < 2) { return false; }
                    if (lv2qualitySetting.FindPropertyRelative("shadows").intValue >= (int)ShadowQuality.All) { return false; }
#if UNITY_2019_1_OR_NEWER
                    if (lv2qualitySetting.FindPropertyRelative("skinWeights").intValue > 2) { return false; }
#else
                    if (lv2qualitySetting.FindPropertyRelative("blendWeights").intValue > 2) { return false; }
#endif
                    if (lv2qualitySetting.FindPropertyRelative("vSyncCount").intValue != 0) { return false; }

                    return true;
                },
                setValueFunc = v =>
                {
                    if (!v) { return; }

                    var settingObj = GetQualitySettingsAsset();
                    settingObj.Update();

                    var qualitySettingsArray = settingObj.FindProperty("m_QualitySettings");
                    for (int i = 0, imax = qualitySettingsArray.arraySize; i < imax; ++i)
                    {
                        // Simple(level 2) is a good one to start from, it should be the only level that is checked.
                        var element = qualitySettingsArray.GetArrayElementAtIndex(i);
                        var excludedArray = element.FindPropertyRelative("excludedTargetPlatforms");

                        var excludeAndroidIndex = -1;
                        for (int j = 0, jmax = excludedArray.arraySize; j < jmax; ++j)
                        {
                            if (excludedArray.GetArrayElementAtIndex(j).stringValue == "Android")
                            {
                                excludeAndroidIndex = j;
                                break;
                            }
                        }

                        if (i == 2)
                        {
                            if (excludeAndroidIndex >= 0)
                            {
                                excludedArray.DeleteArrayElementAtIndex(excludeAndroidIndex);
                            }
                        }
                        else if (excludeAndroidIndex < 0)
                        {
                            excludedArray.arraySize += 1;
                            excludedArray.GetArrayElementAtIndex(excludedArray.arraySize - 1).stringValue = "Android";
                        }
                    }

                    var lv2qualitySetting = qualitySettingsArray.GetArrayElementAtIndex(2);

                    var pixelLightCountProp = lv2qualitySetting.FindPropertyRelative("pixelLightCount");
                    var pixelLightCount = pixelLightCountProp.intValue;
                    if (pixelLightCount > 1) { pixelLightCountProp.intValue = 1; }
                    else if (pixelLightCount < 0) { pixelLightCountProp.intValue = 0; }

                    lv2qualitySetting.FindPropertyRelative("anisotropicTextures").intValue = (int)AnisotropicFiltering.Disable;

                    var antiAliasingLevelProp = lv2qualitySetting.FindPropertyRelative("antiAliasing");
                    var antiAliasingLevel = antiAliasingLevelProp.intValue;
                    if (antiAliasingLevel != 2 || antiAliasingLevel != 4) { antiAliasingLevelProp.intValue = 4; }

                    var shadowsProp = lv2qualitySetting.FindPropertyRelative("shadows");
                    if (shadowsProp.intValue >= (int)ShadowQuality.All) { shadowsProp.intValue = (int)ShadowQuality.HardOnly; }

#if UNITY_2019_1_OR_NEWER
                    var blendWeightsProp = lv2qualitySetting.FindPropertyRelative("skinWeights");
                    if (blendWeightsProp.intValue > 2) { blendWeightsProp.intValue = 2; }
#else
                    var blendWeightsProp = lv2qualitySetting.FindPropertyRelative("blendWeights");
                    if (blendWeightsProp.intValue > 2) { blendWeightsProp.intValue = 2; }
#endif

                    lv2qualitySetting.FindPropertyRelative("vSyncCount").intValue = 0;

                    settingObj.ApplyModifiedProperties();
                },
                recommendedValue = true,
            });
#endif

#if UNITY_5_6_OR_NEWER
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

#if VIU_OCULUSVR
            private OVRProjectConfig m_oculusProjectConfig;

            private OVRProjectConfig oculusProjectConfig
            {
                get
                {
                    if (m_oculusProjectConfig == null)
                    {
                        m_oculusProjectConfig = OVRProjectConfig.GetProjectConfig();
                    }

                    return m_oculusProjectConfig;
                }
            }
#endif

            public static OculusGoSettings instance { get; private set; }

            public OculusGoSettings() { instance = this; }

            public override int order { get { return 103; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

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
#if UNITY_2019_3_OR_NEWER
                    return activeBuildTargetGroup == BuildTargetGroup.Android && (PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME) || PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME));
#elif UNITY_2018_1_OR_NEWER
                    return activeBuildTargetGroup == BuildTargetGroup.Android && (PackageManagerHelper.IsPackageInList(OCULUS_ANDROID_PACKAGE_NAME) || PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME));
#elif UNITY_5_6_OR_NEWER
                    return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isOculusVRPluginDetected;
#else
                    return false;
#endif
                }
            }

            public override bool support
            {
#if UNITY_5_6_OR_NEWER
                get
                {
                    if (!canSupport) { return false; }
                    if (PlayerSettings.Android.minSdkVersion < MIN_SUPPORTED_ANDROID_SDK_VERSION) { return false; }
                    if (PlayerSettings.graphicsJobs) { return false; }
                    if ((PlayerSettings.colorSpace == ColorSpace.Linear || PlayerSettings.gpuSkinning) && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }

#if UNITY_2019_3_OR_NEWER
                    bool supportOculusXR = (VIUSettings.activateOculusVRModule || VIUSettings.activateUnityXRModule) && XRPluginManagementUtils.IsXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_NAME, requirdPlatform);
                    bool supportOpenXR = VIUSettings.activateUnityXRModule && XRPluginManagementUtils.IsXRLoaderEnabled(UnityXRModule.OPENXR_LOADER_NAME, requirdPlatform);

                    return canSupport && (supportOculusXR || supportOpenXR);
#else
                    if (!VIUSettings.activateOculusVRModule) { return false; }
                    if (!OculusSDK.enabled) { return false; }

                    return true;
#endif
                }
                set
                {
                    if (support == value) { return; }

                    if (value)
                    {
                        supportWaveVR = false;
                        supportDaydream = false;

                        if (PlayerSettings.Android.minSdkVersion < MIN_SUPPORTED_ANDROID_SDK_VERSION)
                        {
                            PlayerSettings.Android.minSdkVersion = MIN_SUPPORTED_ANDROID_SDK_VERSION;
                        }

                        PlayerSettings.graphicsJobs = false;

                        if (PlayerSettings.colorSpace == ColorSpace.Linear || PlayerSettings.gpuSkinning)
                        {
                            SetGraphicsAPI(BuildTarget.Android, GraphicsDeviceType.OpenGLES3);
                        }
                    }

                    VIUSettings.activateOculusVRModule = value;
#if UNITY_2019_3_OR_NEWER
                    if (PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME))
                    {
                        XRPluginManagementUtils.SetXRLoaderEnabled(UnityXRModule.OPENXR_LOADER_CLASS_NAME, requirdPlatform, value);
                    }
                    else if (PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME))
                    {
                        XRPluginManagementUtils.SetXRLoaderEnabled(OculusVRModule.OCULUS_XR_LOADER_CLASS_NAME, requirdPlatform, value);
                    }

                    OculusSDK.enabled = value && !PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME);
                    VIUSettings.activateUnityXRModule = XRPluginManagementUtils.IsAnyXRLoaderEnabled(requirdPlatform);
#else
                    OculusSDK.enabled = value;
                    VIUSettings.activateUnityNativeVRModule = value;
#endif
                }
#else
                get { return false; }
                set { }
#endif
            }

            public int callbackOrder { get { return 0; } }

            public override void OnPreferenceGUI()
            {
                const string title = "Oculus Android";
                if (canSupport)
                {
                    var wasSupported = support;
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title, "Oculus Go, Oculus Quest"), wasSupported);
                    s_symbolChanged |= wasSupported != support;
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();

                    if (activeBuildTargetGroup != BuildTargetGroup.Android)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Android platform required."), false, GUILayout.Width(150f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(BuildTargetGroup.Android, BuildTarget.Android);
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
                    if (support) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel += 2;

#if VIU_OCULUSVR
                        // Hand tracking support
                        EditorGUILayout.BeginHorizontal();
                        OVRProjectConfig.HandTrackingSupport originalHandTrackingSupport = oculusProjectConfig.handTrackingSupport;
                        oculusProjectConfig.handTrackingSupport = (OVRProjectConfig.HandTrackingSupport) EditorGUILayout.EnumPopup("Hand Tracking Support: ", originalHandTrackingSupport);
                        
                        if (oculusProjectConfig.handTrackingSupport != originalHandTrackingSupport)
                        {
                            EditorUtility.SetDirty(oculusProjectConfig);
                        }
                        EditorGUILayout.EndHorizontal();
#else
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.HelpBox("Hand tracking not supported. Please install Oculus Integration.", MessageType.Info);
                        GUILayout.FlexibleSpace();

                        s_warningHeight = Mathf.Max(s_warningHeight, GUILayoutUtility.GetLastRect().height);
                        GUILayout.BeginVertical(GUILayout.Height(s_warningHeight));
                        GUILayout.FlexibleSpace();
                        ShowUrlLinkButton(URL_OCULUS_VR_PLUGIN, "Get Oculus Integration");
                        GUILayout.FlexibleSpace();
                        GUILayout.EndVertical();

                        EditorGUILayout.EndHorizontal();
#endif

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
                    if (support) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
                }
            }

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
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
        }
    }
}