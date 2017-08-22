//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [InitializeOnLoad]
    public class VIUVersionCheck : EditorWindow
    {
        [Serializable]
        private struct RepoInfo
        {
            public string tag_name;
            public string body;
        }

        public const string VIU_AUTO_BINDING_INTERFACE_SYMBOL = "VIU_AUTO_BINDING_INTERFACE";

        public const string lastestVersionUrl = "https://api.github.com/repos/ViveSoftware/ViveInputUtility-Unity/releases/latest";
        public const string pluginUrl = "https://github.com/ViveSoftware/ViveInputUtility-Unity/releases";
        public const double versionCheckIntervalMinutes = 60.0;

        public readonly static string nextVersionCheckTimeKey = "ViveInputUtility." + PlayerSettings.productGUID + ".LastVersionCheckTime";
        public readonly static string doNotShowUpdateKey = "ViveInputUtility." + PlayerSettings.productGUID + ".DoNotShowUpdate.v{0}";
        public readonly static string doNotShowSetupAutoBindUIKey = "ViveInputUtility." + PlayerSettings.productGUID + ".DoNotShowSetupAutoBindUI";

        private readonly static string s_enableAutoBindUIInfo = "This project will enable auto binding interface! Press RightShift + B to open the binding interface in play mode.";
        private readonly static string s_disableAutoBindUIInfo = "This project will NOT enable auto binding interface! Please enable it manually by calling ViveRoleBindingsHelper.EnableBindingInterface() in script, or copy \"ViveInputUtility/Scripts/ViveRole/BindingInterface/BindingConfigSample/vive_role_bindings.cfg\" file into project folder before you can press RightShift + B to open the binding interface in play mode.";
        private static bool s_waitingForCompile;

        private static bool completeCheckVersionFlow = false;
        private static WWW www;
        private static RepoInfo latestRepoInfo;
        private static Version latestVersion;
        private static VIUVersionCheck window;

        private static bool showNewVersionInfo = false;
        private static bool showBindHotKeySetup = false;

        static VIUVersionCheck()
        {
            EditorApplication.update += CheckVersion;
            s_waitingForCompile = false;
            EditorApplication.RepaintProjectWindow();
        }

        // check vive input utility version on github
        private static void CheckVersion()
        {
            if (!completeCheckVersionFlow)
            {
                if (www == null) // web request not running
                {
                    if (EditorPrefs.HasKey(nextVersionCheckTimeKey) && DateTime.UtcNow < UtcDateTimeFromStr(EditorPrefs.GetString(nextVersionCheckTimeKey)))
                    {
                        completeCheckVersionFlow = true;
                        return;
                    }

                    www = new WWW(lastestVersionUrl);
                }

                if (!www.isDone)
                {
                    return;
                }

                if (UrlSuccess(www))
                {
                    EditorPrefs.SetString(nextVersionCheckTimeKey, UtcDateTimeToStr(DateTime.UtcNow.AddMinutes(versionCheckIntervalMinutes)));

                    latestRepoInfo = JsonUtility.FromJson<RepoInfo>(www.text);
                }

                showNewVersionInfo = ShouldDisplayNewUpdate();

                www.Dispose();
                www = null;

                completeCheckVersionFlow = true;
            }

            showBindHotKeySetup = showNewVersionInfo || !EditorPrefs.HasKey(doNotShowSetupAutoBindUIKey);

            if (showNewVersionInfo || showBindHotKeySetup)
            {
                window = GetWindow<VIUVersionCheck>(true, "Vive Input Utility");
                window.minSize = new Vector2(320, 440);
            }

            EditorApplication.update -= CheckVersion;
        }

        private static DateTime UtcDateTimeFromStr(string str)
        {
            var utcTicks = default(long);
            if (string.IsNullOrEmpty(str) || !long.TryParse(str, out utcTicks)) { return DateTime.MinValue; }
            return new DateTime(utcTicks, DateTimeKind.Utc);
        }

        private static string UtcDateTimeToStr(DateTime utcDateTime)
        {
            return utcDateTime.Ticks.ToString();
        }

        private static bool UrlSuccess(WWW www)
        {
            if (!string.IsNullOrEmpty(www.error))
            {
                // API rate limit exceeded, see https://developer.github.com/v3/#rate-limiting
                Debug.Log("url:" + www.url);
                Debug.Log("error:" + www.error);
                Debug.Log(www.text);
                return false;
            }

            if (Regex.IsMatch(www.text, "404 not found", RegexOptions.IgnoreCase))
            {
                Debug.Log("url:" + www.url);
                Debug.Log("error:" + www.error);
                Debug.Log(www.text);
                return false;
            }

            return true;
        }

        private static bool ShouldDisplayNewUpdate()
        {
            if (string.IsNullOrEmpty(latestRepoInfo.tag_name)) { return false; }

            try
            {
                latestVersion = new Version(Regex.Replace(latestRepoInfo.tag_name, "[^0-9\\.]", string.Empty));
            }
            catch
            {
                latestVersion = default(Version);
                return false;
            }

            if (latestVersion <= VIUVersion.current) { return false; }

            if (EditorPrefs.HasKey(string.Format(doNotShowUpdateKey, latestVersion.ToString()))) { return false; }

            return true;
        }

        private Vector2 scrollPosition;
        private bool toggleDoNotShowState = false;
        private bool toggleAutoBindState = true;

        public void OnGUI()
        {
            if (showBindHotKeySetup)
            {
                if (toggleAutoBindState)
                {
                    EditorGUILayout.HelpBox(s_enableAutoBindUIInfo, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox(s_disableAutoBindUIInfo, MessageType.Warning);
                }

                toggleAutoBindState = GUILayout.Toggle(toggleAutoBindState, "Enable Auto Binding Interface");
            }

            if (showNewVersionInfo)
            {
                EditorGUILayout.HelpBox("A new version of the Vive Input Utility is available!", MessageType.Warning);

                GUILayout.Label("Current version: " + VIUVersion.current);
                GUILayout.Label("New version: " + latestVersion);

                if (!string.IsNullOrEmpty(latestRepoInfo.body))
                {
                    GUILayout.Label("Release notes:");
                    EditorGUILayout.HelpBox(latestRepoInfo.body, MessageType.Info);
                }

                if (GUILayout.Button("Get Latest Version"))
                {
                    Application.OpenURL(pluginUrl);
                }

                EditorGUI.BeginChangeCheck();
                var doNotShow = GUILayout.Toggle(toggleDoNotShowState, "Do not prompt for this version again.");
                if (EditorGUI.EndChangeCheck())
                {
                    toggleDoNotShowState = doNotShow;
                    var key = string.Format(doNotShowUpdateKey, latestVersion);
                    if (doNotShow)
                    {
                        EditorPrefs.SetBool(key, true);
                    }
                    else
                    {
                        EditorPrefs.DeleteKey(key);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (showBindHotKeySetup)
            {
                EditorPrefs.SetBool(doNotShowSetupAutoBindUIKey, true);

                SetAutoBindingInterfaceSymbol(toggleAutoBindState);
            }
        }

        private static void SetAutoBindingInterfaceSymbol(bool enable)
        {
            var symbolChanged = false;
            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var symbolsList = new List<string>(scriptingDefineSymbols.Split(';'));

            if (enable)
            {
                if (!symbolsList.Contains(VIU_AUTO_BINDING_INTERFACE_SYMBOL))
                {
                    symbolsList.Add(VIU_AUTO_BINDING_INTERFACE_SYMBOL);
                    symbolChanged = true;
                }
            }
            else
            {
                if (symbolsList.RemoveAll(s => s == VIU_AUTO_BINDING_INTERFACE_SYMBOL) > 0)
                {
                    symbolChanged = true;
                }
            }

            if (symbolChanged)
            {
                EditorApplication.delayCall += GetSetSymbolsCallback(string.Join(";", symbolsList.ToArray()));
            }
        }

        private static EditorApplication.CallbackFunction GetSetSymbolsCallback(string symbols)
        {
            return () => PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
        }

        [PreferenceItem("Vive Input Utility")]
        public static void OnVIUPreferenceGUI()
        {
            if (!s_waitingForCompile)
            {
                var toggleValue = false;
                EditorGUI.BeginChangeCheck();

#if VIU_AUTO_BINDING_INTERFACE
                toggleValue = EditorGUILayout.Toggle("Enable Auto Binding Interface", true);
                EditorGUILayout.HelpBox(s_enableAutoBindUIInfo, MessageType.Info);
#else
                toggleValue = EditorGUILayout.Toggle("Enable Auto Binding Interface", false);
                EditorGUILayout.HelpBox(s_disableAutoBindUIInfo, MessageType.Info);
#endif

                if (EditorGUI.EndChangeCheck())
                {
                    s_waitingForCompile = true;
                    SetAutoBindingInterfaceSymbol(toggleValue);
                }
            }
            else
            {
                GUILayout.Space(30f);
                GUILayout.Button("Re-compiling...");
            }
        }
    }
}