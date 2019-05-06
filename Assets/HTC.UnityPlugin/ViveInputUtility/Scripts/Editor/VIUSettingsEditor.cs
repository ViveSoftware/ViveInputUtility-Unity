//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

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
            private static ListRequest m_listRequest;
            private static AddRequest m_addRequest;

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
                                Debug.LogError("Somthing wrong when adding package to list. error:" + m_addRequest.Error.errorCode + "(" + m_addRequest.Error.message + ")");
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
                                Debug.LogError("Somthing wrong when adding package to list. error:" + m_addRequest.Error.errorCode + "(" + m_addRequest.Error.message + ")");
                            }
                            break;
                        case StatusCode.Success:
                            if (!m_wasAdded)
                            {
                                m_addRequest = null;
                                ResetPackageList();
                            }
                            break;
                    }

                    return m_wasAdded = false;
                }
            }

            public static void PreparePackageList()
            {
                if (m_listRequest != null) { return; }
                m_listRequest = Client.List(true);
            }

            public static void ResetPackageList()
            {
                s_wasPreparing = false;
                m_listRequest = null;
            }

            public static bool IsPackageInList(string name)
            {
                Debug.Assert(m_listRequest != null);
                return m_listRequest.Result.Any(pkg => pkg.name == name);
            }

            public static void AddToPackageList(string name)
            {
                Debug.Assert(m_addRequest != null);
                m_addRequest = Client.Add(name);
            }
#else
            public static bool isPreparingList { get { return false; } }
            public static bool isAddingToList { get { return false; } }
            public static void PreparePackageList() { }
            public static void ResetPackageList() { }
            public static bool IsPackageInList(string name) { return true; }
            public static void AddToPackageList(string name) { }
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

        private static Foldouter s_autoBindFoldouter = new Foldouter();
        private static Foldouter s_bindingUIFoldouter = new Foldouter();

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
                    if (ps.support && (ps.isAndroidVR || ps.isAndroidVR))
                    {
                        return true;
                    }
                }
                return false;
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

        [PreferenceItem("VIU Settings")]
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
                EditorGUILayout.LabelField("Installing Packages...");
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

            //if (GUILayout.Button("Create Partial Action Set", GUILayout.ExpandWidth(false)))
            //{
            //    var actionFile = new SteamVRExtension.VIUSteamVRActionFile()
            //    {
            //        dirPath = VIUProjectSettings.partialActionDirPath,
            //        fileName = VIUProjectSettings.partialActionFileName,
            //    };

            //    actionFile.action_sets.Add(new SteamVRExtension.VIUSteamVRActionFile.ActionSet()
            //    {
            //        name = SteamVRModule.ACTION_SET_NAME,
            //        usage = "leftright",
            //    });

            //    actionFile.localization.Add(new SteamVRExtension.VIUSteamVRActionFile.Localization()
            //    {
            //        { "language_tag", "en_US" },
            //    });

            //    SteamVRModule.InitializePaths();
            //    for (SteamVRModule.pressActions.Reset(); SteamVRModule.pressActions.IsCurrentValid(); SteamVRModule.pressActions.MoveNext())
            //    {
            //        if (string.IsNullOrEmpty(SteamVRModule.pressActions.CurrentPath)) { continue; }
            //        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
            //        {
            //            name = SteamVRModule.pressActions.CurrentPath,
            //            type = SteamVRModule.pressActions.DataType,
            //            requirement = "optional",
            //        });
            //        actionFile.localization[0].Add(SteamVRModule.pressActions.CurrentPath, SteamVRModule.pressActions.CurrentAlias);
            //    }
            //    for (SteamVRModule.touchActions.Reset(); SteamVRModule.touchActions.IsCurrentValid(); SteamVRModule.touchActions.MoveNext())
            //    {
            //        if (string.IsNullOrEmpty(SteamVRModule.touchActions.CurrentPath)) { continue; }
            //        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
            //        {
            //            name = SteamVRModule.touchActions.CurrentPath,
            //            type = SteamVRModule.touchActions.DataType,
            //            requirement = "optional",
            //        });
            //        actionFile.localization[0].Add(SteamVRModule.touchActions.CurrentPath, SteamVRModule.touchActions.CurrentAlias);
            //    }
            //    for (SteamVRModule.v1Actions.Reset(); SteamVRModule.v1Actions.IsCurrentValid(); SteamVRModule.v1Actions.MoveNext())
            //    {
            //        if (string.IsNullOrEmpty(SteamVRModule.v1Actions.CurrentPath)) { continue; }
            //        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
            //        {
            //            name = SteamVRModule.v1Actions.CurrentPath,
            //            type = SteamVRModule.v1Actions.DataType,
            //            requirement = "optional",
            //        });
            //        actionFile.localization[0].Add(SteamVRModule.v1Actions.CurrentPath, SteamVRModule.v1Actions.CurrentAlias);
            //    }
            //    for (SteamVRModule.v2Actions.Reset(); SteamVRModule.v2Actions.IsCurrentValid(); SteamVRModule.v2Actions.MoveNext())
            //    {
            //        if (string.IsNullOrEmpty(SteamVRModule.v2Actions.CurrentPath)) { continue; }
            //        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
            //        {
            //            name = SteamVRModule.v2Actions.CurrentPath,
            //            type = SteamVRModule.v2Actions.DataType,
            //            requirement = "optional",
            //        });
            //        actionFile.localization[0].Add(SteamVRModule.v2Actions.CurrentPath, SteamVRModule.v2Actions.CurrentAlias);
            //    }
            //    for (SteamVRModule.vibrationActions.Reset(); SteamVRModule.vibrationActions.IsCurrentValid(); SteamVRModule.vibrationActions.MoveNext())
            //    {
            //        if (string.IsNullOrEmpty(SteamVRModule.vibrationActions.CurrentPath)) { continue; }
            //        actionFile.actions.Add(new SteamVRExtension.VIUSteamVRActionFile.Action()
            //        {
            //            name = SteamVRModule.vibrationActions.CurrentPath,
            //            type = SteamVRModule.vibrationActions.DataType,
            //            requirement = "optional",
            //        });
            //        actionFile.localization[0].Add(SteamVRModule.vibrationActions.CurrentPath, SteamVRModule.vibrationActions.CurrentAlias);
            //    }

            //    actionFile.Save();
            //}

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

        private static void ShowAddPackageButton(string displayName, string pkgName)
        {
            if (GUILayout.Button(new GUIContent("Add " + displayName + " Package", "Add " + pkgName + " to Package Manager"), GUILayout.ExpandWidth(false)))
            {
                PackageManagerHelper.AddToPackageList(pkgName);
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