//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTC.UnityPlugin.Vive
{
    public static partial class VIUSettingsEditor
    {
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
        {
            private Foldouter m_foldouter = new Foldouter();

            public static WaveVRSettings instance { get; private set; }

            public WaveVRSettings() { instance = this; }

            public override int order { get { return 102; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Android; } }

            public override bool canSupport
            {
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1 && !UNITY_5_6_2
                get { return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isWaveVRPluginDetected; }
#else
                get { return false; }
#endif
            }

            public override bool support
            {
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1 && !UNITY_5_6_2
                get
                {
                    if (!canSupport) { return false; }
                    if (!VIUSettings.activateWaveVRModule) { return false; }
                    if (virtualRealitySupported) { return false; }
                    if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23) { return false; }
                    if (PlayerSettings.colorSpace == ColorSpace.Linear && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                    return true;
                }
                set
                {
                    if (support == value) { return; }

                    if (value)
                    {
                        virtualRealitySupported = false;

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

                    VIUSettings.activateWaveVRModule = value;
                }
#else
                get { return false; }
                set { }
#endif
            }

            public override void OnPreferenceGUI()
            {

                const string title = "VIVE Focus <size=9>(WaveVR compatible device)</size>";
                if (canSupport)
                {
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title), support);
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
        }
    }
}