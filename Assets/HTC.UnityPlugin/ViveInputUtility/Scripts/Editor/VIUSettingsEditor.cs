//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

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

        private static class Foldouter
        {
            public enum Index
            {
                Simulator,
                Vive,
                Oculus,
                Daydream,
                WaveVR,
                AutoBinding,
                BindingUISwitch,
            }

            //private static string s_prefKey;
            private static bool s_initialized;
            private static uint s_expendedFlags;
            private static GUIStyle s_styleFoleded;
            private static GUIStyle s_styleExpended;

            private static bool isChanged { get; set; }

            private static uint Flag(Index i) { return 1u << (int)i; }

            public static void Initialize()
            {
                if (s_initialized) { return; }
                s_initialized = true;

                //s_prefKey = "ViveInputUtility.VIUSettingsFolded";

                //if (EditorPrefs.HasKey(s_prefKey))
                //{
                //    s_expendedFlags = (uint)EditorPrefs.GetInt(s_prefKey);
                //}
                s_expendedFlags = 0u;

                s_styleFoleded = new GUIStyle(EditorStyles.foldout);
                s_styleExpended = new GUIStyle(EditorStyles.foldout);
                s_styleExpended.normal = s_styleFoleded.onNormal;
                s_styleExpended.active = s_styleFoleded.onNormal;
            }

            public static void ShowFoldoutBlank()
            {
                GUILayout.Space(20);
            }

            public static void ShowFoldoutButton(Index i, bool visible)
            {
                if (visible)
                {
                    var flag = Flag(i);
                    var style = IsExpended(flag) ? s_styleExpended : s_styleFoleded;
                    if (GUILayout.Button(string.Empty, style, GUILayout.Width(12f)))
                    {
                        s_expendedFlags ^= flag;
                        isChanged = true;
                    }
                }
                else
                {
                    ShowFoldoutBlank();
                }
            }

            public static bool ShowFoldoutButtonWithEnabledToggle(Index i, GUIContent content, bool toggleValue)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutButton(i, toggleValue);
                var toggleResult = EditorGUILayout.ToggleLeft(content, toggleValue);
                if (toggleResult != toggleValue) { s_guiChanged = true; }
                GUILayout.EndHorizontal();
                return toggleResult;
            }

            public static void ShowFoldoutBlankWithDisbledToggle(GUIContent content)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutBlank();
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(content, false);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            public static bool ShowFoldoutBlankWithEnabledToggle(GUIContent content, bool toggleValue)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutBlank();
                var toggleResult = EditorGUILayout.ToggleLeft(content, toggleValue);
                if (toggleResult != toggleValue) { s_guiChanged = true; }
                GUILayout.EndHorizontal();
                return toggleResult;
            }

            private static bool IsExpended(uint flag)
            {
                return (s_expendedFlags & flag) > 0;
            }

            public static bool IsExpended(Index i)
            {
                return IsExpended(Flag(i));
            }

            //public static void ApplyChanges()
            //{
            //    if (!isChanged) { return; }

            //    EditorPrefs.SetInt(s_prefKey, (int)s_expendedFlags);
            //}
        }

        private const float s_buttonIndent = 35f;
        private static GUIStyle s_labelStyle;
        private static bool s_guiChanged;
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

        public static bool canSupportSimulator
        {
            get
            {
                return true;
            }
        }

        public static bool supportSimulator
        {
            get
            {
                return canSupportSimulator && VIUSettings.activateSimulatorModule;
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
            if (s_labelStyle == null)
            {
                s_labelStyle = new GUIStyle(EditorStyles.label);
                s_labelStyle.richText = true;
                Foldouter.Initialize();
            }

            s_guiChanged = false;

            EditorGUILayout.SelectableLabel("<b>Version</b> v" + VIUVersion.current, s_labelStyle);

            EditorGUILayout.LabelField("<b>Supporting Device</b>", s_labelStyle);
            GUILayout.Space(5);

            const string supportSimulatorTitle = "Simulator";
            if (canSupportSimulator)
            {
                supportSimulator = Foldouter.ShowFoldoutButtonWithEnabledToggle(Foldouter.Index.Simulator, new GUIContent(supportSimulatorTitle, "If checked, the simulator will activated automatically if no other valid VR devices found."), supportSimulator);

                if (supportSimulator && Foldouter.IsExpended(Foldouter.Index.Simulator))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel += 2;

                    VIUSettings.simulatorAutoTrackMainCamera = EditorGUILayout.ToggleLeft(new GUIContent("Enable Auto Camera Tracking", "Main camera only"), VIUSettings.simulatorAutoTrackMainCamera);
                    if (VIUSettings.enableSimulatorKeyboardMouseControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Keyboard-Mouse Control", "You can also handle VRModule.Simulator.onUpdateDeviceState by your self."), VIUSettings.enableSimulatorKeyboardMouseControl))
                    {
                        EditorGUI.indentLevel++;
                        VIUSettings.simulateTrackpadTouch = EditorGUILayout.Toggle(new GUIContent("Simulate Trackpad Touch", VIUSettings.SIMULATE_TRACKPAD_TOUCH_TOOLTIP), VIUSettings.simulateTrackpadTouch);
                        VIUSettings.simulatorKeyMoveSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Move Speed", VIUSettings.SIMULATOR_KEY_MOVE_SPEED_TOOLTIP), VIUSettings.simulatorKeyMoveSpeed);
                        VIUSettings.simulatorKeyRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Rotate Speed", VIUSettings.SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP), VIUSettings.simulatorKeyRotateSpeed);
                        VIUSettings.simulatorMouseRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Mouse Rotate Speed"), VIUSettings.simulatorMouseRotateSpeed);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel -= 2;
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(supportSimulatorTitle));
            }

            GUILayout.Space(5);

            const string supportOpenVRTitle = "Vive (OpenVR compatible device)";
            if (canSupportOpenVR)
            {
                supportOpenVR = Foldouter.ShowFoldoutButtonWithEnabledToggle(Foldouter.Index.Vive, new GUIContent(supportOpenVRTitle), supportOpenVR);

                if (supportOpenVR && Foldouter.IsExpended(Foldouter.Index.Vive))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel += 2;

                    if (VRModule.isSteamVRPluginDetected)
                    {
                        VIUSettings.externalCameraConfigFilePath = EditorGUILayout.DelayedTextField(new GUIContent("External Camera Config Path"), VIUSettings.externalCameraConfigFilePath);

                        if (!string.IsNullOrEmpty(VIUSettings.externalCameraConfigFilePath))
                        {
                            if (!System.IO.File.Exists(VIUSettings.externalCameraConfigFilePath))
                            {
                                ShowCreateExCamCfgButton();
                            }

                            if (VIUSettings.enableExternalCameraSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable External Camera Switch", VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP), VIUSettings.enableExternalCameraSwitch))
                            {
                                EditorGUI.indentLevel++;
                                VIUSettings.externalCameraSwitchKey = (KeyCode)EditorGUILayout.EnumPopup("Switch Key", VIUSettings.externalCameraSwitchKey);
                                if (VIUSettings.externalCameraSwitchKey != KeyCode.None)
                                {
                                    VIUSettings.externalCameraSwitchKeyModifier = (KeyCode)EditorGUILayout.EnumPopup("Switch Key Modifier", VIUSettings.externalCameraSwitchKeyModifier);
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        EditorGUILayout.DelayedTextField(new GUIContent("External Camera Config File Path", "SteamVR Plugin required."), string.Empty);
                        EditorGUILayout.ToggleLeft(new GUIContent("Enable External Camera Switch", "SteamVR Plugin required."), false);
                        GUI.enabled = true;

                        var noViveTrackerSupportWanning =
#if UNITY_5_5 || UNITY_5_6
                        "Vive Tracker not supported! Install SteamVR Plugin to get support.";
#elif UNITY_2017_1_OR_NEWER
                        "Vive Tracker's input not supported! Install SteamVR Plugin to get support.";
#else 
                        string.Empty;
#endif
                        EditorGUILayout.HelpBox(noViveTrackerSupportWanning, MessageType.Warning);

                        ShowGetSteamVRPluginButton();
                    }

                    EditorGUI.indentLevel -= 2;
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }
            }
            else
            {
                if (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildTargetGroup.Standalone)
                {
                    Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(supportOpenVRTitle));
                    ShowSwitchPlatformButton(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
                else if (!VRModule.isSteamVRPluginDetected)
                {
                    Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(supportOpenVRTitle, "SteamVR Plugin required."));
                    ShowGetSteamVRPluginButton();
                }
            }

            GUILayout.Space(5f);

            const string supportOculusVRTitle = "Oculus Rift & Touch";
            if (canSupportOculus)
            {
                supportOculus = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(supportOculusVRTitle), supportOculus);
            }
            else
            {
                if (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildTargetGroup.Standalone)
                {
                    Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(supportOculusVRTitle));
                    ShowSwitchPlatformButton(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
                else if (!VRModule.isOculusVRPluginDetected)
                {
                    Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(supportOculusVRTitle, "Oculus VR plugin required."));
                    ShowGetOculusVRPluginButton();
                }
            }

            GUILayout.Space(5);

            const string supportDaydreamVRTitle = "Daydream";
            if (canSupportDaydream)
            {
                supportDaydream = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(supportDaydreamVRTitle), supportDaydream);

                if (supportDaydream)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel += 2;

                    EditorGUILayout.HelpBox("VRDevice daydream not supported in Editor Mode.  Please run on target device.", MessageType.Warning);

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

                    EditorGUI.indentLevel -= 2;
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(supportDaydreamVRTitle, (VRModule.isGoogleVRPluginDetected ? "Google VR plugin required." : "")));

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
                EditorGUI.indentLevel += 2;
                EditorGUILayout.HelpBox("Unity 5.6 or newer version required.", MessageType.Warning);
                EditorGUI.indentLevel -= 2;
#endif
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("<b>Role Binding</b>", s_labelStyle);
            GUILayout.Space(5);

            if (supportAnyStandaloneVR)
            {
                VIUSettings.autoLoadBindingConfigOnStart = Foldouter.ShowFoldoutButtonWithEnabledToggle(Foldouter.Index.AutoBinding, new GUIContent("Auto Load Binding Config on Start"), VIUSettings.autoLoadBindingConfigOnStart);

                if (VIUSettings.autoLoadBindingConfigOnStart && Foldouter.IsExpended(Foldouter.Index.AutoBinding))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel += 2;
                    VIUSettings.bindingConfigFilePath = EditorGUILayout.DelayedTextField(new GUIContent("Config File Path"), VIUSettings.bindingConfigFilePath);
                    EditorGUI.indentLevel -= 2;
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }

                VIUSettings.enableBindingInterfaceSwitch = Foldouter.ShowFoldoutButtonWithEnabledToggle(Foldouter.Index.BindingUISwitch, new GUIContent("Enable Binding Interface Switch", VIUSettings.BIND_UI_SWITCH_TOOLTIP), VIUSettings.enableBindingInterfaceSwitch);

                if (VIUSettings.enableBindingInterfaceSwitch && Foldouter.IsExpended(Foldouter.Index.BindingUISwitch))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel += 2;
                    VIUSettings.bindingInterfaceSwitchKey = (KeyCode)EditorGUILayout.EnumPopup("Switch Key", VIUSettings.bindingInterfaceSwitchKey);
                    if (VIUSettings.bindingInterfaceSwitchKey != KeyCode.None)
                    {
                        VIUSettings.bindingInterfaceSwitchKeyModifier = (KeyCode)EditorGUILayout.EnumPopup("Switch Key Modifier", VIUSettings.bindingInterfaceSwitchKeyModifier);
                    }
                    VIUSettings.bindingInterfaceObjectSource = EditorGUILayout.ObjectField("Interface Prefab", VIUSettings.bindingInterfaceObjectSource, typeof(GameObject), false) as GameObject;
                    EditorGUI.indentLevel -= 2;
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent("Auto Load Binding Config on Start", "Role Binding only works on standalone device."));
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent("Enable Binding Interface Switch", "Role Binding only works on sstandalone device."));
            }

            //Foldouter.ApplyChanges();

            var assetPath = AssetDatabase.GetAssetPath(VIUSettings.Instance);

            if (s_guiChanged)
            {
                EnabledDevices.ApplyChanges();

                if (string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.CreateAsset(VIUSettings.Instance, defaultAssetPath);
                }
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("    Use Defaults    "))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    supportSimulator = canSupportSimulator;
                    supportOpenVR = canSupportOpenVR;
                    supportOculus = canSupportOculus;
                    supportDaydream = canSupportDaydream;

                    EnabledDevices.ApplyChanges();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private static void ShowSwitchPlatformButton(BuildTargetGroup group, BuildTarget target)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(s_buttonIndent);
            if (GUILayout.Button("Switch Platform to " + group))
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

        private static void ShowCreateExCamCfgButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(s_buttonIndent);
            if (GUILayout.Button(new GUIContent("Create Default Config File", "To get External Camera work in playmode, the config file must exits under project folder or build folder when start playing.")))
            {
                System.IO.File.WriteAllText(VIUSettings.externalCameraConfigFilePath,
@"x=0
y=0
z=0
rx=0
ry=0
rz=0
fov=60
near=0.1
far=100
sceneResolutionScale=0.5");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}