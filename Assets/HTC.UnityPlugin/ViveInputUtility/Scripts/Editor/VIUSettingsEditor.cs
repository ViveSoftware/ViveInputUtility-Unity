//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;


namespace HTC.UnityPlugin.Vive
{
    public static partial class VIUSettingsEditor
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
            public static readonly VRSDK MockHMD = new VRSDK("MockHMD");
            public static readonly VRSDK WindowsMR = new VRSDK("WindowsMR");

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
#if UNITY_2018_1_OR_NEWER
                        s_vrEnabled = value && (!PackageManagerHelper.IsPackageInList(OPENVR_XR_PACKAGE_NAME) || !PackageManagerHelper.IsPackageInList(OCULUS_XR_PACKAGE_NAME));
#else
                        s_vrEnabled = value;
#endif
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
                    MockHMD,
                    WindowsMR,
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

        public class Foldouter
        {
            private static bool s_initialized;
            private static GUIStyle s_styleFoleded;
            private static GUIStyle s_styleExpended;

            public bool isExpended { get; private set; }

            public static void Initialize()
            {
                if (s_initialized) { return; }
                s_initialized = true;

                s_styleFoleded = new GUIStyle(EditorStyles.foldout);
                s_styleExpended = new GUIStyle(EditorStyles.foldout);
                s_styleExpended.normal = s_styleFoleded.onNormal;
                s_styleExpended.active = s_styleFoleded.onActive;
            }

            public static void ShowFoldoutBlank()
            {
                GUILayout.Space(20f);
            }

            public void ShowFoldoutButton()
            {
                var style = isExpended ? s_styleExpended : s_styleFoleded;
                if (GUILayout.Button(string.Empty, style, GUILayout.Width(12f)))
                {
                    isExpended = !isExpended;
                }
            }

            public void ShowFoldoutWithLabel(GUIContent content)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutButton();
                if (GUILayout.Button(content, EditorStyles.label))
                {
                    isExpended = !isExpended;
                }
                //EditorGUILayout.LabelField(content);
                GUILayout.EndHorizontal();
            }

            public bool ShowFoldoutButtonOnToggleEnabled(GUIContent content, bool toggleValue)
            {
                GUILayout.BeginHorizontal();
                if (toggleValue)
                {
                    ShowFoldoutButton();
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

            public bool ShowFoldoutButtonWithEnabledToggle(GUIContent content, bool toggleValue)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutButton();
                var toggleResult = EditorGUILayout.ToggleLeft(content, toggleValue, s_labelStyle);
                if (toggleResult != toggleValue) { s_guiChanged = true; }
                GUILayout.EndHorizontal();
                return toggleResult;
            }

            public void ShowFoldoutButtonWithDisbledToggle(GUIContent content)
            {
                GUILayout.BeginHorizontal();
                ShowFoldoutButton();
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
        }

        public static class PackageManagerHelper
        {
#if UNITY_2018_1_OR_NEWER
            private static bool s_wasPreparing;
            private static bool m_wasAdded;
            private static bool s_wasRemoved;
            private static ListRequest m_listRequest;
            private static AddRequest m_addRequest;
            private static RemoveRequest m_removeRequest;
            private static string s_fallbackIdentifier;

            public static bool isPreparingList
            {
                get
                {
                    if (m_listRequest == null) { return s_wasPreparing = true; }

                    switch (m_listRequest.Status)
                    {
                        case StatusCode.InProgress:
                            return s_wasPreparing = true;
                        case StatusCode.Failure:
                            if (!s_wasPreparing)
                            {
                                Debug.LogError("Something wrong when adding package to list. error:" + m_listRequest.Error.errorCode + "(" + m_listRequest.Error.message + ")");
                            }
                            break;
                        case StatusCode.Success:
                            break;
                    }

                    return s_wasPreparing = false;
                }
            }

            public static bool isAddingToList
            {
                get
                {
                    if (m_addRequest == null) { return m_wasAdded = false; }

                    switch (m_addRequest.Status)
                    {
                        case StatusCode.InProgress:
                            return m_wasAdded = true;
                        case StatusCode.Failure:
                            if (!m_wasAdded)
                            {
                                AddRequest request = m_addRequest;
                                m_addRequest = null;
                                if (string.IsNullOrEmpty(s_fallbackIdentifier))
                                {
                                    Debug.LogError("Something wrong when adding package to list. error:" + request.Error.errorCode + "(" + request.Error.message + ")");
                                }
                                else
                                {
                                    Debug.Log("Failed to install package: \"" + request.Error.message + "\". Retry with fallback identifier \"" + s_fallbackIdentifier + "\"");
                                    AddToPackageList(s_fallbackIdentifier);
                                }

                                s_fallbackIdentifier = null;
                            }
                            break;
                        case StatusCode.Success:
                            if (!m_wasAdded)
                            {
                                m_addRequest = null;
                                s_fallbackIdentifier = null;
                                ResetPackageList();
                            }
                            break;
                    }

                    return m_wasAdded = false;
                }
            }

            public static bool isRemovingFromList
            {
                get
                {
                    if (m_removeRequest == null) { return s_wasRemoved = false; }

                    switch (m_removeRequest.Status)
                    {
                        case StatusCode.InProgress:
                            return s_wasRemoved = true;
                        case StatusCode.Failure:
                            if (!s_wasRemoved)
                            {
                                var request = m_removeRequest;
                                m_removeRequest = null;
                                Debug.LogError("Something wrong when removing package from list. error:" + m_removeRequest.Error.errorCode + "(" + m_removeRequest.Error.message + ")");
                            }
                            break;
                        case StatusCode.Success:
                            if (!s_wasRemoved)
                            {
                                m_removeRequest = null;
                                ResetPackageList();
                            }
                            break;
                    }

                    return s_wasRemoved = false;
                }
            }

            public static void PreparePackageList()
            {
                if (m_listRequest != null) { return; }
#if UNITY_2019_3_OR_NEWER
                m_listRequest = Client.List(true, true);
#else
                m_listRequest = Client.List(true);
#endif
            }

            public static void ResetPackageList()
            {
                s_wasPreparing = false;
                m_listRequest = null;
            }

            public static bool IsPackageInList(string name)
            {
                if (m_listRequest == null || m_listRequest.Result == null) return false;

                return m_listRequest.Result.Any(pkg => pkg.name == name);
            }

            public static void AddToPackageList(string identifier, string fallbackIdentifier = null)
            {
                Debug.Assert(m_addRequest == null);

                m_addRequest = Client.Add(identifier);
                s_fallbackIdentifier = fallbackIdentifier;
            }

            public static void RemovePackage(string identifier)
            {
                Debug.Assert(m_removeRequest == null);

                m_removeRequest = Client.Remove(identifier);
            }

            public static PackageCollection GetPackageList()
            {
                if (m_listRequest == null || m_listRequest.Result == null)
                {
                    return null;
                }

                return m_listRequest.Result;
            }
#else
            public static bool isPreparingList { get { return false; } }
            public static bool isAddingToList { get { return false; } }
            public static void PreparePackageList() { }
            public static void ResetPackageList() { }
            public static bool IsPackageInList(string name) { return false; }
            public static void AddToPackageList(string identifier, string fallbackIdentifier = null) { }
            public static void RemovePackage(string identifier) { }
#endif
        }

        private abstract class VRPlatformSetting
        {
            public bool isStandaloneVR { get { return requirdPlatform == BuildTargetGroup.Standalone; } }
            public bool isAndroidVR { get { return requirdPlatform == BuildTargetGroup.Android; } }
            public abstract bool canSupport { get; }
            public abstract bool support { get; set; }

            public abstract int order { get; }
            protected abstract BuildTargetGroup requirdPlatform { get; }

            public abstract void OnPreferenceGUI();
        }

        private static VRPlatformSetting[] s_platformSettings;

        public const string URL_VIU_GITHUB_RELEASE_PAGE = "https://github.com/ViveSoftware/ViveInputUtility-Unity/releases";
        public const string OPENXR_PLUGIN_PACKAGE_NAME = "com.unity.xr.openxr";
        public const string OPENXR_PLUGIN_LOADER_NAME = "Open XR Loader";
        public const string OPENXR_PLUGIN_LOADER_TYPE = "OpenXRLoader";

        private const string DEFAULT_ASSET_PATH = "Assets/VIUSettings/Resources/VIUSettings.asset";

        private static Vector2 s_scrollValue = Vector2.zero;
        private static float s_warningHeight;
        private static GUIStyle s_labelStyle;
        private static bool s_guiChanged;
        private static bool s_symbolChanged;
        private static string s_defaultAssetPath;
        private static string s_VIUPackageName = null;

        private static Foldouter s_autoBindFoldouter = new Foldouter();
        private static Foldouter s_bindingUIFoldouter = new Foldouter();
        private static Foldouter s_overrideModelFoldouter = new Foldouter();

        static VIUSettingsEditor()
        {
            var platformSettins = new List<VRPlatformSetting>();
            foreach (var type in Assembly.GetAssembly(typeof(VRPlatformSetting)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(VRPlatformSetting))))
            {
                platformSettins.Add((VRPlatformSetting)Activator.CreateInstance(type));
            }
            s_platformSettings = platformSettins.OrderBy(e => e.order).ToArray();
        }

        public static bool virtualRealitySupported { get { return VRSDKSettings.vrEnabled; } set { VRSDKSettings.vrEnabled = value; } }
        public static ISupportedSDK OpenVRSDK { get { return VRSDKSettings.OpenVR; } }
        public static ISupportedSDK OculusSDK { get { return VRSDKSettings.Oculus; } }
        public static ISupportedSDK DaydreamSDK { get { return VRSDKSettings.Daydream; } }
        public static ISupportedSDK MockHMDSDK { get { return VRSDKSettings.MockHMD; } }
        public static ISupportedSDK WindowsMRSDK { get { return VRSDKSettings.WindowsMR; } }
        public static void ApplySDKChanges() { VRSDKSettings.ApplyChanges(); }

        public static BuildTargetGroup activeBuildTargetGroup { get { return BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget); } }

        public static string defaultAssetPath
        {
            get
            {
                if (s_defaultAssetPath == null)
                {
                    s_defaultAssetPath = DEFAULT_ASSET_PATH;
                }

                return s_defaultAssetPath;
            }
        }

        public static bool supportAnyStandaloneVR
        {
            get
            {
                foreach (var ps in s_platformSettings)
                {
                    if (ps.support && ps.isStandaloneVR)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static bool supportAnyAndroidVR
        {
            get
            {
                foreach (var ps in s_platformSettings)
                {
                    if (ps.support && ps.isAndroidVR)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static bool supportAnyVR
        {
            get
            {
                foreach (var ps in s_platformSettings)
                {
                    if (ps.support)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static string VIUPackageName
        {
            get
            {
                if (s_VIUPackageName == null)
                {
                    MonoScript script = MonoScript.FromScriptableObject(VIUSettings.Instance);
                    string settingsPath = AssetDatabase.GetAssetPath(script);
                    Match match = Regex.Match(settingsPath, @"^Packages\/([^\/]+)\/");
                    if (match.Success)
                    {
                        s_VIUPackageName = match.Groups[1].Value;
                    }
                    else
                    {
                        s_VIUPackageName = "";
                    }
                }

                return s_VIUPackageName;
            }
        }

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

#pragma warning disable 0618
        [PreferenceItem("VIU Settings")]
#pragma warning restore 0618
        private static void OnVIUPreferenceGUI()
        {
#if UNITY_2017_1_OR_NEWER
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.LabelField("Compiling...");
                return;
            }
#endif
#if UNITY_2018_1_OR_NEWER
            if (PackageManagerHelper.isAddingToList)
            {
                EditorGUILayout.LabelField("Installing packages...");
                return;
            }
            if (PackageManagerHelper.isRemovingFromList)
            {
                EditorGUILayout.LabelField("Removing packages...");
                return;
            }
            PackageManagerHelper.PreparePackageList();
            if (PackageManagerHelper.isPreparingList)
            {
                EditorGUILayout.LabelField("Checking Packages...");
                return;
            }
#endif
            if (s_labelStyle == null)
            {
                s_labelStyle = new GUIStyle(EditorStyles.label);
                s_labelStyle.richText = true;
            }

            Foldouter.Initialize();

            s_guiChanged = false;
            s_symbolChanged = false;

            s_scrollValue = EditorGUILayout.BeginScrollView(s_scrollValue);

            EditorGUILayout.LabelField("<b>VIVE Input Utility v" + VIUVersion.current + "</b>", s_labelStyle);
            EditorGUI.BeginChangeCheck();
            VIUSettings.autoCheckNewVIUVersion = EditorGUILayout.ToggleLeft("Auto Check Latest Version", VIUSettings.autoCheckNewVIUVersion);
            s_guiChanged |= EditorGUI.EndChangeCheck();

            GUILayout.BeginHorizontal();
            ShowUrlLinkButton(URL_VIU_GITHUB_RELEASE_PAGE, "Get Latest Release");
            ShowCheckRecommendedSettingsButton();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("<b>Supporting Device</b>", s_labelStyle);

            GUILayout.Space(5);

            foreach (var ps in s_platformSettings)
            {
                ps.OnPreferenceGUI();
                GUILayout.Space(5f);
            }

            if (supportAnyAndroidVR)
            {
                EditorGUI.indentLevel += 2;

                // on Windows, following preferences is stored at HKEY_CURRENT_USER\Software\Unity Technologies\Unity Editor 5.x\
#if UNITY_2019_1_OR_NEWER
                if (!EditorPrefs.GetBool("SdkUseEmbedded") && string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot")))
#else
                if (string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot")))
#endif
                {
                    EditorGUILayout.HelpBox("AndroidSdkRoot is empty. Setup at Edit -> Preferences... -> External Tools -> Android SDK", MessageType.Warning);
                }
#if UNITY_2018_3_OR_NEWER
                if (!EditorPrefs.GetBool("JdkUseEmbedded") && string.IsNullOrEmpty(EditorPrefs.GetString("JdkPath")))
#else
                if (string.IsNullOrEmpty(EditorPrefs.GetString("JdkPath")))
#endif
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
                VIUSettings.autoLoadBindingConfigOnStart = s_autoBindFoldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent("Load Binding Config on Start"), VIUSettings.autoLoadBindingConfigOnStart);
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent("Load Binding Config on Start", "Role Binding only works on standalone device."));
            }

            if (supportAnyStandaloneVR && VIUSettings.autoLoadBindingConfigOnStart && s_autoBindFoldouter.isExpended)
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
                VIUSettings.enableBindingInterfaceSwitch = s_bindingUIFoldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent("Enable Binding Interface Switch", VIUSettings.BIND_UI_SWITCH_TOOLTIP), VIUSettings.enableBindingInterfaceSwitch);
            }
            else
            {
                Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent("Enable Binding Interface Switch", "Role Binding only works with Standalone device."));
            }

            if (supportAnyStandaloneVR && VIUSettings.enableBindingInterfaceSwitch && s_bindingUIFoldouter.isExpended)
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

            GUILayout.Space(5);

            EditorGUILayout.LabelField("<b>Other</b>", s_labelStyle);
            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel += 1;
            VRModuleSettings.initializeOnStartup = EditorGUILayout.ToggleLeft(new GUIContent("Initialize on Startup", VRModuleSettings.INITIALIZE_ON_STARTUP_TOOLTIP), VRModuleSettings.initializeOnStartup);
            EditorGUI.indentLevel -= 1;
            s_guiChanged |= EditorGUI.EndChangeCheck();

            s_overrideModelFoldouter.ShowFoldoutWithLabel(new GUIContent("Globel Custom Render Model", "Override model object created by RenderModelHook with custom render model"));
            if (s_overrideModelFoldouter.isExpended)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel += 1;
                foreach (var e in EnumArrayBase<VRModuleDeviceModel>.StaticEnums)
                {
                    EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(e.ToString()), VIUSettings.GetOverrideDeviceModel(e), typeof(GameObject), false);
                }
                EditorGUI.indentLevel -= 1;
                s_guiChanged |= EditorGUI.EndChangeCheck();
            }

            //Foldouter.ApplyChanges();
            ApplySDKChanges();

            var viuSettingsAssetPath = AssetDatabase.GetAssetPath(VIUSettings.Instance);
            var moduleSettingsAssetPath = AssetDatabase.GetAssetPath(VRModuleSettings.Instance);

            if (s_guiChanged)
            {
                if (string.IsNullOrEmpty(viuSettingsAssetPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(defaultAssetPath));
                    AssetDatabase.CreateAsset(VIUSettings.Instance, defaultAssetPath);
                }

                EditorUtility.SetDirty(VIUSettings.Instance);

                if (string.IsNullOrEmpty(moduleSettingsAssetPath))
                {
                    const string defaultModuleSettingsAssetPath = "Assets/VIUSettings/Resources/VRModuleSettings.asset";
                    Directory.CreateDirectory(Path.GetDirectoryName(defaultModuleSettingsAssetPath));
                    AssetDatabase.CreateAsset(VRModuleSettings.Instance, defaultModuleSettingsAssetPath);
                }

                EditorUtility.SetDirty(VRModuleSettings.Instance);

                VIUVersionCheck.UpdateIgnoredNotifiedSettingsCount(false);
            }

            if (!string.IsNullOrEmpty(viuSettingsAssetPath) || !string.IsNullOrEmpty(moduleSettingsAssetPath))
            {
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Use Default Settings"))
                {
                    AssetDatabase.DeleteAsset(viuSettingsAssetPath);
                    AssetDatabase.DeleteAsset(moduleSettingsAssetPath);
                    foreach (var ps in s_platformSettings)
                    {
                        if (ps.canSupport && !ps.support)
                        {
                            ps.support = true;
                            s_symbolChanged |= ps.support;
                        }
                    }

                    VRSDKSettings.ApplyChanges();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Repair Define Symbols", "Repair symbols that handled by VIU.")))
            {
                s_symbolChanged = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (s_symbolChanged)
            {
                VRModuleManagerEditor.UpdateScriptingDefineSymbols();
            }

#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
            if (false && GUILayout.Button("Create Partial Action Set", GUILayout.ExpandWidth(false)))
            {
                var actionFile = new SteamVRExtension.VIUSteamVRActionFile()
                {
                    dirPath = VIUProjectSettings.partialActionDirPath,
                    fileName = VIUProjectSettings.partialActionFileName,
                };

                actionFile.action_sets.Add(new SteamVRExtension.VIUSteamVRActionFile.ActionSet()
                {
                    name = SteamVRModule.ACTION_SET_NAME,
                    usage = "leftright",
                });

                actionFile.localization.Add(new SteamVRExtension.VIUSteamVRActionFile.Localization()
                {
                    { "language_tag", "en_US" },
                });

                SteamVRModule.InitializePaths();
                foreach (var rawBtn in EnumArrayBase<VRModuleRawButton>.StaticEnums)
                {
                    var pressPath = SteamVRModule.pressActions.ActionPaths[(int)rawBtn];
                    if (!string.IsNullOrEmpty(pressPath))
                    {
                        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
                        {
                            name = pressPath,
                            type = SteamVRModule.pressActions.DataTypeName,
                            requirement = "optional",
                        });
                        actionFile.localization[0].Add(pressPath, SteamVRModule.pressActions.ActionAlias[(int)rawBtn]);
                    }

                    var touchPath = SteamVRModule.touchActions.ActionPaths[(int)rawBtn];
                    if (!string.IsNullOrEmpty(touchPath))
                    {
                        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
                        {
                            name = touchPath,
                            type = SteamVRModule.touchActions.DataTypeName,
                            requirement = "optional",
                        });
                        actionFile.localization[0].Add(touchPath, SteamVRModule.touchActions.ActionAlias[(int)rawBtn]);
                    }
                }
                foreach (var rawAxis in EnumArrayBase<VRModuleRawButton>.StaticEnums)
                {
                    var v1Path = SteamVRModule.v1Actions.ActionPaths[(int)rawAxis];
                    if (!string.IsNullOrEmpty(v1Path))
                    {
                        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
                        {
                            name = v1Path,
                            type = SteamVRModule.v1Actions.DataTypeName,
                            requirement = "optional",
                        });
                        actionFile.localization[0].Add(v1Path, SteamVRModule.v1Actions.ActionAlias[(int)rawAxis]);
                    }

                    var v2Path = SteamVRModule.v2Actions.ActionPaths[(int)rawAxis];
                    if (!string.IsNullOrEmpty(v2Path))
                    {
                        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
                        {
                            name = v2Path,
                            type = SteamVRModule.v1Actions.DataTypeName,
                            requirement = "optional",
                        });
                        actionFile.localization[0].Add(v2Path, SteamVRModule.v2Actions.ActionAlias[(int)rawAxis]);
                    }
                }
                foreach (var haptic in EnumArrayBase<SteamVRModule.HapticStruct>.StaticEnums)
                {
                    var path = SteamVRModule.vibrateActions.ActionPaths[(int)haptic];
                    if (!string.IsNullOrEmpty(path))
                    {
                        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
                        {
                            name = path,
                            type = SteamVRModule.vibrateActions.DataTypeName,
                            requirement = "optional",
                        });
                        actionFile.localization[0].Add(path, SteamVRModule.vibrateActions.ActionAlias[(int)haptic]);
                    }
                }

                actionFile.Save();
            }
#endif

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

        private static void ShowAddPackageButton(string displayName, string identifier, string fallbackIdentifier = null)
        {
            if (GUILayout.Button(new GUIContent("Add " + displayName + " Package", "Add " + identifier + " to Package Manager"), GUILayout.ExpandWidth(false)))
            {
                PackageManagerHelper.AddToPackageList(identifier, fallbackIdentifier);
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