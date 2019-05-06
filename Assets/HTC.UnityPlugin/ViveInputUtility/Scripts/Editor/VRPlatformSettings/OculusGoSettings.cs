//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
                    if (lv2qualitySetting.FindPropertyRelative("blendWeights").intValue > 2) { return false; }
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

                    var blendWeightsProp = lv2qualitySetting.FindPropertyRelative("blendWeights");
                    if (blendWeightsProp.intValue > 2) { blendWeightsProp.intValue = 2; }

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
        private const string OCULUS_ANDROID_PACKAGE_NAME = "com.unity.xr.oculus.android";

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
        {
            public static OculusGoSettings instance { get; private set; }

            public OculusGoSettings() { instance = this; }

            public override int order { get { return 103; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

            public override bool canSupport
            {
                get
                {
#if UNITY_2018_1_OR_NEWER
                    return activeBuildTargetGroup == BuildTargetGroup.Standalone && VRModule.isOculusVRPluginDetected && PackageManagerHelper.IsPackageInList(OCULUS_ANDROID_PACKAGE_NAME);
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
                    if (!VIUSettings.activateOculusVRModule) { return false; }
                    if (!OculusSDK.enabled) { return false; }
                    if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel21) { return false; }
                    if (PlayerSettings.graphicsJobs) { return false; }
                    if ((PlayerSettings.colorSpace == ColorSpace.Linear || PlayerSettings.gpuSkinning) && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                    return true;
                }
                set
                {
                    if (support == value) { return; }

                    if (value)
                    {
                        supportWaveVR = false;
                        supportDaydream = false;

                        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel21)
                        {
                            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21;
                        }

                        PlayerSettings.graphicsJobs = false;

                        if (PlayerSettings.colorSpace == ColorSpace.Linear || PlayerSettings.gpuSkinning)
                        {
                            SetGraphicsAPI(BuildTarget.Android, GraphicsDeviceType.OpenGLES3);
                        }
                    }

                    OculusSDK.enabled = value;
                    VIUSettings.activateOculusVRModule = value;
                }
#else
                get { return false; }
                set { }
#endif
            }

            public override void OnPreferenceGUI()
            {
                const string title = "Oculus Go";
                if (canSupport)
                {
                    support = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(title), support);
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
                    else if (!PackageManagerHelper.IsPackageInList(OCULUS_ANDROID_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Oculus(Android) package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("Oculus(Android)", OCULUS_ANDROID_PACKAGE_NAME);
                    }
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