//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTC.UnityPlugin.Vive
{
    public static partial class VIUSettingsEditor
    {
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
#if UNITY_5_6_OR_NEWER
                get { return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isOculusVRPluginDetected; }
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