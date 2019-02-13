//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public static partial class VIUSettingsEditor
    {
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
#if UNITY_5_5_OR_NEWER
                    return activeBuildTargetGroup == BuildTargetGroup.Standalone;
#else
                    return activeBuildTargetGroup == BuildTargetGroup.Standalone && VRModule.isSteamVRPluginDetected;
#endif
                    ;
                }
            }

            public override bool support
            {
                get
                {
#if UNITY_5_5_OR_NEWER
                    return canSupport && (VIUSettings.activateSteamVRModule || VIUSettings.activateUnityNativeVRModule) && OpenVRSDK.enabled;
#elif UNITY_5_4_OR_NEWER
                    return canSupport && VIUSettings.activateSteamVRModule && OpenVRSDK.enabled;
#else
                    return canSupport && VIUSettings.activateSteamVRModule && !virtualRealitySupported;
#endif
                }
                set
                {
                    if (support == value) { return; }

                    VIUSettings.activateSteamVRModule = value;

#if UNITY_5_5_OR_NEWER
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
                }
            }

            public override void OnPreferenceGUI()
            {
                const string title = "VIVE <size=9>(OpenVR compatible device)</size>";
                if (canSupport)
                {
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title), support);
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
                    if (support && VRModule.isSteamVRPluginDetected) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel += 2;

                        VIUSettings.autoLoadExternalCameraConfigOnStart = EditorGUILayout.ToggleLeft(new GUIContent("Load Config and Enable External Camera on Start", "You can also load config by calling ExternalCameraHook.LoadConfigFromFile(path) in script."), VIUSettings.autoLoadExternalCameraConfigOnStart);
                        if (!VIUSettings.autoLoadExternalCameraConfigOnStart && support) { GUI.enabled = false; }
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
                            if (VIUSettings.autoLoadExternalCameraConfigOnStart && support && !File.Exists(VIUSettings.externalCameraConfigFilePath))
                            {
                                if (support && VRModule.isSteamVRPluginDetected) { s_guiChanged |= EditorGUI.EndChangeCheck(); }
                                ShowCreateExCamCfgButton();
                                if (support && VRModule.isSteamVRPluginDetected) { EditorGUI.BeginChangeCheck(); }
                            }

                            EditorGUI.indentLevel--;
                        }
                        if (!VIUSettings.autoLoadExternalCameraConfigOnStart && support) { GUI.enabled = true; }

                        VIUSettings.enableExternalCameraSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable External Camera Switch", VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP), VIUSettings.enableExternalCameraSwitch);
                        if (!VIUSettings.enableExternalCameraSwitch && support) { GUI.enabled = false; }
                        {
                            EditorGUI.indentLevel++;

                            VIUSettings.externalCameraSwitchKey = (KeyCode)EditorGUILayout.EnumPopup("Switch Key", VIUSettings.externalCameraSwitchKey);
                            VIUSettings.externalCameraSwitchKeyModifier = (KeyCode)EditorGUILayout.EnumPopup("Switch Key Modifier", VIUSettings.externalCameraSwitchKeyModifier);

                            EditorGUI.indentLevel--;
                        }
                        if (!VIUSettings.enableExternalCameraSwitch && support) { GUI.enabled = true; }

                        EditorGUI.indentLevel -= 2;
                    }
                    if (support && VRModule.isSteamVRPluginDetected) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
                }

                if (support && !VRModule.isSteamVRPluginDetected)
                {
                    EditorGUI.indentLevel += 2;

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("External-Camera(Mix-Reality), animated controller model, VIVE Controller haptics(vibration)" +
#if UNITY_2017_1_OR_NEWER
                        ", VIVE Tracker USB/Pogo-pin input" +
#else
                        ", VIVE Tracker device" +
#endif
                        " NOT supported! Install SteamVR Plugin to get support.", MessageType.Warning);

                    s_warningHeight = Mathf.Max(s_warningHeight, GUILayoutUtility.GetLastRect().height);

                    if (!VRModule.isSteamVRPluginDetected)
                    {
                        GUILayout.BeginVertical(GUILayout.Height(s_warningHeight));
                        GUILayout.FlexibleSpace();
                        ShowUrlLinkButton(URL_STEAM_VR_PLUGIN);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    EditorGUI.indentLevel -= 2;
                }
            }
        }
    }
}