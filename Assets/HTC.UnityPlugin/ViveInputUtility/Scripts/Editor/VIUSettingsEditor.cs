//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using System.IO;
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

                            if (s_enabledSDKNames.Count == 0)
                            {
                                s_vrEnabled = false;
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
            private static SerializedProperty s_devicesProp = null;

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
                        break;
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

                s_enabledProp.boolValue = s_vrEnabled;

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
                OculusGo,
            }

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
                s_expendedFlags = ~0u;

                s_styleFoleded = new GUIStyle(EditorStyles.foldout);
                s_styleExpended = new GUIStyle(EditorStyles.foldout);
                s_styleExpended.normal = s_styleFoleded.onNormal;
                s_styleExpended.active = s_styleFoleded.onActive;
            }

            public static void ShowFoldoutBlank()
            {
                GUILayout.Space(20f);
            }

            public static void ShowFoldoutButton(Index i)
            {
                var flag = Flag(i);
                var style = IsExpended(flag) ? s_styleExpended : s_styleFoleded;
                if (GUILayout.Button(string.Empty, style, GUILayout.Width(12f)))
                {
                    s_expendedFlags ^= flag;
                    isChanged = true;
                }
            }

            public static bool ShowFoldoutButtonOnToggleEnabled(Index i, GUIContent content, bool toggleValue)
            {
                GUILayout.BeginHorizontal();
                if (toggleValue)
                {
                    ShowFoldoutButton(i);
                }
                else
                {
                    ShowFoldoutBlank();
                }
                var toggleResult = EditorGUILayout.ToggleLeft(content, toggleValue, s_labelStyle);
                if (toggleResult != toggleValue) { s_guiChanged = true; }
                GUILayout.EndHorizontal();
                return toggleResult;
            }

            public static bool ShowFoldoutButtonWithEnabledToggle(Index i, GUIContent content, bool toggleValue)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutButton(i);
                var toggleResult = EditorGUILayout.ToggleLeft(content, toggleValue, s_labelStyle);
                if (toggleResult != toggleValue) { s_guiChanged = true; }
                GUILayout.EndHorizontal();
                return toggleResult;
            }

            public static void ShowFoldoutButtonWithDisbledToggle(Index i, GUIContent content)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutButton(i);
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(content, false, s_labelStyle);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            public static void ShowFoldoutBlankWithDisbledToggle(GUIContent content)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutBlank();
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(content, false, s_labelStyle);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            public static bool ShowFoldoutBlankWithEnabledToggle(GUIContent content, bool toggleValue)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutBlank();
                var toggleResult = EditorGUILayout.ToggleLeft(content, toggleValue, s_labelStyle);
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

        public const string URL_VIU_GITHUB_RELEASE_PAGE = "https://github.com/ViveSoftware/ViveInputUtility-Unity/releases";
        public const string URL_STEAM_VR_PLUGIN = "https://www.assetstore.unity3d.com/en/#!/content/32647";
        public const string URL_OCULUS_VR_PLUGIN = "https://developer.oculus.com/downloads/package/oculus-utilities-for-unity-5/";
        public const string URL_GOOGLE_VR_PLUGIN = "https://developers.google.com/vr/develop/unity/download";
        public const string URL_WAVE_VR_PLUGIN = "https://developer.vive.com/resources/knowledgebase/wave-sdk/";
        public const string URL_WAVE_VR_6DOF_SUMULATOR_USAGE_PAGE = "https://github.com/ViveSoftware/ViveInputUtility-Unity/wiki/Wave-VR-6-DoF-Controller-Simulator";

        private static Vector2 s_scrollValue = Vector2.zero;
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

        public static bool supportAnyAndroidVR { get { return supportDaydream || supportWaveVR || supportOculusGo; } }

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
#if !UNITY_5_5_OR_NEWER || UNITY_5_6_0 || UNITY_5_6_1 || UNITY_5_6_2
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
                OculusSDK.enabled = value;
                VIUSettings.activateUnityNativeVRModule = value || supportOpenVR;
#elif UNITY_5_4_OR_NEWER
                OculusSDK.enabled = value;
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
                if (!canSupportDaydream) { return false; }
                if (!VIUSettings.activateGoogleVRModule) { return false; }
                if (!DaydreamSDK.enabled) { return false; }
                if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24) { return false; }
                if (PlayerSettings.colorSpace == ColorSpace.Linear && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                return true;
            }
            set
            {
                if (supportDaydream == value) { return; }

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
        }
#else
        public static bool canSupportDaydream { get { return false; } }

        public static bool supportDaydream { get { return false; } set { } }
#endif

#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1 && !UNITY_5_6_2
        public static bool canSupportWaveVR
        {
            get
            {
                return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isWaveVRPluginDetected;
            }
        }

        public static bool supportWaveVR
        {
            get
            {
                if (!canSupportWaveVR) { return false; }
                if (!VIUSettings.activateWaveVRModule) { return false; }
                if (virtualRealitySupported) { return false; }
                if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel23) { return false; }
                if (PlayerSettings.colorSpace == ColorSpace.Linear && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                return true;
            }
            set
            {
                if (supportWaveVR == value) { return; }

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
        }
#else
        public static bool canSupportWaveVR { get { return false; } }

        public static bool supportWaveVR { get { return false; } set { } }
#endif

#if UNITY_5_6_OR_NEWER
        public static bool canSupportOculusGo
        {
            get
            {
                return activeBuildTargetGroup == BuildTargetGroup.Android && VRModule.isOculusVRPluginDetected;
            }
        }

        public static bool supportOculusGo
        {
            get
            {
                if (!canSupportOculusGo) { return false; }
                if (!VIUSettings.activateOculusVRModule) { return false; }
                if (!OculusSDK.enabled) { return false; }
                if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel21) { return false; }
                if (PlayerSettings.graphicsJobs) { return false; }
                if ((PlayerSettings.colorSpace == ColorSpace.Linear || PlayerSettings.gpuSkinning) && !GraphicsAPIContainsOnly(BuildTarget.Android, GraphicsDeviceType.OpenGLES3)) { return false; }
                return true;
            }
            set
            {
                if (supportOculusGo == value) { return; }

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
        }
#else
        public static bool canSupportOculusGo { get { return false; } }

        public static bool supportOculusGo { get { return false; } set { } }
#endif

        public static bool GraphicsAPIContainsOnly(BuildTarget buildTarget, params GraphicsDeviceType[] types)
        {
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(buildTarget)) { return false; }

            var result = false;
            var apiList = ListPool<GraphicsDeviceType>.Get();
            apiList.AddRange(PlayerSettings.GetGraphicsAPIs(buildTarget));
            if (types.Length == apiList.Count)
            {
                result = true;
                for (int i = 0, imax = apiList.Count; i < imax; ++i)
                {
                    if (apiList[i] != types[i]) { result = false; break; }
                }
            }
            ListPool<GraphicsDeviceType>.Release(apiList);
            return result;
        }

        public static void SetGraphicsAPI(BuildTarget buildTarget, params GraphicsDeviceType[] types)
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(buildTarget, false);
            PlayerSettings.SetGraphicsAPIs(buildTarget, types);
        }

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

            s_scrollValue = EditorGUILayout.BeginScrollView(s_scrollValue);

            EditorGUILayout.LabelField("<b>VIVE Input Utility v" + VIUVersion.current + "</b>", s_labelStyle);
            VIUSettings.autoCheckNewVIUVersion = EditorGUILayout.ToggleLeft("Auto Check Latest Version", VIUSettings.autoCheckNewVIUVersion);

            GUILayout.BeginHorizontal();
            ShowUrlLinkButton(URL_VIU_GITHUB_RELEASE_PAGE, "Get Latest Release");
            ShowCheckRecommendedSettingsButton();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("<b>Supporting Device</b>", s_labelStyle);
            GUILayout.Space(5);

            const string supportSimulatorTitle = "Simulator";
            if (canSupportSimulator)
            {
                supportSimulator = Foldouter.ShowFoldoutButtonOnToggleEnabled(Foldouter.Index.Simulator, new GUIContent(supportSimulatorTitle, "If checked, the simulator will activated automatically if no other valid VR devices found."), supportSimulator);
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(supportSimulatorTitle));
            }

            if (supportSimulator && Foldouter.IsExpended(Foldouter.Index.Simulator))
            {
                if (supportSimulator) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                {
                    EditorGUI.indentLevel += 2;
                    VIUSettings.simulatorAutoTrackMainCamera = EditorGUILayout.ToggleLeft(new GUIContent("Enable Auto Camera Tracking", "Main camera only"), VIUSettings.simulatorAutoTrackMainCamera);
                    VIUSettings.enableSimulatorKeyboardMouseControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Keyboard-Mouse Control", "You can also control Simulator devices by handling VRModule.Simulator.onUpdateDeviceState event."), VIUSettings.enableSimulatorKeyboardMouseControl);

                    if (!VIUSettings.enableSimulatorKeyboardMouseControl && supportSimulator) { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel++;
                        VIUSettings.simulateTrackpadTouch = EditorGUILayout.Toggle(new GUIContent("Simulate Trackpad Touch", VIUSettings.SIMULATE_TRACKPAD_TOUCH_TOOLTIP), VIUSettings.simulateTrackpadTouch);
                        VIUSettings.simulatorKeyMoveSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Move Speed", VIUSettings.SIMULATOR_KEY_MOVE_SPEED_TOOLTIP), VIUSettings.simulatorKeyMoveSpeed);
                        VIUSettings.simulatorKeyRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Rotate Speed", VIUSettings.SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP), VIUSettings.simulatorKeyRotateSpeed);
                        VIUSettings.simulatorMouseRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Mouse Rotate Speed"), VIUSettings.simulatorMouseRotateSpeed);
                        EditorGUI.indentLevel--;
                    }
                    if (!VIUSettings.enableSimulatorKeyboardMouseControl && supportSimulator) { GUI.enabled = true; }

                    EditorGUI.indentLevel -= 2;
                }
                if (supportSimulator) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
            }

            GUILayout.Space(5);

            const string supportOpenVRTitle = "VIVE <size=9>(OpenVR compatible device)</size>";
            if (canSupportOpenVR)
            {
                supportOpenVR = Foldouter.ShowFoldoutButtonOnToggleEnabled(Foldouter.Index.Vive, new GUIContent(supportOpenVRTitle), supportOpenVR);
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
                    ShowUrlLinkButton(URL_STEAM_VR_PLUGIN);
                }

                GUILayout.EndHorizontal();
            }

            if (supportOpenVR && Foldouter.IsExpended(Foldouter.Index.Vive))
            {
                if (supportOpenVR && VRModule.isSteamVRPluginDetected) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                {
                    EditorGUI.indentLevel += 2;

                    VIUSettings.autoLoadExternalCameraConfigOnStart = EditorGUILayout.ToggleLeft(new GUIContent("Load Config and Enable External Camera on Start", "You can also load config by calling ExternalCameraHook.LoadConfigFromFile(path) in script."), VIUSettings.autoLoadExternalCameraConfigOnStart);
                    if (!VIUSettings.autoLoadExternalCameraConfigOnStart && supportOpenVR) { GUI.enabled = false; }
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
                        if (VIUSettings.autoLoadExternalCameraConfigOnStart && supportOpenVR && !File.Exists(VIUSettings.externalCameraConfigFilePath))
                        {
                            if (supportOpenVR && VRModule.isSteamVRPluginDetected) { s_guiChanged |= EditorGUI.EndChangeCheck(); }
                            ShowCreateExCamCfgButton();
                            if (supportOpenVR && VRModule.isSteamVRPluginDetected) { EditorGUI.BeginChangeCheck(); }
                        }

                        EditorGUI.indentLevel--;
                    }
                    if (!VIUSettings.autoLoadExternalCameraConfigOnStart && supportOpenVR) { GUI.enabled = true; }

                    VIUSettings.enableExternalCameraSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable External Camera Switch", VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP), VIUSettings.enableExternalCameraSwitch);
                    if (!VIUSettings.enableExternalCameraSwitch && supportOpenVR) { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel++;

                        VIUSettings.externalCameraSwitchKey = (KeyCode)EditorGUILayout.EnumPopup("Switch Key", VIUSettings.externalCameraSwitchKey);
                        VIUSettings.externalCameraSwitchKeyModifier = (KeyCode)EditorGUILayout.EnumPopup("Switch Key Modifier", VIUSettings.externalCameraSwitchKeyModifier);

                        EditorGUI.indentLevel--;
                    }
                    if (!VIUSettings.enableExternalCameraSwitch && supportOpenVR) { GUI.enabled = true; }

                    EditorGUI.indentLevel -= 2;
                }
                if (supportOpenVR && VRModule.isSteamVRPluginDetected) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
            }

            if (supportOpenVR && !VRModule.isSteamVRPluginDetected)
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
                    ShowUrlLinkButton(URL_OCULUS_VR_PLUGIN);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            const string supportDaydreamVRTitle = "Daydream";
            if (canSupportDaydream)
            {
                supportDaydream = Foldouter.ShowFoldoutButtonOnToggleEnabled(Foldouter.Index.Daydream, new GUIContent(supportDaydreamVRTitle), supportDaydream);
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
                    ShowUrlLinkButton(URL_GOOGLE_VR_PLUGIN);
                }
#endif
                GUILayout.EndHorizontal();
            }

            if (supportDaydream && Foldouter.IsExpended(Foldouter.Index.Daydream))
            {
                if (supportDaydream) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                {
                    EditorGUI.indentLevel += 2;

                    VIUSettings.daydreamSyncPadPressToTrigger = EditorGUILayout.ToggleLeft(new GUIContent("Sync Pad Press to Trigger", "Enable this option to handle the trigger button since the Daydream controller lacks one."), VIUSettings.daydreamSyncPadPressToTrigger);

                    EditorGUI.indentLevel -= 2;
                }
                if (supportDaydream) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
            }

            if (supportDaydream)
            {
                EditorGUI.indentLevel += 2;

                EditorGUILayout.HelpBox("VRDevice daydream not supported in Editor Mode. Please run on target device.", MessageType.Info);

                EditorGUI.indentLevel -= 2;
            }

            GUILayout.Space(5);

            const string supportWaveVRTitle = "VIVE Focus <size=9>(WaveVR compatible device)</size>";
            if (canSupportWaveVR)
            {
                supportWaveVR = Foldouter.ShowFoldoutButtonOnToggleEnabled(Foldouter.Index.WaveVR, new GUIContent(supportWaveVRTitle), supportWaveVR);
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
                    ShowToggle(new GUIContent(supportWaveVRTitle, "Android platform required."), false, GUILayout.Width(wvrToggleWidth));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowSwitchPlatformButton(BuildTargetGroup.Android, BuildTarget.Android);
                }
                else if (!VRModule.isWaveVRPluginDetected)
                {
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(supportWaveVRTitle, "Wave VR plugin required."), false, GUILayout.Width(wvrToggleWidth));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowUrlLinkButton(URL_WAVE_VR_PLUGIN);
                }
#else
                GUI.enabled = false;
                ShowToggle(new GUIContent(supportWaveVRTitle, "Unity 5.6.3 or later version required."), false, GUILayout.Width(wvrToggleWidth));
                GUI.enabled = true;
#endif
                GUILayout.EndHorizontal();
            }

            if (supportWaveVR && Foldouter.IsExpended(Foldouter.Index.WaveVR))
            {
                if (supportWaveVR) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                {
                    EditorGUI.indentLevel += 2;

                    VIUSettings.waveVRAddVirtualArmTo3DoFController = EditorGUILayout.ToggleLeft(new GUIContent("Add Airtual Arm for 3 Dof Controller"), VIUSettings.waveVRAddVirtualArmTo3DoFController);
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
                if (supportWaveVR) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
            }

            if (supportWaveVR)
            {
                EditorGUI.indentLevel += 2;

                EditorGUILayout.HelpBox("WaveVR device not supported in Editor Mode. Please run on target device.", MessageType.Info);

                EditorGUI.indentLevel -= 2;
            }

            GUILayout.Space(5);

            const string supportOculusGoTitle = "Oculus Go";
            if (canSupportOculusGo)
            {
                supportOculusGo = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(supportOculusGoTitle), supportOculusGo);
            }
            else
            {
                GUILayout.BeginHorizontal();
                Foldouter.ShowFoldoutBlank();

                if (activeBuildTargetGroup != BuildTargetGroup.Android)
                {
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(supportOculusGoTitle, "Android platform required."), false, GUILayout.Width(150f));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowSwitchPlatformButton(BuildTargetGroup.Android, BuildTarget.Android);
                }
                else if (!VRModule.isOculusVRPluginDetected)
                {
                    GUI.enabled = false;
                    ShowToggle(new GUIContent(supportOculusGoTitle, "Oculus VR Plugin required."), false, GUILayout.Width(150f));
                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    ShowUrlLinkButton(URL_OCULUS_VR_PLUGIN);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            if (supportAnyAndroidVR)
            {
                EditorGUI.indentLevel += 2;

                // on Windows, following preferences is stored at HKEY_CURRENT_USER\Software\Unity Technologies\Unity Editor 5.x\
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

#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0 && !UNITY_5_6_1 && !UNITY_5_6_2
                if (PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android).Equals("com.Company.ProductName"))
#else
                if (PlayerSettings.bundleIdentifier.Equals("com.Company.ProductName"))
#endif
                {
                    EditorGUILayout.HelpBox("Cannot build using default package name. Change at Edit -> Project Settings -> Player -> Android settings -> Other Settings -> Identification(Package Name)", MessageType.Warning);
                }

                EditorGUI.indentLevel -= 2;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("<b>Role Binding</b>", s_labelStyle);
            GUILayout.Space(5);

            if (supportAnyStandaloneVR)
            {
                VIUSettings.autoLoadBindingConfigOnStart = Foldouter.ShowFoldoutButtonOnToggleEnabled(Foldouter.Index.AutoBinding, new GUIContent("Load Binding Config on Start"), VIUSettings.autoLoadBindingConfigOnStart);
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent("Load Binding Config on Start", "Role Binding only works on standalone device."));
            }

            if (supportAnyStandaloneVR && VIUSettings.autoLoadBindingConfigOnStart && Foldouter.IsExpended(Foldouter.Index.AutoBinding))
            {
                if (supportAnyStandaloneVR && VIUSettings.autoLoadBindingConfigOnStart) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                {
                    EditorGUI.indentLevel += 2;

                    EditorGUI.BeginChangeCheck();
                    VIUSettings.bindingConfigFilePath = EditorGUILayout.DelayedTextField(new GUIContent("Config Path"), VIUSettings.bindingConfigFilePath);
                    if (string.IsNullOrEmpty(VIUSettings.bindingConfigFilePath))
                    {
                        VIUSettings.bindingConfigFilePath = VIUSettings.BINDING_CONFIG_FILE_PATH_DEFAULT_VALUE;
                        EditorGUI.EndChangeCheck();
                    }
                    else if (EditorGUI.EndChangeCheck() && VIUSettings.bindingConfigFilePath.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        VIUSettings.bindingConfigFilePath = VIUSettings.EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE;
                    }

                    EditorGUI.indentLevel -= 2;
                }
                if (supportAnyStandaloneVR && VIUSettings.autoLoadBindingConfigOnStart) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
            }

            GUILayout.Space(5);

            if (supportAnyStandaloneVR)
            {
                VIUSettings.enableBindingInterfaceSwitch = Foldouter.ShowFoldoutButtonOnToggleEnabled(Foldouter.Index.BindingUISwitch, new GUIContent("Enable Binding Interface Switch", VIUSettings.BIND_UI_SWITCH_TOOLTIP), VIUSettings.enableBindingInterfaceSwitch);
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent("Enable Binding Interface Switch", "Role Binding only works with Standalone device."));
            }

            if (supportAnyStandaloneVR && VIUSettings.enableBindingInterfaceSwitch && Foldouter.IsExpended(Foldouter.Index.BindingUISwitch))
            {
                if (supportAnyStandaloneVR && VIUSettings.enableBindingInterfaceSwitch) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                {
                    EditorGUI.indentLevel += 2;

                    VIUSettings.bindingInterfaceSwitchKey = (KeyCode)EditorGUILayout.EnumPopup("Switch Key", VIUSettings.bindingInterfaceSwitchKey);
                    VIUSettings.bindingInterfaceSwitchKeyModifier = (KeyCode)EditorGUILayout.EnumPopup("Switch Key Modifier", VIUSettings.bindingInterfaceSwitchKeyModifier);

                    EditorGUI.indentLevel -= 2;
                }
                if (supportAnyStandaloneVR && VIUSettings.enableBindingInterfaceSwitch) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
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

                EditorUtility.SetDirty(VIUSettings.Instance);

                VIUVersionCheck.UpdateIgnoredNotifiedSettingsCount(false);
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Use Default Settings"))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    supportSimulator = canSupportSimulator;
                    supportOpenVR = canSupportOpenVR;
                    supportOculus = canSupportOculus;
                    supportDaydream = canSupportDaydream;

                    VRSDKSettings.ApplyChanges();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Repair Define Symbols", "Repair symbols that handled by VIU.")))
            {
                VRModuleManagerEditor.UpdateScriptingDefineSymbols();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private static bool ShowToggle(GUIContent label, bool value, params GUILayoutOption[] options)
        {
            var result = EditorGUILayout.ToggleLeft(label, value, s_labelStyle, options);
            if (result != value) { s_guiChanged = true; }
            return result;
        }

        private static void ShowSwitchPlatformButton(BuildTargetGroup group, BuildTarget target)
        {
            if (GUILayout.Button(new GUIContent("Switch Platform", "Switch platform to " + group), GUILayout.ExpandWidth(false)))
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

        private static void ShowCheckRecommendedSettingsButton()
        {
            if (VIUVersionCheck.notifiedSettingsCount <= 0) { return; }

            if (GUILayout.Button("View Recommended Settings", GUILayout.ExpandWidth(false)))
            {
                VIUVersionCheck.TryOpenRecommendedSettingWindow();
            }
        }

        private static void ShowUrlLinkButton(string url, string label = "Get Plugin")
        {
            if (GUILayout.Button(new GUIContent(label, url), GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL(url);
            }
        }

        private static void ShowCreateExCamCfgButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(50f);
            if (GUILayout.Button(new GUIContent("Generate Default Config File", "To get External Camera work in playmode, the config file must exits under project folder or build folder when start playing.")))
            {
                File.WriteAllText(VIUSettings.externalCameraConfigFilePath,
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