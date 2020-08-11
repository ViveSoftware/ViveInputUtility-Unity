//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
#if UNITY_5_6_OR_NEWER
using UnityEditor.Build;
#endif
#if UNITY_2018_1_OR_NEWER
using HTC.UnityPlugin.UPMRegistryTool;
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

            public int callbackOrder { get { return 0; } }

            public override void OnPreferenceGUI()
            {
                const string title = "WaveVR";
                if (canSupport)
                {
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title, "VIVE Focus, VIVE Focus Plus"), support);
                }
                else
                {
                    const float wvrToggleWidth = 226f;
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1 && !UNITY_5_6_2
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

                        bool hasHTCRegistryAdded = RegistryToolSettings.IsRegistryExists(RegistryToolSettings.Instance.ViveRegistry);
                        if (hasHTCRegistryAdded)
                        {
                            ShowAddPackageButton("Wave XR Plugin", WAVE_XR_PACKAGE_NAME);
                        }
                        else
                        {
                            if (GUILayout.Button(new GUIContent("Add VIVE Registry")))
                            {
                                bool result = EditorUtility.DisplayDialog(
                                    "Add VIVE Registry",
                                    "Do you want to add VIVE registry to your project?\n\nBy adding the VIVE registry (" + RegistryToolSettings.Instance.ViveRegistry.url + ") in your 'Packages/manifest.json', VIU can install Wave XR Plugin packages for you.\n\nIn addition, you can discover, install, update or remove the packages from VIVE in the package manager window later.",
                                    "Add",
                                    "Cancel");

                                if (result)
                                {
                                    RegistryToolSettings.AddRegistry(RegistryToolSettings.Instance.ViveRegistry);
                                }
                            }
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
#else
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(title, "Unity 5.6.3 or later version required."), false, GUILayout.Width(wvrToggleWidth));
                    GUI.enabled = true;
#endif
                    GUILayout.EndHorizontal();
                }

                if (support && m_foldouter.isExpended)
                {
                    if (support) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel += 2;
                        EditorGUILayout.BeginHorizontal();

                        EditorGUIUtility.labelWidth = 230;
                        var style = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleLeft };
                        VIUSettings.waveVRAndroidManifestPath = EditorGUILayout.DelayedTextField(new GUIContent("Customized AndroidManifest Path:", "Default path: " + defaultAndroidManifestPath),
                                                VIUSettings.waveVRAndroidManifestPath, style);
                        if (GUILayout.Button("Open", new GUILayoutOption[] { GUILayout.Width(44), GUILayout.Height(18) }))
                        {
                            VIUSettings.waveVRAndroidManifestPath = EditorUtility.OpenFilePanel("Select AndroidManifest.xml", string.Empty, "xml");
                        }

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();

                        if (!File.Exists(VIUSettings.waveVRAndroidManifestPath) && (string.IsNullOrEmpty(defaultAndroidManifestPath) || !File.Exists(defaultAndroidManifestPath)))
                        {
                            EditorGUILayout.HelpBox("Default AndroidManifest.xml does not existed!", MessageType.Warning);
                        }
                        else if (!string.IsNullOrEmpty(VIUSettings.waveVRAndroidManifestPath) && !File.Exists(VIUSettings.waveVRAndroidManifestPath))
                        {
                            EditorGUILayout.HelpBox("File does not existed!", MessageType.Warning);
                        }

                        EditorGUI.BeginChangeCheck();
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