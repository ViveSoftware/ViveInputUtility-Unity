//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTC.UnityPlugin.Vive
{
    public static class VIUSettingsEditor
    {
        public interface ISupportedSDK
        {
            string name { get; }
            bool enabled { get; set; }
        }

        private static class VRSDKSettings
        {
            public class VRSDK : ISupportedSDK
            {
                private bool m_sdkEnabled;

                public string name { get; private set; }
                public bool addLast { get; private set; }

                public bool enabled
                {
#if UNITY_5_4_OR_NEWER
                    get
                    {
                        Update();
                        return s_vrEnabled && m_sdkEnabled;
                    }
                    set
                    {
                        if (enabled == value) { return; }

                        s_isDirty = true;

                        if (value)
                        {
                            if (!m_sdkEnabled)
                            {
                                if (addLast)
                                {
                                    s_enabledSDKNames.Add(name);
                                }
                                else
                                {
                                    s_enabledSDKNames.Insert(0, name);
                                }
                            }

                            s_vrEnabled = true;
                        }
                        else
                        {
                            if (m_sdkEnabled)
                            {
                                s_enabledSDKNames.Remove(name);
                            }
                        }

                        m_sdkEnabled = value;
                    }
#else
                    get { return false; }
                    set { }
#endif
                }

                public VRSDK(string name, bool addLast = false) { this.name = name; this.addLast = addLast; }
                public void Reset() { m_sdkEnabled = false; }
                public bool Validate(string skdName) { return !m_sdkEnabled && (m_sdkEnabled = (name == skdName)); } // return true if confirmed
            }

            private static bool s_initialized;
            private static int s_updatedFrame = -1;
            private static List<string> s_enabledSDKNames;
            private static List<VRSDK> s_supportedSDKs;
            private static bool s_isDirty;
            private static bool s_vrEnabled;
            private static SerializedObject s_projectSettingAsset;
            private static SerializedProperty s_enabledProp;
            private static SerializedProperty s_devicesProp;

            public static readonly VRSDK Oculus = new VRSDK("Oculus");
            public static readonly VRSDK OpenVR = new VRSDK("OpenVR", true);
            public static readonly VRSDK Daydream = new VRSDK("daydream");

            public static bool vrEnabled
            {
                get
                {
                    Update();

                    return s_vrEnabled;
                }
                set
                {
                    if (vrEnabled != value)
                    {
                        s_isDirty = true;
                        s_vrEnabled = value;
                    }
                }
            }

            private static void Initialize()
            {
                if (s_initialized) { return; }
                s_initialized = true;

                s_enabledSDKNames = new List<string>();
                s_supportedSDKs = new List<VRSDK>
                {
                    Oculus,
                    OpenVR,
                    Daydream,
                };

                s_projectSettingAsset = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
#if UNITY_5_5_OR_NEWER
                var buildTargetGroupName = activeBuildTargetGroup.ToString();
                var targetVRSettingsArray = s_projectSettingAsset.FindProperty("m_BuildTargetVRSettings");
                var targetVRSettings = default(SerializedProperty);

                for (int i = 0, imax = targetVRSettingsArray.arraySize; i < imax; ++i)
                {
                    var element = targetVRSettingsArray.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("m_BuildTarget").stringValue == buildTargetGroupName)
                    {
                        targetVRSettings = element;
                    }
                }

                if (targetVRSettings == null)
                {
                    targetVRSettingsArray.arraySize += 1;
                    targetVRSettings = targetVRSettingsArray.GetArrayElementAtIndex(targetVRSettingsArray.arraySize - 1);
                    targetVRSettings.FindPropertyRelative("m_BuildTarget").stringValue = buildTargetGroupName;
                    s_projectSettingAsset.ApplyModifiedProperties();
                }

                s_enabledProp = targetVRSettings.FindPropertyRelative("m_Enabled");
                s_devicesProp = targetVRSettings.FindPropertyRelative("m_Devices");
#elif UNITY_5_4_OR_NEWER
                s_enabledProp = s_projectSettingAsset.FindProperty(activeBuildTargetGroup + "::VR::enable");
                s_devicesProp = s_projectSettingAsset.FindProperty(activeBuildTargetGroup + "::VR::enabledDevices");
#else
                s_enabledProp = s_projectSettingAsset.FindProperty("virtualRealitySupported");
#endif
            }

            public static void Update()
            {
                if (!ChangeProp.Set(ref s_updatedFrame, Time.frameCount)) { return; }

                Initialize();

                s_projectSettingAsset.Update();

                vrEnabled = s_enabledProp.boolValue;

                if (s_devicesProp != null)
                {
                    s_enabledSDKNames.Clear();
                    s_supportedSDKs.ForEach(sdk => sdk.Reset());

                    for (int i = s_devicesProp.arraySize - 1; i >= 0; --i)
                    {
                        var name = s_devicesProp.GetArrayElementAtIndex(i).stringValue;
                        s_enabledSDKNames.Add(name);
                        foreach (var sdk in s_supportedSDKs)
                        {
                            if (sdk.Validate(name)) { break; }
                        }
                    }
                }
            }

            public static void ApplyChanges()
            {
                if (!s_isDirty) { return; }
                s_isDirty = false;

                s_projectSettingAsset.Update();

                if (s_devicesProp != null)
                {
                    if (s_devicesProp.arraySize != s_enabledSDKNames.Count)
                    {
                        s_devicesProp.arraySize = s_enabledSDKNames.Count;
                    }

                    for (int i = s_enabledSDKNames.Count - 1; i >= 0; --i)
                    {
                        s_devicesProp.GetArrayElementAtIndex(i).stringValue = s_enabledSDKNames[i];
                    }
                }

                s_enabledProp.boolValue = vrEnabled;

                s_projectSettingAsset.ApplyModifiedProperties();
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
                s_styleExpended.active = s_styleFoleded.onActive;
            }

            public static void ShowFoldoutBlank()
            {
                GUILayout.Space(20f);
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

        //private const float s_buttonIndent = 35f;
        private static float s_warningHeight;
        private static GUIStyle s_labelStyle;
        private static bool s_guiChanged;
        private static string s_defaultAssetPath;

        public static bool virtualRealitySupported { get { return VRSDKSettings.vrEnabled; } set { VRSDKSettings.vrEnabled = value; } }
        public static ISupportedSDK OpenVRSDK { get { return VRSDKSettings.OpenVR; } }
        public static ISupportedSDK OculusSDK { get { return VRSDKSettings.Oculus; } }
        public static ISupportedSDK DaydreamSDK { get { return VRSDKSettings.Daydream; } }
        public static void ApplySDKChanges() { VRSDKSettings.ApplyChanges(); }

        public static BuildTargetGroup activeBuildTargetGroup { get { return BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget); } }

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
                    activeBuildTargetGroup == BuildTargetGroup.Standalone
#if !UNITY_5_5_OR_NEWER
                    && VRModule.isSteamVRPluginDetected
#endif
                    ;
            }
        }

        public static bool supportOpenVR
        {
            get
            {
#if UNITY_5_5_OR_NEWER
                return canSupportOpenVR && (VIUSettings.activateSteamVRModule || VIUSettings.activateUnityNativeVRModule) && OpenVRSDK.enabled;
#elif UNITY_5_4_OR_NEWER
                return canSupportOpenVR && VIUSettings.activateSteamVRModule && OpenVRSDK.enabled;
#else
                return canSupportOpenVR && VIUSettings.activateSteamVRModule && !virtualRealitySupported;
#endif
            }
            set
            {
                if (supportOpenVR == value) { return; }

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

        public static bool canSupportOculus
        {
            get
            {
                return
                    activeBuildTargetGroup == BuildTargetGroup.Standalone
#if !UNITY_5_5_OR_NEWER
                    && VRModule.isOculusVRPluginDetected
#endif
                    ;
            }
        }

        public static bool supportOculus
        {
            get
            {
#if UNITY_5_5_OR_NEWER
                return canSupportOculus && (VIUSettings.activateOculusVRModule || VIUSettings.activateUnityNativeVRModule) && OculusSDK.enabled;
#elif UNITY_5_4_OR_NEWER
                return canSupportOculus && VIUSettings.activateOculusVRModule && OculusSDK.enabled;
#else
                return canSupportOculus && VIUSettings.activateOculusVRModule && virtualRealitySupported;
#endif
            }
            set
            {
                if (supportOculus == value) { return; }

                VIUSettings.activateOculusVRModule = value;

#if UNITY_5_5_OR_NEWER
                OpenVRSDK.enabled = value;
                VIUSettings.activateUnityNativeVRModule = value || supportOpenVR;
#elif UNITY_5_4_OR_NEWER
                OpenVRSDK.enabled = value;
#else
                virtualRealitySupported = value;
#endif
            }
        }

#if UNITY_5_6_OR_NEWER
        public static bool canSupportDaydream
        {
            get
            {
                return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isGoogleVRPluginDetected;
            }
        }

        public static bool supportDaydream
        {
            get
            {
                if (!canSupportDaydream || !VIUSettings.activateGoogleVRModule || !DaydreamSDK.enabled || PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
                {
                    return false;
                }

                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                {
                    if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) == true) { return false; }

                    var apiList = ListPool<GraphicsDeviceType>.Get();
                    apiList.AddRange(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android));
                    var result = !apiList.Contains(GraphicsDeviceType.OpenGLES2) && apiList.Contains(GraphicsDeviceType.OpenGLES3) && !apiList.Contains(GraphicsDeviceType.Vulkan);
                    ListPool<GraphicsDeviceType>.Release(apiList);
                    return result;
                }

                return true;
            }
            set
            {
                if (supportDaydream == value) { return; }

                DaydreamSDK.enabled = value;
                VIUSettings.activateGoogleVRModule = value;

                if (value)
                {
                    if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
                    {
                        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                    }

                    if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    {
                        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                        var listChanged = false;
                        var apiList = ListPool<GraphicsDeviceType>.Get();
                        apiList.AddRange(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android));
                        if (!apiList.Contains(GraphicsDeviceType.OpenGLES3)) { apiList.Add(GraphicsDeviceType.OpenGLES3); listChanged = true; }
                        // FIXME: Daydream SDK currently not support Vulkan API
                        if (apiList.Remove(GraphicsDeviceType.Vulkan)) { listChanged = true; }
                        if (apiList.Remove(GraphicsDeviceType.OpenGLES2)) { listChanged = true; }
                        if (listChanged) { PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apiList.ToArray()); }
                    }
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

            EditorGUILayout.LabelField("<b>Version</b> v" + VIUVersion.current, s_labelStyle);
            ShowGetReleaseNoteButton();

            GUILayout.Space(10);

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
                if (VRModule.isSteamVRPluginDetected)
                {
                    supportOpenVR = Foldouter.ShowFoldoutButtonWithEnabledToggle(Foldouter.Index.Vive, new GUIContent(supportOpenVRTitle), supportOpenVR);

                    if (supportOpenVR && Foldouter.IsExpended(Foldouter.Index.Vive))
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.indentLevel += 2;

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

                        EditorGUI.indentLevel -= 2;
                        s_guiChanged |= EditorGUI.EndChangeCheck();
                    }
                }
                else
                {
                    supportOpenVR = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(supportOpenVRTitle), supportOpenVR);

                    if (supportOpenVR)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.indentLevel += 2;

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox("External-Camera(Mix-Reality), animated controller model, Vive Controller haptics(vibration)" +
#if UNITY_2017_1_OR_NEWER
                        ", Vive Tracker USB/Pogo-pin input" +
#else
                        ", Vive Tracker device" +
#endif
                        " NOT supported! Install SteamVR Plugin to get support.", MessageType.Warning);

                        s_warningHeight = Mathf.Max(s_warningHeight, GUILayoutUtility.GetLastRect().height);

                        if (!VRModule.isSteamVRPluginDetected)
                        {
                            GUILayout.BeginVertical(GUILayout.Height(s_warningHeight));
                            GUILayout.FlexibleSpace();
                            ShowGetSteamVRPluginButton();
                            GUILayout.FlexibleSpace();
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();

                        EditorGUI.indentLevel -= 2;
                        s_guiChanged |= EditorGUI.EndChangeCheck();
                    }
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                Foldouter.ShowFoldoutBlank();

                if (activeBuildTargetGroup != BuildTargetGroup.Standalone)
                {
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(supportOpenVRTitle, "Standalone platform required."), false, GUILayout.Width(230f));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowSwitchPlatformButton(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
                else if (!VRModule.isSteamVRPluginDetected)
                {
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(supportOpenVRTitle, "SteamVR Plugin required."), false, GUILayout.Width(230f));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowGetSteamVRPluginButton();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5f);

            const string supportOculusVRTitle = "Oculus Rift & Touch";
            if (canSupportOculus)
            {
                supportOculus = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(supportOculusVRTitle), supportOculus);
            }
            else
            {
                GUILayout.BeginHorizontal();
                Foldouter.ShowFoldoutBlank();

                if (activeBuildTargetGroup != BuildTargetGroup.Standalone)
                {
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(supportOculusVRTitle, "Standalone platform required."), false, GUILayout.Width(150f));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowSwitchPlatformButton(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
                else if (!VRModule.isOculusVRPluginDetected)
                {
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(supportOculusVRTitle, "Oculus VR Plugin required."), false, GUILayout.Width(150f));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowGetOculusVRPluginButton();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            const string supportDaydreamVRTitle = "Daydream";
            if (canSupportDaydream)
            {
                supportDaydream = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(supportDaydreamVRTitle), supportDaydream);

                if (supportDaydream)
                {
                    EditorGUI.indentLevel += 2;

                    EditorGUILayout.HelpBox("VRDevice daydream not supported in Editor Mode.  Please run on target device.", MessageType.Info);

                    // following preferences is stored at HKEY_CURRENT_USER\Software\Unity Technologies\Unity Editor 5.x\
                    if (string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot")))
                    {
                        EditorGUILayout.HelpBox("AndroidSdkRoot is empty. Setup at Edit -> Preferences... -> External Tools -> Android SDK", MessageType.Warning);
                    }

                    if (string.IsNullOrEmpty(EditorPrefs.GetString("JdkPath")))
                    {
                        EditorGUILayout.HelpBox("JdkPath is empty. Setup at Edit -> Preferences... -> External Tools -> Android JDK", MessageType.Warning);
                    }

                    // Optional
                    //if (string.IsNullOrEmpty(EditorPrefs.GetString("AndroidNdkRoot")))
                    //{
                    //    EditorGUILayout.HelpBox("AndroidNdkRoot is empty. Setup at Edit -> Preferences... -> External Tools -> Android SDK", MessageType.Warning);
                    //}

                    EditorGUI.indentLevel -= 2;
                }
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
                tooltip = "Unity 5.6 or later required.";
#endif
                GUI.enabled = false;
                ShowToggle(new GUIContent(supportDaydreamVRTitle, tooltip), false, GUILayout.Width(80f));
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
                    ShowGetGoogleVRPluginButton();
                }
#endif
                GUILayout.EndHorizontal();
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
                    VIUSettings.bindingConfigFilePath = EditorGUILayout.DelayedTextField(new GUIContent("Config Path"), VIUSettings.bindingConfigFilePath);
                    EditorGUI.indentLevel -= 2;
                    s_guiChanged |= EditorGUI.EndChangeCheck();
                }

                GUILayout.Space(5);

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

                GUILayout.Space(5);

                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent("Enable Binding Interface Switch", "Role Binding only works on sstandalone device."));
            }

            //Foldouter.ApplyChanges();
            ApplySDKChanges();

            var assetPath = AssetDatabase.GetAssetPath(VIUSettings.Instance);

            if (s_guiChanged)
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.CreateAsset(VIUSettings.Instance, defaultAssetPath);
                }
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Use Defaults"))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    supportSimulator = canSupportSimulator;
                    supportOpenVR = canSupportOpenVR;
                    //supportOculus = canSupportOculus;
                    //supportDaydream = canSupportDaydream;

                    VRSDKSettings.ApplyChanges();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private static bool ShowToggle(GUIContent label, bool value, params GUILayoutOption[] options)
        {
            var result = EditorGUILayout.ToggleLeft(label, value, options);
            if (result != value) { s_guiChanged = true; }
            return result;
        }

        private static void ShowSwitchPlatformButton(BuildTargetGroup group, BuildTarget target)
        {
            if (GUILayout.Button(new GUIContent("Swich Platform", "Switch platform to " + group), GUILayout.ExpandWidth(false)))
            {
#if UNITY_2017_1_OR_NEWER
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(group, target);
#elif UNITY_5_6_OR_NEWER
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
#else
                EditorUserBuildSettings.SwitchActiveBuildTarget(target);
#endif
            }
        }

        private static void ShowGetReleaseNoteButton()
        {
            if (GUILayout.Button("Release Note", GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("https://github.com/ViveSoftware/ViveInputUtility-Unity/releases");
            }
        }

        private static void ShowGetSteamVRPluginButton()
        {
            const string url = "https://www.assetstore.unity3d.com/en/#!/content/32647";

            if (GUILayout.Button(new GUIContent("Get Plugin", url), GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL(url);
            }
        }

        private static void ShowGetOculusVRPluginButton()
        {
            const string url = "https://developer.oculus.com/downloads/package/oculus-utilities-for-unity-5/";

            if (GUILayout.Button(new GUIContent("Get Plugin", url), GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL(url);
            }
        }

        private static void ShowGetGoogleVRPluginButton()
        {
            const string url = "https://developers.google.com/vr/develop/unity/download";

            if (GUILayout.Button(new GUIContent("Get Plugin", url), GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL(url);
            }
        }

        private static void ShowCreateExCamCfgButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Generate Default Config File", "To get External Camera work in playmode, the config file must exits under project folder or build folder when start playing.")))
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
            GUILayout.EndHorizontal();
        }
    }
}