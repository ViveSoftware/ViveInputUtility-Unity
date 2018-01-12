//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public static class VIUSettingsEditor
    {
        public static class EnabledDevices
        {
            public class Device
            {
                public readonly string m_name;
                private readonly bool m_addLast;
                private bool m_enabled;
                public bool enabled
                {
                    get
                    {
                        Update();
                        return m_enabled;
                    }
                    set
                    {
                        Update();
                        if (m_enabled == value) { return; }
                        s_listDirty = true;
                        m_enabled = value;
                        if (value)
                        {
                            if (m_addLast) { s_deviceNames.Add(m_name); }
                            else { s_deviceNames.Insert(0, m_name); }
                        }
                        else
                        {
                            s_deviceNames.Remove(m_name);
                        }
                    }
                }
                public Device(string name, bool addLast = false) { m_name = name; m_addLast = addLast; }
                public void Reset() { m_enabled = false; }
                public bool CheckSupport(string deviceName) { return !m_enabled && (m_enabled = m_name == deviceName); } // return true if confirmed
            }

            private static int s_updatedFrame = -1;
            private static bool s_listDirty;
            private static List<string> s_deviceNames;
            private static List<Device> s_devices;

            public static readonly Device Oculus = new Device("Oculus");
            public static readonly Device OpenVR = new Device("OpenVR", true);
            public static readonly Device Daydream = new Device("daydream");

            public static int deviceCount { get { return s_deviceNames == null ? 0 : s_deviceNames.Count; } }

            private static void Update()
            {
                if (!ChangeProp.Set(ref s_updatedFrame, Time.frameCount)) { return; }
                UpdateDeviceList();

                // Register device for name check here
                if (s_devices == null)
                {
                    s_devices = new List<Device>();
                    s_devices.Add(Oculus);
                    s_devices.Add(OpenVR);
                    s_devices.Add(Daydream);
                }

                s_devices.ForEach(device => device.Reset());

                foreach (var name in s_deviceNames)
                {
                    foreach (var device in s_devices)
                    {
                        if (device.CheckSupport(name)) { break; }
                    }
                }
            }

            private static void UpdateDeviceList()
            {
                if (s_deviceNames == null) { s_deviceNames = new List<string>(); }
                s_deviceNames.Clear();
                if (!PlayerSettings.virtualRealitySupported) { return; }
#if UNITY_5_4
                s_deviceNames.AddRange(UnityEditorInternal.VR.VREditor.GetVREnabledDevices(EditorUserBuildSettings.selectedBuildTargetGroup));
#elif UNITY_5_5_OR_NEWER
                s_deviceNames.AddRange(UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#endif
            }

            public static void ApplyChanges()
            {
                if (!s_listDirty) { return; }
                s_listDirty = false;
                if (s_deviceNames.Count > 0 && !PlayerSettings.virtualRealitySupported)
                {
                    PlayerSettings.virtualRealitySupported = true;
                }
                else if (s_deviceNames.Count == 0 && PlayerSettings.virtualRealitySupported)
                {
                    PlayerSettings.virtualRealitySupported = false;
                }
#if UNITY_5_4
                UnityEditorInternal.VR.VREditor.SetVREnabledDevices(EditorUserBuildSettings.selectedBuildTargetGroup, s_deviceNames.ToArray());
#elif UNITY_5_5_OR_NEWER
                UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), s_deviceNames.ToArray());
#endif
            }
        }

        private const float s_buttonIndent = 20f;
        private static GUIStyle s_boldStyle;
        private static string s_defaultAssetPath;

        public static string defaultAssetPath
        {
            get
            {
                if (s_defaultAssetPath == null)
                {
                    var ms = MonoScript.FromScriptableObject(VIUSettings.Instance);
                    var path = AssetDatabase.GetAssetPath(ms);
                    path = System.IO.Path.GetDirectoryName(path);
                    s_defaultAssetPath = path.Substring(0, path.Length - "Scripts".Length) + "Resources/" + VIUSettings.DEFAULT_RESOURCE_PATH + ".asset";
                }

                return s_defaultAssetPath;
            }
        }

        public static bool supportAnyStandaloneVR { get { return supportOpenVR || supportOculus; } }

        public static bool supportAnyAndroidVR { get { return supportDaydream; } }

        public static bool supportAnyVR { get { return supportAnyStandaloneVR || supportAnyAndroidVR; } }

        public static bool canSupportSimulatedDevice
        {
            get
            {
                return true;
            }
        }

        public static bool supportSimulatedDevice
        {
            get
            {
                return canSupportSimulatedDevice && VIUSettings.activateSimulatorModule;
            }
            private set
            {
                VIUSettings.activateSimulatorModule = value;
            }
        }

        public static bool canSupportOpenVR
        {
            get
            {
                return
                    BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == BuildTargetGroup.Standalone
#if UNITY_5_3 || UNITY_5_4
                    && VRModule.isSteamVRPluginDetected
#endif
                    ;
            }
        }

        public static bool supportOpenVR
        {
            get
            {
#if UNITY_5_3 || UNITY_5_4
                return canSupportOpenVR && VIUSettings.activateSteamVRModule;
#elif UNITY_5_5_OR_NEWER
                return canSupportOpenVR && (VIUSettings.activateSteamVRModule || VIUSettings.activateUnityNativeVRModule) && EnabledDevices.OpenVR.enabled;
#endif
            }
            set
            {
#if UNITY_5_5_OR_NEWER
                EnabledDevices.OpenVR.enabled = value;
#endif
                VIUSettings.activateSteamVRModule = value;
                VIUSettings.activateUnityNativeVRModule = supportOpenVR || supportOculus;
            }
        }

        public static bool canSupportOculus
        {
            get
            {
                return
                    BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == BuildTargetGroup.Standalone
#if UNITY_5_3 || UNITY_5_4
                    && VRModule.isOculusVRPluginDetected
#endif
                    ;
            }
        }

        public static bool supportOculus
        {
            get
            {
#if UNITY_5_3
                return canSupportOculus && VIUSettings.activateOculusVRModule && PlayerSettings.virtualRealitySupported;
#elif UNITY_5_4
                return canSupportOculus && VIUSettings.activateOculusVRModule && EnabledDevices.Oculus.enabled;
#elif UNITY_5_5_OR_NEWER
                return canSupportOculus && (VIUSettings.activateOculusVRModule || VIUSettings.activateUnityNativeVRModule) && EnabledDevices.Oculus.enabled;
#endif
            }
            set
            {
#if UNITY_5_3
                PlayerSettings.virtualRealitySupported = value;
#else
                EnabledDevices.Oculus.enabled = value;
#endif
                VIUSettings.activateOculusVRModule = value;
                VIUSettings.activateUnityNativeVRModule = supportOpenVR || supportOculus;
            }
        }

#if UNITY_5_6_OR_NEWER
        public static bool canSupportDaydream
        {
            get
            {
                return BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == BuildTargetGroup.Android && VRModule.isGoogleVRPluginDetected;
            }
        }

        public static bool supportDaydream
        {
            get
            {
                return canSupportDaydream && VIUSettings.activateGoogleVRModule && EnabledDevices.Daydream.enabled && PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel24;
            }
            set
            {
                EnabledDevices.Daydream.enabled = value;
                VIUSettings.activateGoogleVRModule = value;
                if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
                {
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                }
            }
        }
#else
        public static bool canSupportDaydream { get { return false; } }

        public static bool supportDaydream { get { return false; } set { } }
#endif

        [PreferenceItem("VIU Settings")]
        private static void OnVIUPreferenceGUI()
        {
            if (s_boldStyle == null)
            {
                s_boldStyle = new GUIStyle();
                s_boldStyle.fontStyle = FontStyle.Bold;
            }

            EditorGUILayout.SelectableLabel("Version: v" + VIUVersion.current);

            EditorGUI.BeginChangeCheck();

            VIUSettings.enableBindingInterfaceSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable Binding Interface Switch", VIUSettings.BIND_UI_SWITCH_TOOLTIP), VIUSettings.enableBindingInterfaceSwitch);
            VIUSettings.enableExternalCameraSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable External Camera Switch", VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP), VIUSettings.enableExternalCameraSwitch);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Supported devices", s_boldStyle);
            GUILayout.Space(5);

            const string supportSimulatorTitle = "Simulated Device";
            if (canSupportSimulatedDevice)
            {
                if (supportSimulatedDevice = EditorGUILayout.ToggleLeft(new GUIContent(supportSimulatorTitle, "If checked, the simulator will activated automatically if no other valid VR devices found."), supportSimulatedDevice))
                {
                    EditorGUI.indentLevel++;
                    VIUSettings.simulatorAutoTrackMainCamera = EditorGUILayout.ToggleLeft(new GUIContent("Auto Main Camera Tracking"), VIUSettings.simulatorAutoTrackMainCamera);
                    if (VIUSettings.enableSimulatorKeyboardMouseControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Keyboard-Mouse Control", "You can also handle VRModule.Simulator.onUpdateDeviceState by your self."), VIUSettings.enableSimulatorKeyboardMouseControl))
                    {
                        EditorGUI.indentLevel++;
                        VIUSettings.simulateTrackpadTouch = EditorGUILayout.Toggle(new GUIContent("Simulate Trackpad Touch", VIUSettings.SIMULATE_TRACKPAD_TOUCH_TOOLTIP), VIUSettings.simulateTrackpadTouch);
                        VIUSettings.simulatorKeyMoveSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Move Speed", VIUSettings.SIMULATOR_KEY_MOVE_SPEED_TOOLTIP), VIUSettings.simulatorKeyMoveSpeed);
                        VIUSettings.simulatorKeyRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Rotate Speed", VIUSettings.SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP), VIUSettings.simulatorKeyRotateSpeed);
                        VIUSettings.simulatorMouseRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Mouse Rotate Speed"), VIUSettings.simulatorMouseRotateSpeed);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent(supportSimulatorTitle), false);
                GUI.enabled = true;
            }

            GUILayout.Space(5);

            const string supportOpenVRTitle = "Vive (OpenVR compatible device)";
            if (canSupportOpenVR)
            {
                if (supportOpenVR = EditorGUILayout.ToggleLeft(new GUIContent(supportOpenVRTitle), supportOpenVR))
                {
                    if (!VRModule.isSteamVRPluginDetected)
                    {
                        var noViveTrackerSupportWanning =
#if UNITY_5_5 || UNITY_5_6
                        "Vive Tracker not supported! Install SteamVR Plugin to get support.";
#elif UNITY_2017_1_OR_NEWER
                        "Vive Tracker's input not supported! Install SteamVR Plugin to get support.";
#else 
                        string.Empty;
#endif
                        EditorGUI.indentLevel++;
                        EditorGUILayout.HelpBox(noViveTrackerSupportWanning, MessageType.Warning);
                        EditorGUI.indentLevel--;

                        ShowGetSteamVRPluginButton();
                    }
                }
            }
            else
            {
                if (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildTargetGroup.Standalone)
                {
                    GUI.enabled = false;
                    EditorGUILayout.ToggleLeft(new GUIContent(supportOpenVRTitle), false);
                    GUI.enabled = true;
                    ShowSwitchPlatformButton(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
                else if (!VRModule.isSteamVRPluginDetected)
                {
                    GUI.enabled = false;
                    EditorGUILayout.ToggleLeft(new GUIContent(supportOpenVRTitle, "SteamVR Plugin required."), false);
                    GUI.enabled = true;
                    ShowGetSteamVRPluginButton();
                }
            }

            GUILayout.Space(5f);

            const string supportOculusVRTitle = "Oculus Rift & Touch";
            if (canSupportOculus)
            {
                supportOculus = EditorGUILayout.ToggleLeft(new GUIContent(supportOculusVRTitle), supportOculus);
            }
            else
            {
                if (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildTargetGroup.Standalone)
                {
                    GUI.enabled = false;
                    EditorGUILayout.ToggleLeft(new GUIContent(supportOculusVRTitle), false);
                    GUI.enabled = true;
                    ShowSwitchPlatformButton(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
                else if (!VRModule.isOculusVRPluginDetected)
                {
                    GUI.enabled = false;
                    EditorGUILayout.ToggleLeft(new GUIContent(supportOculusVRTitle, "Oculus VR plugin required."), false);
                    GUI.enabled = true;
                    ShowGetOculusVRPluginButton();
                }
            }

            GUILayout.Space(5);

            const string supportDaydreamVRTitle = "Daydream";
            if (canSupportDaydream)
            {
                if (supportDaydream = EditorGUILayout.ToggleLeft(new GUIContent(supportDaydreamVRTitle), supportDaydream))
                {
                    EditorGUI.indentLevel++;

                    // following preferences is stored at HKEY_CURRENT_USER\Software\Unity Technologies\Unity Editor 5.x\
                    //EditorPrefs.GetString("AndroidNdkRoot");
                    if (string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot")))
                    {
                        EditorGUILayout.HelpBox("AndroidSdkRoot is empty. Setup at Edit -> Preferences... -> External Tools -> Android SDK", MessageType.Warning);
                    }

                    if (string.IsNullOrEmpty(EditorPrefs.GetString("JdkPath")))
                    {
                        EditorGUILayout.HelpBox("JdkPath is empty. Setup at Edit -> Preferences... -> External Tools -> Android JDK", MessageType.Warning);
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent(supportDaydreamVRTitle, (VRModule.isGoogleVRPluginDetected ? "Google VR plugin required." : "")), false);
                GUI.enabled = true;

#if UNITY_5_6_OR_NEWER
                if (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildTargetGroup.Android)
                {
                    ShowSwitchPlatformButton(BuildTargetGroup.Android, BuildTarget.Android);
                }

                if (!VRModule.isGoogleVRPluginDetected)
                {
                    ShowGetGoogleVRPluginButton();
                }
#else
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Unity 5.6 or newer version required.", MessageType.Warning);
                EditorGUI.indentLevel--;
#endif
            }

            if (EditorGUI.EndChangeCheck())
            {
                EnabledDevices.ApplyChanges();

                var path = AssetDatabase.GetAssetPath(VIUSettings.Instance);
                if (string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(VIUSettings.Instance, defaultAssetPath);
                }
            }

            //GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            //if (GUILayout.Button("Reset"))
            //{
            //    var path = AssetDatabase.GetAssetPath(VIUSettings.Instance);
            //    if (!string.IsNullOrEmpty(path))
            //    {
            //        AssetDatabase.DeleteAsset(path);
            //    }
            //}
            //GUILayout.EndHorizontal();
        }

        private static void ShowSwitchPlatformButton(BuildTargetGroup group, BuildTarget target)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(s_buttonIndent);
            if (GUILayout.Button("Switch platform to " + group))
            {
#if UNITY_2017_1_OR_NEWER
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(group, target);
#else
                EditorUserBuildSettings.SwitchActiveBuildTarget(target);
#endif
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void ShowGetSteamVRPluginButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(s_buttonIndent);
            if (GUILayout.Button("Get SteamVR Plugin"))
            {
                Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/content/32647");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void ShowGetOculusVRPluginButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(s_buttonIndent);
            if (GUILayout.Button("Get Oculus VR Plugin"))
            {
                Application.OpenURL("https://developer.oculus.com/downloads/package/oculus-utilities-for-unity-5/");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void ShowGetGoogleVRPluginButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(s_buttonIndent);
            if (GUILayout.Button("Get Google VR Plugin"))
            {
                Application.OpenURL("https://developers.google.com/vr/develop/unity/download");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}