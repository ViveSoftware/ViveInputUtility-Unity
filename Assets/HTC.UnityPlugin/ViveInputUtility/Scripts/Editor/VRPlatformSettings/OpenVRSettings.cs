//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_2018_1_OR_NEWER
using HTC.UnityPlugin.UPMRegistryTool.Editor.Utils;
#endif

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_STEAMVR_2_0_0_OR_NEWER
using Valve.VR;
using HTC.UnityPlugin.Vive.SteamVRExtension;
#endif

namespace HTC.UnityPlugin.Vive
{
    public class OpenVRRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
#if VIU_STEAMVR_2_0_0_OR_NEWER
        private class RecommendedSteamVRInputFileSettings : VIUVersionCheck.RecommendedSetting<bool>
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
#if VIU_STEAMVR_2_4_0_OR_NEWER
                m_mainDirPath = Path.GetDirectoryName(SteamVR_Input.GetActionsFilePath());
#else
                m_mainDirPath = Path.GetFullPath(Application.dataPath + "/../");
#endif
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
#if VIU_STEAMVR_2_1_0_OR_NEWER
                if (SteamVR_Input.actions == null || SteamVR_Input.actions.Length == 0) { return false; }
#endif
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

                if (!Directory.Exists(m_mainDirPath))
                {
                    Directory.CreateDirectory(m_mainDirPath);
                }

                VIUSteamVRActionFile mainFile;
                VIUSteamVRActionFile exampleFile;
                VIUSteamVRActionFile partialFile;

                if (SteamVR_Input.actionFile != null)
                {
                    EditorWindow.GetWindow<SteamVR_Input_EditorWindow>(false, "SteamVR Input", true).Close();
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
                    EditorWindow.GetWindow<SteamVR_Input_EditorWindow>(false, "SteamVR Input", true);
                    SteamVR_Input_Generator.BeginGeneration();
                };
            }
        }
#endif
        public OpenVRRecommendedSettings()
        {
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Virtual Reality Supported with OpenVR",
                skipCheckFunc = () => !VIUSettingsEditor.canSupportOpenVR,
                currentValueFunc = () => VIUSettingsEditor.supportOpenVR,
                setValueFunc = v => VIUSettingsEditor.supportOpenVR = v,
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<BuildTarget>()
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

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Load Binding Config on Start",
                skipCheckFunc = () => !VIUSettingsEditor.supportOpenVR,
                toolTip = "You can change this option later in Edit -> Preferences... -> VIU Settings.",
                currentValueFunc = () => VIUSettings.autoLoadBindingConfigOnStart,
                setValueFunc = v => { VIUSettings.autoLoadBindingConfigOnStart = v; },
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Binding Interface Switch",
                skipCheckFunc = () => !VIUSettingsEditor.supportOpenVR,
                toolTip = VIUSettings.BIND_UI_SWITCH_TOOLTIP + " You can change this option later in Edit -> Preferences... -> VIU Settings.",
                currentValueFunc = () => VIUSettings.enableBindingInterfaceSwitch,
                setValueFunc = v => { VIUSettings.enableBindingInterfaceSwitch = v; },
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "External Camera Switch",
                skipCheckFunc = () => !VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportOpenVR,
                toolTip = VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP + " You can change this option later in Edit -> Preferences... -> VIU Settings.",
                currentValueFunc = () => VIUSettings.enableExternalCameraSwitch,
                setValueFunc = v => { VIUSettings.enableExternalCameraSwitch = v; },
                recommendedValue = true,
            });

#if UNITY_5_3
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Stereoscopic Rendering",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyVR,
                currentValueFunc = () => PlayerSettings.stereoscopic3D,
                setValueFunc = v => PlayerSettings.stereoscopic3D = v,
                recommendedValue = false,
            });
#endif

#if UNITY_5_3 || UNITY_5_4
            Add(new VIUVersionCheck.RecommendedSetting<RenderingPath>()
            {
                settingTitle = "Rendering Path",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyVR,
                recommendBtnPostfix = "required for MSAA",
                currentValueFunc = () => PlayerSettings.renderingPath,
                setValueFunc = v => PlayerSettings.renderingPath = v,
                recommendedValue = RenderingPath.Forward,
            });

            // Unity 5.3 doesn't have SplashScreen for VR
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Show Unity Splash Screen",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !InternalEditorUtility.HasPro() || !VIUSettingsEditor.supportAnyVR,
                currentValueFunc = () => PlayerSettings.showUnitySplashScreen,
                setValueFunc = v => PlayerSettings.showUnitySplashScreen = v,
                recommendedValue = false,
            });
#endif

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
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

            Add(new VIUVersionCheck.RecommendedSetting<Vector2>()
            {
                settingTitle = "Default Screen Size",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => new Vector2(PlayerSettings.defaultScreenWidth, PlayerSettings.defaultScreenHeight),
                setValueFunc = v => { PlayerSettings.defaultScreenWidth = (int)v.x; PlayerSettings.defaultScreenHeight = (int)v.y; },
                recommendedValue = new Vector2(1024f, 768f),
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Run In Background",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.runInBackground,
                setValueFunc = v => PlayerSettings.runInBackground = v,
                recommendedValue = true,
            });

#if !UNITY_2019_2_OR_NEWER
            Add(new VIUVersionCheck.RecommendedSetting<ResolutionDialogSetting>()
            {
                settingTitle = "Display Resolution Dialog",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.displayResolutionDialog,
                setValueFunc = v => PlayerSettings.displayResolutionDialog = v,
                recommendedValue = ResolutionDialogSetting.HiddenByDefault,
            });
#endif

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Resizable Window",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.resizableWindow,
                setValueFunc = v => PlayerSettings.resizableWindow = v,
                recommendedValue = true,
            });

#if !UNITY_2018_1_OR_NEWER
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Default Is Fullscreen",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.defaultIsFullScreen,
                setValueFunc = v => PlayerSettings.defaultIsFullScreen = v,
                recommendedValue = false,
            });

            Add(new VIUVersionCheck.RecommendedSetting<D3D11FullscreenMode>()
            {
                settingTitle = "D3D11 Fullscreen Mode",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.d3d11FullscreenMode,
                setValueFunc = v => PlayerSettings.d3d11FullscreenMode = v,
                recommendedValue = D3D11FullscreenMode.FullscreenWindow,
            });
#else
            Add(new VIUVersionCheck.RecommendedSetting<FullScreenMode>()
            {
                settingTitle = "Fullscreen Mode",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.fullScreenMode,
                setValueFunc = v => PlayerSettings.fullScreenMode = v,
                recommendedValue = FullScreenMode.FullScreenWindow,
            });
#endif

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Visible In Background",
                skipCheckFunc = () => VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyStandaloneVR,
                currentValueFunc = () => PlayerSettings.visibleInBackground,
                setValueFunc = v => PlayerSettings.visibleInBackground = v,
                recommendedValue = true,
            });

            Add(new VIUVersionCheck.RecommendedSetting<ColorSpace>()
            {
                settingTitle = "Color Space",
                skipCheckFunc = () => VIUSettingsEditor.activeBuildTargetGroup != BuildTargetGroup.Standalone || VRModule.isSteamVRPluginDetected || !VIUSettingsEditor.supportAnyVR,
                recommendBtnPostfix = "requires reloading scene",
                currentValueFunc = () => PlayerSettings.colorSpace,
                setValueFunc = v => PlayerSettings.colorSpace = v,
                recommendedValue = ColorSpace.Linear,
            });
#if VIU_STEAMVR_2_0_0_OR_NEWER
            Add(new RecommendedSteamVRInputFileSettings());
#endif
        }
    }

    public static partial class VIUSettingsEditor
    {
        public const string URL_STEAM_VR_PLUGIN = "https://assetstore.unity.com/packages/slug/32647";

        private const string OPENVR_PACKAGE_NAME = "com.unity.xr.openvr.standalone";
        private const string OPENVR_XR_PACKAGE_NAME_OLD = "com.valve.openvr";
        private const string OPENVR_XR_PACKAGE_NAME = "com.valvesoftware.unity.openvr";

#if UNITY_2019_3_OR_NEWER
        private static readonly RegistryInfo ValveRegistry = new RegistryInfo
        {
            Name = "Valve",
            Url = "https://registry.npmjs.org/",
            Scopes = new List<string>
            {
                "com.valvesoftware",
                "com.valvesoftware.unity.openvr",
            },
        };
#endif

        public static bool canSupportOpenVR
        {
            get { return OpenVRSettings.instance.canSupport; }
        }

        public static bool supportOpenVR
        {
            get { return OpenVRSettings.instance.support; }
            set { OpenVRSettings.instance.support = value; }
        }

        private class OpenVRSettings : VRPlatformSetting
        {
            private Foldouter m_foldouter = new Foldouter();

            public static OpenVRSettings instance { get; private set; }

            public OpenVRSettings() { instance = this; }

            public override int order { get { return 1; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Standalone; } }

            public override bool canSupport
            {
                get
                {
                    if (activeBuildTargetGroup != requirdPlatform) { return false; }
#if UNITY_2019_3_OR_NEWER
                    return PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME) || PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME_OLD);
#elif UNITY_2018_1_OR_NEWER
                    return PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME) || PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME_OLD) || PackageManagerHelper.IsPackageInList(OPENVR_PACKAGE_NAME);
#elif UNITY_5_5_OR_NEWER
                    return true;
#else
                    return VRModule.isSteamVRPluginDetected;
#endif
                }
            }

            public override bool support
            {
                get
                {
                    if (!canSupport) { return false; }
#if UNITY_2019_3_OR_NEWER
                    return (VIUSettings.activateSteamVRModule || VIUSettings.activateUnityXRModule) && XRPluginManagementUtils.IsXRLoaderEnabled(SteamVRModule.OPENVR_XR_LOADER_NAME, SteamVRModule.OPENVR_XR_LOADER_CLASS_NAME, requirdPlatform);
#elif UNITY_5_5_OR_NEWER
                    return (VIUSettings.activateSteamVRModule || VIUSettings.activateUnityNativeVRModule) && OpenVRSDK.enabled;
#elif UNITY_5_4_OR_NEWER
                    return VIUSettings.activateSteamVRModule && OpenVRSDK.enabled;
#else
                    return VIUSettings.activateSteamVRModule && !virtualRealitySupported;
#endif
                }
                set
                {
                    if (support == value) { return; }

                    VIUSettings.activateSteamVRModule = value;
#if UNITY_2019_3_OR_NEWER
                    if (PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME) || PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME_OLD))
                    {
                        XRPluginManagementUtils.SetXRLoaderEnabled(SteamVRModule.OPENVR_XR_LOADER_CLASS_NAME, requirdPlatform, value);
                    }

                    OpenVRSDK.enabled = value && (!PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME) || !PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME_OLD));

                    VIUSettings.activateUnityXRModule = XRPluginManagementUtils.IsAnyXRLoaderEnabled(requirdPlatform);
#elif UNITY_5_5_OR_NEWER
                    OpenVRSDK.enabled = value;
                    VIUSettings.activateUnityNativeVRModule = value || supportOculus;
#elif UNITY_5_4_OR_NEWER
                    OpenVRSDK.enabled = value;
#else
                    if (value)
                    {
                        virtualRealitySupported = false;
                    }
#endif

#if VIU_STEAMVR_2_2_0_OR_NEWER
                    SteamVR_Settings.instance.autoEnableVR = false;
                    EditorUtility.SetDirty(SteamVR_Settings.instance);
                    AssetDatabase.SaveAssets();
#elif VIU_STEAMVR_1_2_1_OR_NEWER && !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
                    SteamVR_Preferences.AutoEnableVR = false;
#endif
                }
            }

            public override void OnPreferenceGUI()
            {
                const string title = "OpenVR";
                if (canSupport)
                {
                    var wasSupported = support;
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title, "VIVE, VIVE Pro, VIVE Pro Eye, VIVE Cosmos\nOculus Rift, Oculus Rift S, Windows MR"), wasSupported);
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
#if UNITY_2019_3_OR_NEWER && FALSE // openvr xr plugin on Valve registry is obsolete
                    else if (!PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "OpenVR XR Plugin package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Add OpenVR XR Plugin Package", "Add " + OPENVR_XR_PACKAGE_NAME + " to Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            if (!ManifestUtils.CheckRegistryExists(ValveRegistry))
                            {
                                ManifestUtils.AddRegistry(ValveRegistry);
                            }

                            PackageManagerHelper.AddToPackageList(OPENVR_XR_PACKAGE_NAME);
                            VIUProjectSettings.Instance.isInstallingOpenVRXRPlugin = true;
                        }
                    }
#elif UNITY_2018_2_OR_NEWER && FALSE // obsolete
                    else if (!PackageManagerHelper.IsPackageInList(OPENVR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "OpenVR (Desktop) package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowAddPackageButton("OpenVR (Desktop)", OPENVR_PACKAGE_NAME);
                    }
#endif
                    else if (!VRModule.isSteamVRPluginDetected)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "SteamVR Plugin required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowUrlLinkButton(URL_STEAM_VR_PLUGIN);
                    }

                    GUILayout.EndHorizontal();
                }

                if (support && m_foldouter.isExpended)
                {
                    EditorGUI.indentLevel += 2;

                    // Vive Hand Tracking Submodule
                    const string vhtSdkUrl = "https://developer.vive.com/resources/vive-sense/sdk/vive-hand-tracking-sdk/";
                    const string vhtTitle = "Enable Vive Hand Tracking";
                    if (!VRModule.isViveHandTrackingDetected)
                    {
                        GUILayout.BeginHorizontal();
                        GUI.enabled = false;
                        EditorGUILayout.ToggleLeft(new GUIContent(vhtTitle, "Vive Hand Tracking SDK required"), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowUrlLinkButton(vhtSdkUrl);
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        VRModuleSettings.activateViveHandTrackingSubmodule = EditorGUILayout.ToggleLeft(new GUIContent(vhtTitle, "Works on Vive, VIVE Pro, Vive Pro Eye, VIVE Cosmos, VIVE Cosmos XR and Valve Index"), VRModuleSettings.activateViveHandTrackingSubmodule);
                        s_guiChanged |= EditorGUI.EndChangeCheck();
                    }

                    if (VRModule.isSteamVRPluginDetected) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }

                    // Skeleton mode
                    VIUSettings.steamVRLeftSkeletonMode = (SteamVRSkeletonMode)EditorGUILayout.EnumPopup(new GUIContent("Left Controller Skeleton", "This effects RenderModelHook's behaviour"), VIUSettings.steamVRLeftSkeletonMode);
                    VIUSettings.steamVRRightSkeletonMode = (SteamVRSkeletonMode)EditorGUILayout.EnumPopup(new GUIContent("Right Controller Skeleton", "This effects RenderModelHook's behaviour"), VIUSettings.steamVRRightSkeletonMode);

                    VIUSettings.autoLoadExternalCameraConfigOnStart = EditorGUILayout.ToggleLeft(new GUIContent("Load Config and Enable External Camera on Start", "You can also load config by calling ExternalCameraHook.LoadConfigFromFile(path) in script."), VIUSettings.autoLoadExternalCameraConfigOnStart);
                    if (!VIUSettings.autoLoadExternalCameraConfigOnStart) { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel++;

                        EditorGUI.BeginChangeCheck();
                        VIUSettings.externalCameraConfigFilePath = EditorGUILayout.DelayedTextField(new GUIContent("Config Path"), VIUSettings.externalCameraConfigFilePath);
                        if (string.IsNullOrEmpty(VIUSettings.externalCameraConfigFilePath))
                        {
                            VIUSettings.externalCameraConfigFilePath = VIUSettings.EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE;
                            EditorGUI.EndChangeCheck();
                        }
                        else if (EditorGUI.EndChangeCheck() && VIUSettings.externalCameraConfigFilePath.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                        {
                            VIUSettings.externalCameraConfigFilePath = VIUSettings.EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE;
                        }
                        // Create button that writes default config file
                        if (VIUSettings.autoLoadExternalCameraConfigOnStart && !File.Exists(VIUSettings.externalCameraConfigFilePath))
                        {
                            if (VRModule.isSteamVRPluginDetected) { s_guiChanged |= EditorGUI.EndChangeCheck(); }
                            ShowCreateExCamCfgButton();
                            if (VRModule.isSteamVRPluginDetected) { EditorGUI.BeginChangeCheck(); }
                        }

                        EditorGUI.indentLevel--;
                    }
                    if (!VIUSettings.autoLoadExternalCameraConfigOnStart) { GUI.enabled = true; }

                    VIUSettings.enableExternalCameraSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable External Camera Switch", VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP), VIUSettings.enableExternalCameraSwitch);
                    if (!VIUSettings.enableExternalCameraSwitch) { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel++;

                        VIUSettings.externalCameraSwitchKey = (KeyCode)EditorGUILayout.EnumPopup("Switch Key", VIUSettings.externalCameraSwitchKey);
                        VIUSettings.externalCameraSwitchKeyModifier = (KeyCode)EditorGUILayout.EnumPopup("Switch Key Modifier", VIUSettings.externalCameraSwitchKeyModifier);

                        EditorGUI.indentLevel--;
                    }
                    if (!VIUSettings.enableExternalCameraSwitch) { GUI.enabled = true; }

                    EditorGUI.indentLevel -= 2;

                    if (VRModule.isSteamVRPluginDetected) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
                }

                if (support && !VRModule.isSteamVRPluginDetected && !PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME))
                {
                    EditorGUI.indentLevel += 2;

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(
#if VIU_XR_GENERAL_SETTINGS
                        "Input" +
#elif UNITY_2017_1_OR_NEWER
                        "External-Camera(Mix-Reality), animated controller model" + 
                        ", VIVE Controller haptics(vibration)" +
                        ", VIVE Tracker USB/Pogo-pin input" +
#else
                        "External-Camera(Mix-Reality), animated controller model" + 
                        ", VIVE Controller haptics(vibration)" +
                        ", VIVE Tracker device" +
#endif
                        " NOT supported! " +
                        "Install SteamVR Plugin to get support."
                        , MessageType.Warning);

                    s_warningHeight = Mathf.Max(s_warningHeight, GUILayoutUtility.GetLastRect().height);
                    GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical(GUILayout.Height(s_warningHeight));
                    GUILayout.FlexibleSpace();
                    ShowUrlLinkButton(URL_STEAM_VR_PLUGIN, "Get SteamVR Plugin");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();

                    EditorGUI.indentLevel -= 2;
                }

#if UNITY_2019_3_OR_NEWER
                if (VIUProjectSettings.Instance.isInstallingOpenVRXRPlugin)
                {
                    bool isPackageInstalled = PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME) ||
                                              PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME_OLD);
                    bool isLoaderEnabled = XRPluginManagementUtils.IsXRLoaderEnabled(SteamVRModule.OPENVR_XR_LOADER_NAME, SteamVRModule.OPENVR_XR_LOADER_CLASS_NAME, BuildTargetGroup.Standalone);
                    if (isPackageInstalled && !isLoaderEnabled)
                    {
                        XRPluginManagementUtils.SetXRLoaderEnabled(SteamVRModule.OPENVR_XR_LOADER_CLASS_NAME, BuildTargetGroup.Standalone, true);
                        OpenVRSDK.enabled = true;

                        VIUProjectSettings.Instance.isInstallingOpenVRXRPlugin = false;
                    }
                }
#endif
            }
        }
    }
}