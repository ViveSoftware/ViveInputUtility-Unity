//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
#if UNITY_5_6_OR_NEWER
using UnityEditor.Build;
#endif
#if UNITY_2018_1_OR_NEWER
using HTC.UnityPlugin.UPMRegistryTool.Editor.Utils;
using HTC.UnityPlugin.UPMRegistryTool.Editor.Configs;
using UnityEditor.Build.Reporting;
#endif
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTC.UnityPlugin.Vive
{
    public class WaveVRRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
        public WaveVRRecommendedSettings()
        {
            Add(new VIUVersionCheck.RecommendedSetting<UIOrientation>()
            {
                settingTitle = "Default Interface Orientation",
                skipCheckFunc = () => !VIUSettingsEditor.supportWaveVR,
                currentValueFunc = () => PlayerSettings.defaultInterfaceOrientation,
                setValueFunc = v => PlayerSettings.defaultInterfaceOrientation = v,
                recommendedValue = UIOrientation.LandscapeLeft,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Multithreaded Rendering",
                skipCheckFunc = () => !VIUSettingsEditor.supportWaveVR,
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
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Graphic Jobs",
                skipCheckFunc = () => !VIUSettingsEditor.supportWaveVR,
                currentValueFunc = () => PlayerSettings.graphicsJobs,
                setValueFunc = v => PlayerSettings.graphicsJobs = v,
                recommendedValue = true,
            });
#endif
        }
    }

    public static partial class VIUSettingsEditor
    {
        public const string URL_WAVE_VR_PLUGIN = "https://developer.vive.com/resources/knowledgebase/wave-sdk/";
        public const string URL_WAVE_VR_6DOF_SUMULATOR_USAGE_PAGE = "https://github.com/ViveSoftware/ViveInputUtility-Unity/wiki/Wave-VR-6-DoF-Controller-Simulator";
        private const string WAVE_XR_PACKAGE_NAME = "com.htc.upm.wave.xrsdk";
        private const string WAVE_XR_PACKAGE_NATIVE_NAME = "com.htc.upm.wave.native";
        private const string WAVE_XR_PACKAGE_ESSENCE_NAME = "com.htc.upm.wave.essence";

        public static bool canSupportWaveVR
        {
            get { return WaveVRSettings.instance.canSupport; }
        }

        public static bool supportWaveVR
        {
            get { return WaveVRSettings.instance.support; }
            set { WaveVRSettings.instance.support = value; }
        }

        private class WaveVRSettings : VRPlatformSetting
#if UNITY_2018_1_OR_NEWER
        , IPreprocessBuildWithReport
#elif UNITY_5_6_OR_NEWER
        , IPreprocessBuild
#endif
        {
            private Foldouter m_foldouter = new Foldouter();

            public static WaveVRSettings instance { get; private set; }

            public WaveVRSettings() { instance = this; }

            public override int order { get { return 102; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

            private string defaultAndroidManifestPath
            {
                get
                {
#if VIU_WAVEVR
                    var monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
                    var monoScript = monoScripts.FirstOrDefault(script => script.GetClass() == typeof(WaveVR));
                    var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(monoScript));
                    var fullPath = Path.GetFullPath((path.Substring(0, path.Length - "Scripts".Length) + "Platform/Android/AndroidManifest.xml").Replace("\\", "/"));

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
                    return activeBuildTargetGroup == BuildTargetGroup.Android &&
                           (VRModule.isWaveVRPluginDetected || PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME));
#elif UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1 && !UNITY_5_6_2
                    return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isWaveVRPluginDetected;
#else
                    return false;
#endif
                }
            }

            public override bool support
            {
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1 && !UNITY_5_6_2
                get
                {
                    if (!canSupport) { return false; }
                    if (!VIUSettings.activateWaveVRModule) { return false; }

#if VIU_XR_GENERAL_SETTINGS
                    if (!(MockHMDSDK.enabled || XRPluginManagementUtils.IsXRLoaderEnabled(UnityXRModule.WAVE_XR_LOADER_NAME, requirdPlatform)))
                    {
                        return false;
                    }
#endif
#if VIU_WAVEVR_3_0_0_OR_NEWER
                    if (!virtualRealitySupported) { return false; }
#else
                    if (virtualRealitySupported) { return false; }
#endif

                    if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23) { return false; }
                    if (PlayerSettings.colorSpace == ColorSpace.Linear && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                    return true;
                }
                set
                {
                    if (support == value) { return; }

                    if (value)
                    {
                        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23)
                        {
                            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
                        }

                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            SetGraphicsAPI(BuildTarget.Android, GraphicsDeviceType.OpenGLES3);
                        }

                        supportDaydream = false;
                        supportOculusGo = false;
                    }

#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
                    XRPluginManagementUtils.SetXRLoaderEnabled(UnityXRModule.WAVE_XR_LOADER_CLASS_NAME, requirdPlatform, value);
                    MockHMDSDK.enabled = value && !PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME);
                    VIUSettings.activateUnityXRModule = XRPluginManagementUtils.IsAnyXRLoaderEnabled(requirdPlatform);
#elif VIU_WAVEVR_3_0_0_OR_NEWER
                    MockHMDSDK.enabled = value;
#else
                    virtualRealitySupported = false;
#endif
                    VIUSettings.activateWaveVRModule = value;
                }
#else
                get { return false; }
                set { }
#endif
            }

            public int callbackOrder { get { return 10; } }

            public override void OnPreferenceGUI()
            {
                const string title = "Wave XR";
                const float wvrToggleWidth = 226f;
                if (canSupport)
                {
                    var wasSupported = support;
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title, "VIVE Focus, VIVE Flow"), wasSupported);
                    s_symbolChanged |= wasSupported != support;
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();

#if !UNITY_5_6_OR_NEWER || UNITY_5_6_0 || UNITY_5_6_1 || UNITY_5_6_2
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(title, "Unity 5.6.3 or later version required."), false, GUILayout.Width(wvrToggleWidth));
                    GUI.enabled = true;
#else
                    if (activeBuildTargetGroup != BuildTargetGroup.Android)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Android platform required."), false, GUILayout.Width(wvrToggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(BuildTargetGroup.Android, BuildTarget.Android);
                    }
#if UNITY_2019_4_OR_NEWER
                    else if (!PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Wave XR Plugin package required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Add Wave XR Plugin Package", "Add " + WAVE_XR_PACKAGE_NAME + " to Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            if (!ManifestUtils.CheckRegistryExists(RegistryToolSettings.Instance().Registry))
                            {
                                ManifestUtils.AddRegistry(RegistryToolSettings.Instance().Registry);
                            }

                            PackageManagerHelper.AddToPackageList(WAVE_XR_PACKAGE_NAME);
                            VIUProjectSettings.Instance.isInstallingWaveXRPlugin = true;
                        }
                    }
#endif
                    else if (!VRModule.isWaveVRPluginDetected)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, "Wave VR plugin required."), false, GUILayout.Width(wvrToggleWidth));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowUrlLinkButton(URL_WAVE_VR_PLUGIN);
                    }
#endif

                    GUILayout.EndHorizontal();
                }

                if (support && m_foldouter.isExpended)
                {
                    if (support) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel += 2;

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUIUtility.labelWidth = 230;
                            var style = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleLeft };
                            VIUSettings.waveVRAndroidManifestPath = EditorGUILayout.DelayedTextField(new GUIContent("Customized AndroidManifest Path:", "Default path: " + defaultAndroidManifestPath),
                                                    VIUSettings.waveVRAndroidManifestPath, style);

                            s_guiChanged |= EditorGUI.EndChangeCheck();
                            if (GUILayout.Button("Open", new GUILayoutOption[] { GUILayout.Width(44), GUILayout.Height(18) }))
                            {
                                VIUSettings.waveVRAndroidManifestPath = EditorUtility.OpenFilePanel("Select AndroidManifest.xml", string.Empty, "xml");
                            }
                            EditorGUI.BeginChangeCheck();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        {
                            if (!string.IsNullOrEmpty(VIUSettings.waveVRAndroidManifestPath) && !File.Exists(VIUSettings.waveVRAndroidManifestPath))
                            {
                                EditorGUILayout.HelpBox("File does not existed!", MessageType.Warning);
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        const string enableWaveXRRenderModelTitle = "Enable Wave XR Render Model";
                        EditorGUILayout.BeginHorizontal();
#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL || VIU_WAVEXR_ESSENCE_RENDERMODEL
                        VIUSettings.enableWaveXRRenderModel = EditorGUILayout.ToggleLeft(new GUIContent(enableWaveXRRenderModelTitle, VIUSettings.ENABLE_WAVE_XR_RENDER_MODEL_TOOLTIP), VIUSettings.enableWaveXRRenderModel);
#elif UNITY_2018_1_OR_NEWER
                        GUI.enabled = false;
                        EditorGUILayout.ToggleLeft(new GUIContent(enableWaveXRRenderModelTitle, VIUSettings.ENABLE_WAVE_XR_RENDER_MODEL_TOOLTIP + ". Required Wave XR Plugin Essence"), false, GUILayout.ExpandWidth(true));
                        GUI.enabled = true;
                        
                        s_guiChanged |= EditorGUI.EndChangeCheck();
                        if (GUILayout.Button(new GUIContent("Add Wave XR Plugin Essence", "Add " + WAVE_XR_PACKAGE_NAME + " to Package Manager"), GUILayout.ExpandWidth(false)))
                        {
                            if (!ManifestUtils.CheckRegistryExists(RegistryToolSettings.Instance().Registry))
                            {
                                ManifestUtils.AddRegistry(RegistryToolSettings.Instance().Registry);
                            }

                            if (!PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_ESSENCE_NAME))
                            {
                                PackageManagerHelper.AddToPackageList(WAVE_XR_PACKAGE_ESSENCE_NAME);
                            }

                            VIUProjectSettings.Instance.isInstallingWaveXRPlugin = true;
                        }
                        EditorGUI.BeginChangeCheck();
#else
                        GUI.enabled = false;
                        EditorGUILayout.ToggleLeft(new GUIContent(enableWaveXRRenderModelTitle, "Unity 2018.1 or later version required."), false, GUILayout.ExpandWidth(true));
                        GUI.enabled = true;
#endif
                        EditorGUILayout.EndHorizontal();

                        const string enableWaveHandTrackingTitle = "Enable Wave Hand Tracking";
                        EditorGUILayout.BeginHorizontal();
#if VIU_WAVEVR_HAND_TRACKING_CHECK
                        {
                            var supported = Wave.XR.BuildCheck.CheckIfHandTrackingEnabled.ValidateEnabled() && VRModuleSettings.activateWaveHandTrackingSubmodule;
                            var shouldSupport = EditorGUILayout.ToggleLeft(new GUIContent(enableWaveHandTrackingTitle), supported);
                            if (supported != shouldSupport)
                            {
                                Wave.XR.BuildCheck.CheckIfHandTrackingEnabled.PerformAction(shouldSupport);
                                VRModuleSettings.activateWaveHandTrackingSubmodule = shouldSupport;
                                // can manually disable gesture data fetching by setting enableWaveHandGesture to false if needed
                                // so far the hardware/runtime will start gesture detaction whenever hand tracking detaction is started,
                                // disabling enableWaveHandGesture only save some data fetching time, it doesn't stop hardware/runtime gesture detaction
                                if (shouldSupport) { VRModuleSettings.enableWaveHandGesture = true; }
                            }
                        }
#elif UNITY_2018_1_OR_NEWER
                        GUI.enabled = false;
                        EditorGUILayout.ToggleLeft(new GUIContent(enableWaveHandTrackingTitle, "Wave XR Plugin Essence required."), false, GUILayout.ExpandWidth(true));
                        GUI.enabled = true;

                        s_guiChanged |= EditorGUI.EndChangeCheck();
                        if (GUILayout.Button(new GUIContent("Update Wave XR Plugin", "Update " + WAVE_XR_PACKAGE_NAME + " to lateast version"), GUILayout.ExpandWidth(false)))
                        {
                            if (!ManifestUtils.CheckRegistryExists(RegistryToolSettings.Instance().Registry))
                            {
                                ManifestUtils.AddRegistry(RegistryToolSettings.Instance().Registry);
                            }

                            if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_ESSENCE_NAME))
                            {
                                PackageManagerHelper.AddToPackageList(WAVE_XR_PACKAGE_ESSENCE_NAME);
                            }
                            else if (PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NATIVE_NAME))
                            {
                                PackageManagerHelper.AddToPackageList(WAVE_XR_PACKAGE_NATIVE_NAME);
                            }
                            else
                            {
                                PackageManagerHelper.AddToPackageList(WAVE_XR_PACKAGE_NAME);
                            }

                            VIUProjectSettings.Instance.isInstallingWaveXRPlugin = true;
                        }
                        EditorGUI.BeginChangeCheck();
#else
                        GUI.enabled = false;
                        EditorGUILayout.ToggleLeft(new GUIContent(enableWaveHandTrackingTitle, "Unity 2018.1 or later version required."), false, GUILayout.ExpandWidth(true));
                        GUI.enabled = true;
#endif
                        EditorGUILayout.EndHorizontal();

                        VIUSettings.waveVRAddVirtualArmTo3DoFController = EditorGUILayout.ToggleLeft(new GUIContent("Add Virtual Arm for 3 Dof Controller"), VIUSettings.waveVRAddVirtualArmTo3DoFController);
                        if (!VIUSettings.waveVRAddVirtualArmTo3DoFController) { GUI.enabled = false; }
                        {
                            EditorGUI.indentLevel++;

                            VIUSettings.waveVRVirtualNeckPosition = EditorGUILayout.Vector3Field("Neck", VIUSettings.waveVRVirtualNeckPosition);
                            VIUSettings.waveVRVirtualElbowRestPosition = EditorGUILayout.Vector3Field("Elbow", VIUSettings.waveVRVirtualElbowRestPosition);
                            VIUSettings.waveVRVirtualArmExtensionOffset = EditorGUILayout.Vector3Field("Arm", VIUSettings.waveVRVirtualArmExtensionOffset);
                            VIUSettings.waveVRVirtualWristRestPosition = EditorGUILayout.Vector3Field("Wrist", VIUSettings.waveVRVirtualWristRestPosition);
                            VIUSettings.waveVRVirtualHandRestPosition = EditorGUILayout.Vector3Field("Hand", VIUSettings.waveVRVirtualHandRestPosition);

                            EditorGUI.indentLevel--;
                        }
                        if (!VIUSettings.waveVRAddVirtualArmTo3DoFController) { GUI.enabled = true; }

                        EditorGUILayout.BeginHorizontal();
                        VIUSettings.simulateWaveVR6DofController = EditorGUILayout.ToggleLeft(new GUIContent("Enable 6 Dof Simulator (Experimental)", "Connect HMD with Type-C keyboard to perform simulation"), VIUSettings.simulateWaveVR6DofController);
                        s_guiChanged |= EditorGUI.EndChangeCheck();
                        ShowUrlLinkButton(URL_WAVE_VR_6DOF_SUMULATOR_USAGE_PAGE, "Usage");
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.EndHorizontal();

                        if (!VIUSettings.enableSimulatorKeyboardMouseControl && supportSimulator) { GUI.enabled = true; }

                        EditorGUI.indentLevel -= 2;
                    }
                    if (support) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
                }

                if (support)
                {
                    EditorGUI.indentLevel += 2;

#if VIU_WAVEVR_2_1_0_OR_NEWER
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("WaveVR Simulator will be activated in Editor Mode.", MessageType.Info);

                    s_warningHeight = Mathf.Max(s_warningHeight, GUILayoutUtility.GetLastRect().height);
                    GUILayout.BeginVertical(GUILayout.Height(s_warningHeight));
                    GUILayout.FlexibleSpace();
                    ShowUrlLinkButton("https://hub.vive.com/storage/app/doc/en-us/Simulator.html", "Usage");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
#else
                    EditorGUILayout.HelpBox("WaveVR device not supported in Editor Mode. Please run on target device.", MessageType.Info);
#endif

                    EditorGUI.indentLevel -= 2;
                }

#if UNITY_2019_4_OR_NEWER
                if (VIUProjectSettings.Instance.isInstallingWaveXRPlugin)
                {
                    bool isPackageInstalled = PackageManagerHelper.IsPackageInList(WAVE_XR_PACKAGE_NAME);
                    bool isLoaderEnabled = XRPluginManagementUtils.IsXRLoaderEnabled(UnityXRModule.WAVE_XR_LOADER_NAME, BuildTargetGroup.Android);
                    if (isPackageInstalled && !isLoaderEnabled)
                    {
                        XRPluginManagementUtils.SetXRLoaderEnabled(UnityXRModule.WAVE_XR_LOADER_CLASS_NAME, BuildTargetGroup.Android, true);
                        VIUProjectSettings.Instance.isInstallingWaveXRPlugin = false;
                    }
                }
#endif
            }

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                if (!support) { return; }

                if (File.Exists(VIUSettings.waveVRAndroidManifestPath))
                {
                    File.Copy(VIUSettings.waveVRAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
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

                if (File.Exists(VIUSettings.waveVRAndroidManifestPath))
                {
                    File.Copy(VIUSettings.waveVRAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                }
                else if (File.Exists(defaultAndroidManifestPath))
                {
                    File.Copy(defaultAndroidManifestPath, "Assets/Plugins/Android/AndroidManifest.xml", true);
                }
            }
#endif
        }
    }
}