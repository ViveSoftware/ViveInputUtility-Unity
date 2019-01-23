//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
#if UNITY_5_4_OR_NEWER
using UnityEditor.Rendering;
#endif
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
#if VIU_STEAMVR_2_0_0_OR_NEWER
using Valve.VR;
using HTC.UnityPlugin.Vive.SteamVRExtension;
#endif

namespace HTC.UnityPlugin.Vive
{
    [InitializeOnLoad]
    public class VIUVersionCheck : EditorWindow
    {
        [Serializable]
        private struct RepoInfo
        {
            public string tag_name;
            public string body;
        }

        private interface IPropSetting
        {
            bool SkipCheck();
            void UpdateCurrentValue();
            bool IsIgnored();
            bool IsUsingRecommendedValue();
            void DoDrawRecommend();
            void AcceptRecommendValue();
            void DoIgnore();
            void DeleteIgnore();
        }

        public class RecommendedSetting<T> : IPropSetting
        {
            private const string fmtTitle = "{0} (current = {1})";
            private const string fmtRecommendBtn = "Use recommended ({0})";
            private const string fmtRecommendBtnWithPosefix = "Use recommended ({0}) - {1}";

            private string m_settingTitle;
            private string m_settingTrimedTitle;
            private string ignoreKey { get { return m_settingTrimedTitle; } }

            public string settingTitle { get { return m_settingTitle; } set { m_settingTitle = value; m_settingTrimedTitle = value.Replace(" ", ""); } }
            public string recommendBtnPostfix = string.Empty;
            public string toolTip = string.Empty;
            public Func<bool> skipCheckFunc = null;
            public Func<T> recommendedValueFunc = null;
            public Func<T> currentValueFunc = null;
            public Action<T> setValueFunc = null;
            public T currentValue = default(T);
            public T recommendedValue = default(T);

            public T GetRecommended() { return recommendedValueFunc == null ? recommendedValue : recommendedValueFunc(); }

            public bool SkipCheck() { return skipCheckFunc == null ? false : skipCheckFunc(); }

            public bool IsIgnored() { return VIUProjectSettings.HasIgnoreKey(ignoreKey); }

            public bool IsUsingRecommendedValue() { return EqualityComparer<T>.Default.Equals(currentValue, GetRecommended()); }

            public void UpdateCurrentValue() { currentValue = currentValueFunc(); }

            public void DoDrawRecommend()
            {
                GUILayout.Label(new GUIContent(string.Format(fmtTitle, settingTitle, currentValue), toolTip));

                GUILayout.BeginHorizontal();

                bool recommendBtnClicked;
                if (string.IsNullOrEmpty(recommendBtnPostfix))
                {
                    recommendBtnClicked = GUILayout.Button(new GUIContent(string.Format(fmtRecommendBtn, GetRecommended()), toolTip));
                }
                else
                {
                    recommendBtnClicked = GUILayout.Button(new GUIContent(string.Format(fmtRecommendBtnWithPosefix, GetRecommended(), recommendBtnPostfix), toolTip));
                }

                if (recommendBtnClicked)
                {
                    AcceptRecommendValue();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("Ignore", toolTip)))
                {
                    DoIgnore();
                }

                GUILayout.EndHorizontal();
            }

            public void AcceptRecommendValue()
            {
                setValueFunc(GetRecommended());
            }

            public void DoIgnore()
            {
                VIUProjectSettings.AddIgnoreKey(ignoreKey);
            }

            public void DeleteIgnore()
            {
                VIUProjectSettings.RemoveIgnoreKey(ignoreKey);
            }
        }

#if VIU_STEAMVR_2_0_0_OR_NEWER
        private class RecommendedSteamVRInputFileSettings : RecommendedSetting<bool>
        {
            private readonly string m_mainDirPath;
            private readonly string m_partialDirPath;
            private readonly string m_partialFileName = "actions.json";
            private DateTime m_mainFileVersion;
            private DateTime m_partialFileVersion;
            private bool m_lastCheckMergedResult;

            private string mainFileName { get { return SteamVR_Settings.instance.actionsFilePath; } }

            private string exampleDirPath
            {
                get
                {
                    var monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
                    var monoScript = monoScripts.FirstOrDefault(script => script.GetClass() == typeof(SteamVR_Input));
                    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(monoScript)), SteamVR_CopyExampleInputFiles.exampleJSONFolderName));
                }
            }

            public RecommendedSteamVRInputFileSettings()
            {
                m_mainDirPath = Path.GetFullPath(Application.dataPath + "/../");
                m_partialDirPath = VIUProjectSettings.partialActionDirPath;
                m_partialFileName = VIUProjectSettings.partialActionFileName;

                settingTitle = "Apply VIU Action Set for SteamVR Input";
                skipCheckFunc = () => !VIUSettingsEditor.canSupportOpenVR;
                currentValueFunc = IsMerged;
                setValueFunc = Merge;
                recommendedValue = true;
            }

            private bool IsMerged()
            {
                VIUSteamVRActionFile mainFile;
                VIUSteamVRActionFile partialFile;

                if (!VIUSteamVRActionFile.TryLoad(m_mainDirPath, mainFileName, out mainFile)) { return false; }
                if (!VIUSteamVRActionFile.TryLoad(m_partialDirPath, m_partialFileName, out partialFile)) { return true; }

                if (m_mainFileVersion != mainFile.lastWriteTime || m_partialFileVersion != partialFile.lastWriteTime)
                {
                    m_mainFileVersion = mainFile.lastWriteTime;
                    m_partialFileVersion = partialFile.lastWriteTime;
                    m_lastCheckMergedResult = mainFile.IsMerged(partialFile);
                }

                return m_lastCheckMergedResult;
            }

            private void Merge(bool value)
            {
                if (!value) { return; }

                VIUSteamVRActionFile mainFile;
                VIUSteamVRActionFile exampleFile;
                VIUSteamVRActionFile partialFile;

                if (SteamVR_Input.actionFile != null)
                {
                    GetWindow<SteamVR_Input_EditorWindow>(false, "SteamVR Input", true).Close();
                }

                if (!VIUSteamVRActionFile.TryLoad(m_partialDirPath, m_partialFileName, out partialFile)) { return; }

                VIUSteamVRActionFile.TryLoad(m_mainDirPath, mainFileName, out mainFile);
                VIUSteamVRActionFile.TryLoad(exampleDirPath, mainFileName, out exampleFile);

                if (exampleFile != null && (mainFile == null || !mainFile.IsMerged(exampleFile)))
                {
                    if (EditorUtility.DisplayDialog("Import SteamVR Example Inputs", "Would you also like to import SteamVR Example Input File? Click yes if you want SteamVR plugin example scene to work.", "Yes", "No"))
                    {
                        if (mainFile == null)
                        {
                            mainFile = exampleFile;
                        }
                        else
                        {
                            mainFile.Merge(exampleFile);
                        }

                        EditorPrefs.SetBool(SteamVR_CopyExampleInputFiles.steamVRInputExampleJSONCopiedKey, true);
                    }
                }

                mainFile.Merge(partialFile);
                mainFile.Save(m_mainDirPath);

                m_mainFileVersion = m_partialFileVersion = default(DateTime);

                EditorApplication.delayCall += () =>
                {
                    GetWindow<SteamVR_Input_EditorWindow>(false, "SteamVR Input", true);
                    SteamVR_Input_Generator.BeginGeneration();
                };
            }
        }
#endif

        public const string lastestVersionUrl = "https://api.github.com/repos/ViveSoftware/ViveInputUtility-Unity/releases/latest";
        public const string pluginUrl = "https://github.com/ViveSoftware/ViveInputUtility-Unity/releases";
        public const double versionCheckIntervalMinutes = 30.0;

        private const string nextVersionCheckTimeKey = "ViveInputUtility.LastVersionCheckTime";
        private const string fmtIgnoreUpdateKey = "DoNotShowUpdate.v{0}";
        private static string ignoreThisVersionKey;
        private static SerializedObject projectSettingsAsset;
        private static SerializedObject qualitySettingsAsset;

        private static bool completeCheckVersionFlow = false;
        private static WWW www;
        private static RepoInfo latestRepoInfo;
        private static Version latestVersion;
        private static Vector2 releaseNoteScrollPosition;
        private static Vector2 settingScrollPosition;
        private static bool showNewVersion;
        private static bool toggleSkipThisVersion = false;
        private static VIUVersionCheck windowInstance;
        private static List<IPropSetting> s_settings;
        private Texture2D viuLogo;

        /// <summary>
        /// Count of settings that are ignored
        /// </summary>
        public static int ignoredSettingsCount { get; private set; }
        /// <summary>
        /// Count of settings that are not using recommended value
        /// </summary>
        public static int shouldNotifiedSettingsCount { get; private set; }
        /// <summary>
        /// Count of settings that are not ignored and not using recommended value
        /// </summary>
        public static int notifiedSettingsCount { get; private set; }

        public static bool recommendedWindowOpened { get { return windowInstance != null; } }

        private static SerializedObject GetPlayerSettings()
        {
            if (projectSettingsAsset == null)
            {
                projectSettingsAsset = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            }

            return projectSettingsAsset;
        }

        private static SerializedObject GetQualitySettingsAsset()
        {
            if (qualitySettingsAsset == null)
            {
                qualitySettingsAsset = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            }

            return qualitySettingsAsset;
        }

        static VIUVersionCheck()
        {
            EditorApplication.update += CheckVersionAndSettings;
        }

        public static void AddRecommendedSetting<T>(RecommendedSetting<T> setting)
        {
            InitializeSettins();
            s_settings.Add(setting);
        }

        private static void InitializeSettins()
        {
            if (s_settings != null) { return; }

            s_settings = new List<IPropSetting>();

            //s_settings.Add(new RecommendedSetting<bool>()
            //{
            //    settingTitle = "Virtual Reality Supported",
            //    currentValueFunc = () => VIUSettingsEditor.virtualRealitySupported,
            //    setValueFunc = v => VIUSettingsEditor.virtualRealitySupported = v,
            //    recommendedValue = true,
            //});

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Virtual Reality Supported with OpenVR",
                skipCheckFunc = () => !VIUSettingsEditor.canSupportOpenVR,
                currentValueFunc = () => VIUSettingsEditor.supportOpenVR,
                setValueFunc = v => VIUSettingsEditor.supportOpenVR = v,
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Virtual Reality Supported with Oculus",
                skipCheckFunc = () => !VIUSettingsEditor.canSupportOculus,
                currentValueFunc = () => VIUSettingsEditor.supportOculus,
                setValueFunc = v => VIUSettingsEditor.supportOculus = v,
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Virtual Reality Supported with Daydream",
                skipCheckFunc = () => !VIUSettingsEditor.canSupportDaydream,
                currentValueFunc = () => VIUSettingsEditor.supportDaydream,
                setValueFunc = v => VIUSettingsEditor.supportDaydream = v,
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<BuildTarget>()
            {
                settingTitle = "Build Target",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || VIUSettingsEditor.activeBuildTargetGroup != BuildTargetGroup.Standalone,
                currentValueFunc = () => EditorUserBuildSettings.activeBuildTarget,
                setValueFunc = v =>
                {
#if UNITY_2017_1_OR_NEWER
                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone, v);
#elif UNITY_5_6_OR_NEWER
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, v);
#else
                    EditorUserBuildSettings.SwitchActiveBuildTarget(v);
#endif
                },
                recommendedValue = BuildTarget.StandaloneWindows64,
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Load Binding Config on Start",
                skipCheckFunc = () => !VIUSettingsEditor.supportOpenVR,
                toolTip = "You can change this option later in Edit -> Preferences... -> VIU Settings.",
                currentValueFunc = () => VIUSettings.autoLoadBindingConfigOnStart,
                setValueFunc = v => { VIUSettings.autoLoadBindingConfigOnStart = v; },
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Binding Interface Switch",
                skipCheckFunc = () => !VIUSettingsEditor.supportOpenVR,
                toolTip = VIUSettings.BIND_UI_SWITCH_TOOLTIP + " You can change this option later in Edit -> Preferences... -> VIU Settings.",
                currentValueFunc = () => VIUSettings.enableBindingInterfaceSwitch,
                setValueFunc = v => { VIUSettings.enableBindingInterfaceSwitch = v; },
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "External Camera Switch",
                skipCheckFunc = () => !VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportOpenVR,
                toolTip = VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP + " You can change this option later in Edit -> Preferences... -> VIU Settings.",
                currentValueFunc = () => VIUSettings.enableExternalCameraSwitch,
                setValueFunc = v => { VIUSettings.enableExternalCameraSwitch = v; },
                recommendedValue = true,
            });

#if UNITY_5_3
            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Stereoscopic Rendering",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyVR,
                currentValueFunc = () => PlayerSettings.stereoscopic3D,
                setValueFunc = v => PlayerSettings.stereoscopic3D = v,
                recommendedValue = false,
            });
#endif

#if UNITY_5_3 || UNITY_5_4
            s_settings.Add(new RecommendedSetting<RenderingPath>()
            {
                settingTitle = "Rendering Path",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyVR,
                recommendBtnPostfix = "required for MSAA",
                currentValueFunc = () => PlayerSettings.renderingPath,
                setValueFunc = v => PlayerSettings.renderingPath = v,
                recommendedValue = RenderingPath.Forward,
            });

            // Unity 5.3 doesn't have SplashScreen for VR
            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Show Unity Splash Screen",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !InternalEditorUtility.HasPro() || !VIUSettingsEditor.supportAnyVR,
                currentValueFunc = () => PlayerSettings.showUnitySplashScreen,
                setValueFunc = v => PlayerSettings.showUnitySplashScreen = v,
                recommendedValue = false,
            });
#endif

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "GPU Skinning",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyVR,
                currentValueFunc = () => PlayerSettings.gpuSkinning,
                setValueFunc = v =>
                {
                    if (VIUSettingsEditor.supportAnyAndroidVR)
                    {
                        VIUSettingsEditor.SetGraphicsAPI(BuildTarget.Android, GraphicsDeviceType.OpenGLES3);
                    }
                    PlayerSettings.gpuSkinning = v;
                },
                recommendedValueFunc = () => !VIUSettingsEditor.supportWaveVR,
            });

            s_settings.Add(new RecommendedSetting<Vector2>()
            {
                settingTitle = "Default Screen Size",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => new Vector2(PlayerSettings.defaultScreenWidth, PlayerSettings.defaultScreenHeight),
                setValueFunc = v => { PlayerSettings.defaultScreenWidth = (int)v.x; PlayerSettings.defaultScreenHeight = (int)v.y; },
                recommendedValue = new Vector2(1024f, 768f),
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Run In Background",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.runInBackground,
                setValueFunc = v => PlayerSettings.runInBackground = v,
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<ResolutionDialogSetting>()
            {
                settingTitle = "Display Resolution Dialog",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.displayResolutionDialog,
                setValueFunc = v => PlayerSettings.displayResolutionDialog = v,
                recommendedValue = ResolutionDialogSetting.HiddenByDefault,
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Resizable Window",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.resizableWindow,
                setValueFunc = v => PlayerSettings.resizableWindow = v,
                recommendedValue = true,
            });

#if !UNITY_2018_1_OR_NEWER
            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Default Is Fullscreen",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.defaultIsFullScreen,
                setValueFunc = v => PlayerSettings.defaultIsFullScreen = v,
                recommendedValue = false,
            });

            s_settings.Add(new RecommendedSetting<D3D11FullscreenMode>()
            {
                settingTitle = "D3D11 Fullscreen Mode",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.d3d11FullscreenMode,
                setValueFunc = v => PlayerSettings.d3d11FullscreenMode = v,
                recommendedValue = D3D11FullscreenMode.FullscreenWindow,
            });
#else
            s_settings.Add(new RecommendedSetting<FullScreenMode>()
            {
                settingTitle = "Fullscreen Mode",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.fullScreenMode,
                setValueFunc = v => PlayerSettings.fullScreenMode = v,
                recommendedValue = FullScreenMode.FullScreenWindow,
            });
#endif

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Visible In Background",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.visibleInBackground,
                setValueFunc = v => PlayerSettings.visibleInBackground = v,
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<ColorSpace>()
            {
                settingTitle = "Color Space",
                skipCheckFunc = () => (VRModule.isSteamVRPluginDetected && VIUSettingsEditor.activeBuildTargetGroup == BuildTargetGroup.Standalone) || !VIUSettingsEditor.supportAnyVR,
                recommendBtnPostfix = "requires reloading scene",
                currentValueFunc = () => PlayerSettings.colorSpace,
                setValueFunc = v =>
                {
                    if (VIUSettingsEditor.supportAnyAndroidVR)
                    {
                        VIUSettingsEditor.SetGraphicsAPI(BuildTarget.Android, GraphicsDeviceType.OpenGLES3);
                    }
                    PlayerSettings.colorSpace = v;
                },
                recommendedValue = ColorSpace.Linear,
            });

            s_settings.Add(new RecommendedSetting<UIOrientation>()
            {
                settingTitle = "Default Interface Orientation",
                skipCheckFunc = () => !VIUSettingsEditor.supportWaveVR,
                currentValueFunc = () => PlayerSettings.defaultInterfaceOrientation,
                setValueFunc = v => PlayerSettings.defaultInterfaceOrientation = v,
                recommendedValue = UIOrientation.LandscapeLeft,
            });

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Multithreaded Rendering",
                skipCheckFunc = () => !VIUSettingsEditor.supportWaveVR || !VIUSettingsEditor.supportOculusGo,
#if UNITY_2017_2_OR_NEWER
                currentValueFunc = () => PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android),
                setValueFunc = v => PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, v),
#else
                currentValueFunc = () => PlayerSettings.mobileMTRendering,
                setValueFunc = v => PlayerSettings.mobileMTRendering = v,
#endif
                recommendedValue = true,
            });

#if UNITY_5_4_OR_NEWER
            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Graphic Jobs",
                skipCheckFunc = () => !VIUSettingsEditor.supportWaveVR || VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.graphicsJobs,
                setValueFunc = v => PlayerSettings.graphicsJobs = v,
                recommendedValue = true,
            });
#endif
            // Oculus mobile recommended settings
            // https://developer.oculus.com/blog/tech-note-unity-settings-for-mobile-vr/
            s_settings.Add(new RecommendedSetting<MobileTextureSubtarget>()
            {
                settingTitle = "Texture Compression",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => EditorUserBuildSettings.androidBuildSubtarget,
                setValueFunc = v => EditorUserBuildSettings.androidBuildSubtarget = v,
                recommendedValue = MobileTextureSubtarget.ASTC,
            });

            s_settings.Add(new RecommendedSetting<bool>()
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

            s_settings.Add(new RecommendedSetting<bool>()
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

            s_settings.Add(new RecommendedSetting<bool>()
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
            s_settings.Add(new RecommendedSetting<StereoRenderingPath>()
            {
                settingTitle = "Stereo Rendering Method",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.stereoRenderingPath,
                setValueFunc = v => PlayerSettings.stereoRenderingPath = v,
                recommendedValue = StereoRenderingPath.SinglePass,
            });
#endif

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Prebake Collision Meshes",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.bakeCollisionMeshes,
                setValueFunc = v => PlayerSettings.bakeCollisionMeshes = v,
                recommendedValue = true,
            });

            s_settings.Add(new RecommendedSetting<bool>()
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

            s_settings.Add(new RecommendedSetting<bool>()
            {
                settingTitle = "Optimize Mesh Data",
                skipCheckFunc = () => !VIUSettingsEditor.supportOculusGo,
                currentValueFunc = () => PlayerSettings.stripUnusedMeshComponents,
                setValueFunc = v => PlayerSettings.stripUnusedMeshComponents = v,
                recommendedValue = true,
            });

#if UNITY_5_5_OR_NEWER
            s_settings.Add(new RecommendedSetting<bool>()
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
            s_settings.Add(new RecommendedSetting<bool>()
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

#if VIU_STEAMVR_2_0_0_OR_NEWER
            s_settings.Add(new RecommendedSteamVRInputFileSettings());
#endif
        }

        private static void WrightVersionCheckLog(string msg)
        {
#if VIU_PRINT_FETCH_VERSION_LOG
            using (var outputFile = new StreamWriter("VIUVersionCheck.log", true))
            {
                outputFile.WriteLine(DateTime.Now.ToString() + " - " + msg + ". Stop fetching until " + UtcDateTimeFromStr(EditorPrefs.GetString(nextVersionCheckTimeKey)).ToLocalTime().ToString());
            }
#endif
        }

        // check vive input utility version on github
        private static void CheckVersionAndSettings()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= CheckVersionAndSettings;
                return;
            }

            InitializeSettins();

            // fetch new version info from github release site
            if (!completeCheckVersionFlow && VIUSettings.autoCheckNewVIUVersion)
            {
                if (www == null) // web request not running
                {
                    if (EditorPrefs.HasKey(nextVersionCheckTimeKey) && DateTime.UtcNow < UtcDateTimeFromStr(EditorPrefs.GetString(nextVersionCheckTimeKey)))
                    {
                        WrightVersionCheckLog("Skipped");
                        completeCheckVersionFlow = true;
                        return;
                    }

                    www = new WWW(lastestVersionUrl);
                }

                if (!www.isDone)
                {
                    return;
                }

                // On Windows, PlaterSetting is stored at \HKEY_CURRENT_USER\Software\Unity Technologies\Unity Editor 5.x
                EditorPrefs.SetString(nextVersionCheckTimeKey, UtcDateTimeToStr(DateTime.UtcNow.AddMinutes(versionCheckIntervalMinutes)));

                if (UrlSuccess(www))
                {
                    latestRepoInfo = JsonUtility.FromJson<RepoInfo>(www.text);
                    WrightVersionCheckLog("Fetched");
                }

                // parse latestVersion and ignoreThisVersionKey
                if (!string.IsNullOrEmpty(latestRepoInfo.tag_name))
                {
                    try
                    {
                        latestVersion = new Version(Regex.Replace(latestRepoInfo.tag_name, "[^0-9\\.]", string.Empty));
                        ignoreThisVersionKey = string.Format(fmtIgnoreUpdateKey, latestVersion.ToString());
                    }
                    catch
                    {
                        latestVersion = default(Version);
                        ignoreThisVersionKey = string.Empty;
                    }
                }

                www.Dispose();
                www = null;

                completeCheckVersionFlow = true;
            }

            showNewVersion = !string.IsNullOrEmpty(ignoreThisVersionKey) && !VIUProjectSettings.HasIgnoreKey(ignoreThisVersionKey) && latestVersion > VIUVersion.current;

            UpdateIgnoredNotifiedSettingsCount(false);

            if (showNewVersion || notifiedSettingsCount > 0)
            {
                TryOpenRecommendedSettingWindow();
            }

            EditorApplication.update -= CheckVersionAndSettings;
        }

        public static void UpdateIgnoredNotifiedSettingsCount(bool drawNotifiedPrompt)
        {
            InitializeSettins();

            ignoredSettingsCount = 0;
            shouldNotifiedSettingsCount = 0;
            notifiedSettingsCount = 0;

            foreach (var setting in s_settings)
            {
                if (setting.SkipCheck()) { continue; }

                setting.UpdateCurrentValue();

                var isIgnored = setting.IsIgnored();
                if (isIgnored) { ++ignoredSettingsCount; }

                if (setting.IsUsingRecommendedValue()) { continue; }
                else { ++shouldNotifiedSettingsCount; }

                if (!isIgnored)
                {
                    ++notifiedSettingsCount;

                    if (drawNotifiedPrompt)
                    {
                        if (notifiedSettingsCount == 1)
                        {
                            EditorGUILayout.HelpBox("Recommended project settings:", MessageType.Warning);

                            settingScrollPosition = GUILayout.BeginScrollView(settingScrollPosition, GUILayout.ExpandHeight(true));
                        }

                        setting.DoDrawRecommend();
                    }

                }
            }
        }

        // Open recommended setting window (with possible new version prompt)
        // won't do any thing if the window is already opened
        public static void TryOpenRecommendedSettingWindow()
        {
            if (recommendedWindowOpened) { return; }

            windowInstance = GetWindow<VIUVersionCheck>(true, "Vive Input Utility");
            windowInstance.minSize = new Vector2(240f, 550f);
            var rect = windowInstance.position;
            windowInstance.position = new Rect(Mathf.Max(rect.x, 50f), Mathf.Max(rect.y, 50f), rect.width, 200f + (showNewVersion ? 700f : 400f));
        }

        private static DateTime UtcDateTimeFromStr(string str)
        {
            var utcTicks = default(long);
            if (string.IsNullOrEmpty(str) || !long.TryParse(str, out utcTicks)) { return DateTime.MinValue; }
            return new DateTime(utcTicks, DateTimeKind.Utc);
        }

        private static string UtcDateTimeToStr(DateTime utcDateTime)
        {
            return utcDateTime.Ticks.ToString();
        }

        private static bool UrlSuccess(WWW www)
        {
            try
            {
                if (!string.IsNullOrEmpty(www.error))
                {
                    // API rate limit exceeded, see https://developer.github.com/v3/#rate-limiting
                    Debug.Log("url:" + www.url);
                    Debug.Log("error:" + www.error);
                    Debug.Log(www.text);

                    if (www.responseHeaders != null)
                    {
                        string responseHeader;
                        if (www.responseHeaders.TryGetValue("X-RateLimit-Limit", out responseHeader))
                        {
                            Debug.Log("X-RateLimit-Limit:" + responseHeader);
                        }
                        if (www.responseHeaders.TryGetValue("X-RateLimit-Remaining", out responseHeader))
                        {
                            Debug.Log("X-RateLimit-Remaining:" + responseHeader);
                        }
                        if (www.responseHeaders.TryGetValue("X-RateLimit-Reset", out responseHeader))
                        {
                            Debug.Log("X-RateLimit-Reset:" + TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(double.Parse(responseHeader))).ToString());
                        }
                    }
                    WrightVersionCheckLog("Failed. Rate limit exceeded");
                    return false;
                }

                if (Regex.IsMatch(www.text, "404 not found", RegexOptions.IgnoreCase))
                {
                    Debug.Log("url:" + www.url);
                    Debug.Log("error:" + www.error);
                    Debug.Log(www.text);
                    WrightVersionCheckLog("Failed. 404 not found");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                WrightVersionCheckLog("Failed. " + e.ToString());
                return false;
            }

            return true;
        }

        private string GetResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Scripts/Editor".Length) + "Textures/";
        }

        public void OnGUI()
        {
#if UNITY_2017_1_OR_NEWER
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.LabelField("Compiling...");
                return;
            }
#endif
            if (viuLogo == null)
            {
                var currentDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
                var texturePath = currentDir.Substring(0, currentDir.Length - "Scripts/Editor".Length) + "Textures/VIU_logo.png";
                viuLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }

            if (viuLogo != null)
            {
                GUI.DrawTexture(GUILayoutUtility.GetRect(position.width, 124, GUI.skin.box), viuLogo, ScaleMode.ScaleToFit);
            }

            if (showNewVersion)
            {
                EditorGUILayout.HelpBox("New version available:", MessageType.Warning);

                GUILayout.Label("Current version: " + VIUVersion.current);
                GUILayout.Label("New version: " + latestVersion);

                if (!string.IsNullOrEmpty(latestRepoInfo.body))
                {
                    GUILayout.Label("Release notes:");
                    releaseNoteScrollPosition = GUILayout.BeginScrollView(releaseNoteScrollPosition, GUILayout.Height(250f));
                    EditorGUILayout.HelpBox(latestRepoInfo.body, MessageType.None);
                    GUILayout.EndScrollView();
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(new GUIContent("Get Latest Version", "Goto " + pluginUrl)))
                    {
                        Application.OpenURL(pluginUrl);
                    }

                    GUILayout.FlexibleSpace();

                    toggleSkipThisVersion = GUILayout.Toggle(toggleSkipThisVersion, "Do not prompt for this version again.");
                }
                GUILayout.EndHorizontal();
            }

            UpdateIgnoredNotifiedSettingsCount(true);

            if (notifiedSettingsCount > 0)
            {
                GUILayout.EndScrollView();

                if (ignoredSettingsCount > 0)
                {
                    if (GUILayout.Button("Clear All Ignores(" + ignoredSettingsCount + ")"))
                    {
                        foreach (var setting in s_settings) { setting.DeleteIgnore(); }
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Accept All(" + notifiedSettingsCount + ")"))
                    {
                        for (int i = 10; i >= 0 && notifiedSettingsCount > 0; --i)
                        {
                            foreach (var setting in s_settings) { if (!setting.SkipCheck() && !setting.IsIgnored()) { setting.AcceptRecommendValue(); } }

                            VIUSettingsEditor.ApplySDKChanges();

                            UpdateIgnoredNotifiedSettingsCount(false);
                        }
                    }

                    if (GUILayout.Button("Ignore All(" + notifiedSettingsCount + ")"))
                    {
                        foreach (var setting in s_settings) { if (!setting.SkipCheck() && !setting.IsIgnored() && !setting.IsUsingRecommendedValue()) { setting.DoIgnore(); } }
                    }
                }
                GUILayout.EndHorizontal();
            }
            else if (shouldNotifiedSettingsCount > 0)
            {
                EditorGUILayout.HelpBox("Some recommended settings ignored.", MessageType.Warning);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear All Ignores(" + ignoredSettingsCount + ")"))
                {
                    foreach (var setting in s_settings) { setting.DeleteIgnore(); }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All recommended settings applied.", MessageType.Info);

                GUILayout.FlexibleSpace();
            }

            VIUSettingsEditor.ApplySDKChanges();

            if (VIUProjectSettings.hasChanged)
            {
                // save ignore keys
                VIUProjectSettings.Save();
            }

            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            if (viuLogo != null)
            {
                viuLogo = null;
            }

            if (showNewVersion && toggleSkipThisVersion && !string.IsNullOrEmpty(ignoreThisVersionKey))
            {
                showNewVersion = false;
                VIUProjectSettings.AddIgnoreKey(ignoreThisVersionKey);
                VIUProjectSettings.Save();
            }

            if (windowInstance == this)
            {
                windowInstance = null;
            }
        }
    }
}