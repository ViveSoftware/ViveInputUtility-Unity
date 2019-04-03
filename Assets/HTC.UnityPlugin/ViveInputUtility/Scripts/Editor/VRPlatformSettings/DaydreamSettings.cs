//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTC.UnityPlugin.Vive
{
    public class DaydreamRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
        public DaydreamRecommendedSettings()
        {
            Add(new VIUVersionCheck.RecommendedSetting<bool>()
            {
                settingTitle = "Virtual Reality Supported with Daydream",
                skipCheckFunc = () => !VIUSettingsEditor.canSupportDaydream,
                currentValueFunc = () => VIUSettingsEditor.supportDaydream,
                setValueFunc = v => VIUSettingsEditor.supportDaydream = v,
                recommendedValue = true,
            });
        }
    }

    public static partial class VIUSettingsEditor
    {
        public static bool canSupportDaydream
        {
            get { return DaydreamSettings.instance.canSupport; }
        }

        public static bool supportDaydream
        {
            get { return DaydreamSettings.instance.support; }
            set { DaydreamSettings.instance.support = value; }
        }

        private class DaydreamSettings : VRPlatformSetting
        {
            private Foldouter m_foldouter = new Foldouter();

            public static DaydreamSettings instance { get; private set; }

            public DaydreamSettings() { instance = this; }

            public override int order { get { return 101; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

            public override bool canSupport
            {
#if UNITY_5_6_OR_NEWER
                get { return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isGoogleVRPluginDetected; }
#else
                get { return false; }
#endif
            }

            public override bool support
            {
#if UNITY_5_6_OR_NEWER
                get
                {
                    if (!canSupport) { return false; }
                    if (!VIUSettings.activateGoogleVRModule) { return false; }
                    if (!DaydreamSDK.enabled) { return false; }
                    if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24) { return false; }
                    if (PlayerSettings.colorSpace == ColorSpace.Linear && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                    return true;
                }
                set
                {
                    if (support == value) { return; }

                    if (value)
                    {
                        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
                        {
                            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                        }

                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            SetGraphicsAPI(BuildTarget.Android, GraphicsDeviceType.OpenGLES3);
                        }

                        supportWaveVR = false;
                        supportOculusGo = false;
                    }

                    DaydreamSDK.enabled = value;
                    VIUSettings.activateGoogleVRModule = value;
                }
#else
                get { return false; }
                set { }
#endif
            }

            public override void OnPreferenceGUI()
            {
                const string title = "Daydream";
                if (canSupport)
                {
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title), support);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();

                    var tooltip = string.Empty;
#if UNITY_5_6_OR_NEWER
                    if (activeBuildTargetGroup != BuildTargetGroup.Android)
                    {
                        tooltip = "Android platform required.";
                    }
                    else if (!VRModule.isGoogleVRPluginDetected)
                    {
                        tooltip = "Google VR plugin required.";
                    }
#else
                    tooltip = "Unity 5.6 or later version required.";
#endif
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(title, tooltip), false, GUILayout.Width(80f));
                    GUI.enabled = true;
#if UNITY_5_6_OR_NEWER
                    if (activeBuildTargetGroup != BuildTargetGroup.Android)
                    {
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(BuildTargetGroup.Android, BuildTarget.Android);
                    }
                    else if (!VRModule.isGoogleVRPluginDetected)
                    {
                        GUILayout.FlexibleSpace();
                        ShowUrlLinkButton(URL_GOOGLE_VR_PLUGIN);
                    }
#endif
                    GUILayout.EndHorizontal();
                }

                if (support && m_foldouter.isExpended)
                {
                    if (support) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel += 2;

                        VIUSettings.daydreamSyncPadPressToTrigger = EditorGUILayout.ToggleLeft(new GUIContent("Sync Pad Press to Trigger", "Enable this option to handle the trigger button since the Daydream controller lacks one."), VIUSettings.daydreamSyncPadPressToTrigger);

                        EditorGUI.indentLevel -= 2;
                    }
                    if (support) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
                }

                if (support)
                {
                    EditorGUI.indentLevel += 2;

                    EditorGUILayout.HelpBox("VRDevice daydream not supported in Editor Mode. Please run on target device.", MessageType.Info);

                    EditorGUI.indentLevel -= 2;
                }
            }
        }
    }
}