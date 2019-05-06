//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Reflection;

#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityWebRequest = UnityEngine.WWW;
#endif

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

        public interface IPropSetting
        {
            bool SkipCheck();
            void UpdateCurrentValue();
            bool IsIgnored();
            bool IsUsingRecommendedValue();
            void DoDrawRecommend();
            void AcceptRecommendValue();
            void DoIgnore();
            void DeleteIgnore();
        }

        public class RecommendedSetting<T> : IPropSetting
        {
            private const string fmtTitle = "{0} (current = {1})";
            private const string fmtRecommendBtn = "Use recommended ({0})";
            private const string fmtRecommendBtnWithPosefix = "Use recommended ({0}) - {1}";

            private string m_settingTitle;
            private string m_settingTrimedTitle;
            private string ignoreKey { get { return m_settingTrimedTitle; } }

            public string settingTitle { get { return m_settingTitle; } set { m_settingTitle = value; m_settingTrimedTitle = value.Replace(" ", ""); } }
            public string recommendBtnPostfix = string.Empty;
            public string toolTip = string.Empty;
            public Func<bool> skipCheckFunc = null;
            public Func<T> recommendedValueFunc = null;
            public Func<T> currentValueFunc = null;
            public Action<T> setValueFunc = null;
            public T currentValue = default(T);
            public T recommendedValue = default(T);

            public T GetRecommended() { return recommendedValueFunc == null ? recommendedValue : recommendedValueFunc(); }

            public bool SkipCheck() { return skipCheckFunc == null ? false : skipCheckFunc(); }

            public bool IsIgnored() { return VIUProjectSettings.HasIgnoreKey(ignoreKey); }

            public bool IsUsingRecommendedValue() { return EqualityComparer<T>.Default.Equals(currentValue, GetRecommended()); }

            public void UpdateCurrentValue() { currentValue = currentValueFunc(); }

            public void DoDrawRecommend()
            {
                GUILayout.Label(new GUIContent(string.Format(fmtTitle, settingTitle, currentValue), toolTip));

                GUILayout.BeginHorizontal();

                bool recommendBtnClicked;
                if (string.IsNullOrEmpty(recommendBtnPostfix))
                {
                    recommendBtnClicked = GUILayout.Button(new GUIContent(string.Format(fmtRecommendBtn, GetRecommended()), toolTip));
                }
                else
                {
                    recommendBtnClicked = GUILayout.Button(new GUIContent(string.Format(fmtRecommendBtnWithPosefix, GetRecommended(), recommendBtnPostfix), toolTip));
                }

                if (recommendBtnClicked)
                {
                    AcceptRecommendValue();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("Ignore", toolTip)))
                {
                    DoIgnore();
                }

                GUILayout.EndHorizontal();
            }

            public void AcceptRecommendValue()
            {
                setValueFunc(GetRecommended());
            }

            public void DoIgnore()
            {
                VIUProjectSettings.AddIgnoreKey(ignoreKey);
            }

            public void DeleteIgnore()
            {
                VIUProjectSettings.RemoveIgnoreKey(ignoreKey);
            }
        }

        public abstract class RecommendedSettingCollection : List<IPropSetting> { }

        public const string lastestVersionUrl = "https://api.github.com/repos/ViveSoftware/ViveInputUtility-Unity/releases/latest";
        public const string pluginUrl = "https://github.com/ViveSoftware/ViveInputUtility-Unity/releases";
        public const double versionCheckIntervalMinutes = 30.0;

        private const string nextVersionCheckTimeKey = "ViveInputUtility.LastVersionCheckTime";
        private const string fmtIgnoreUpdateKey = "DoNotShowUpdate.v{0}";
        private static string ignoreThisVersionKey;

        private static bool completeCheckVersionFlow = false;
        private static UnityWebRequest webReq;
        private static RepoInfo latestRepoInfo;
        private static System.Version latestVersion;
        private static Vector2 releaseNoteScrollPosition;
        private static Vector2 settingScrollPosition;
        private static bool showNewVersion;
        private static bool toggleSkipThisVersion = false;
        private static VIUVersionCheck windowInstance;
        private static List<IPropSetting> s_settings;
        private Texture2D viuLogo;

        /// <summary>
        /// Count of settings that are ignored
        /// </summary>
        public static int ignoredSettingsCount { get; private set; }
        /// <summary>
        /// Count of settings that are not using recommended value
        /// </summary>
        public static int shouldNotifiedSettingsCount { get; private set; }
        /// <summary>
        /// Count of settings that are not ignored and not using recommended value
        /// </summary>
        public static int notifiedSettingsCount { get; private set; }

        public static bool recommendedWindowOpened { get { return windowInstance != null; } }

        static VIUVersionCheck()
        {
            EditorApplication.update += CheckVersionAndSettings;
        }

        public static void AddRecommendedSetting<T>(RecommendedSetting<T> setting)
        {
            InitializeSettins();
            s_settings.Add(setting);
        }

        private static void InitializeSettins()
        {
            if (s_settings != null) { return; }

            s_settings = new List<IPropSetting>();

            foreach (var type in Assembly.GetAssembly(typeof(RecommendedSettingCollection)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(RecommendedSettingCollection))))
            {
                s_settings.AddRange((RecommendedSettingCollection)Activator.CreateInstance(type));
            }
        }

        private static void VersionCheckLog(string msg)
        {
#if VIU_PRINT_FETCH_VERSION_LOG
            using (var outputFile = new StreamWriter("VIUVersionCheck.log", true))
            {
                outputFile.WriteLine(DateTime.Now.ToString() + " - " + msg + ". Stop fetching until " + UtcDateTimeFromStr(EditorPrefs.GetString(nextVersionCheckTimeKey)).ToLocalTime().ToString());
            }
#endif
        }

        // check vive input utility version on github
        private static void CheckVersionAndSettings()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= CheckVersionAndSettings;
                return;
            }

            InitializeSettins();

            // fetch new version info from github release site
            if (!completeCheckVersionFlow && VIUSettings.autoCheckNewVIUVersion)
            {
                if (webReq == null) // web request not running
                {
                    if (EditorPrefs.HasKey(nextVersionCheckTimeKey) && DateTime.UtcNow < UtcDateTimeFromStr(EditorPrefs.GetString(nextVersionCheckTimeKey)))
                    {
                        VersionCheckLog("Skipped");
                        completeCheckVersionFlow = true;
                        return;
                    }

                    webReq = new UnityWebRequest(lastestVersionUrl);
                }

                if (!webReq.isDone)
                {
                    return;
                }

                // On Windows, PlaterSetting is stored at \HKEY_CURRENT_USER\Software\Unity Technologies\Unity Editor 5.x
                EditorPrefs.SetString(nextVersionCheckTimeKey, UtcDateTimeToStr(DateTime.UtcNow.AddMinutes(versionCheckIntervalMinutes)));

                if (UrlSuccess(webReq))
                {
                    latestRepoInfo = JsonUtility.FromJson<RepoInfo>(GetWebText(webReq));
                    VersionCheckLog("Fetched");
                }

                // parse latestVersion and ignoreThisVersionKey
                if (!string.IsNullOrEmpty(latestRepoInfo.tag_name))
                {
                    try
                    {
                        latestVersion = new System.Version(Regex.Replace(latestRepoInfo.tag_name, "[^0-9\\.]", string.Empty));
                        ignoreThisVersionKey = string.Format(fmtIgnoreUpdateKey, latestVersion.ToString());
                    }
                    catch
                    {
                        latestVersion = default(System.Version);
                        ignoreThisVersionKey = string.Empty;
                    }
                }

                webReq.Dispose();
                webReq = null;

                completeCheckVersionFlow = true;
            }

            VIUSettingsEditor.PackageManagerHelper.PreparePackageList();
            if (VIUSettingsEditor.PackageManagerHelper.isPreparingList) { return; }

            showNewVersion = !string.IsNullOrEmpty(ignoreThisVersionKey) && !VIUProjectSettings.HasIgnoreKey(ignoreThisVersionKey) && latestVersion > VIUVersion.current;

            UpdateIgnoredNotifiedSettingsCount(false);

            if (showNewVersion || notifiedSettingsCount > 0)
            {
                TryOpenRecommendedSettingWindow();
            }

            EditorApplication.update -= CheckVersionAndSettings;
        }

        public static void UpdateIgnoredNotifiedSettingsCount(bool drawNotifiedPrompt)
        {
            InitializeSettins();

            ignoredSettingsCount = 0;
            shouldNotifiedSettingsCount = 0;
            notifiedSettingsCount = 0;

            foreach (var setting in s_settings)
            {
                if (setting.SkipCheck()) { continue; }

                setting.UpdateCurrentValue();

                var isIgnored = setting.IsIgnored();
                if (isIgnored) { ++ignoredSettingsCount; }

                if (setting.IsUsingRecommendedValue()) { continue; }
                else { ++shouldNotifiedSettingsCount; }

                if (!isIgnored)
                {
                    ++notifiedSettingsCount;

                    if (drawNotifiedPrompt)
                    {
                        if (notifiedSettingsCount == 1)
                        {
                            EditorGUILayout.HelpBox("Recommended project settings:", MessageType.Warning);

                            settingScrollPosition = GUILayout.BeginScrollView(settingScrollPosition, GUILayout.ExpandHeight(true));
                        }

                        setting.DoDrawRecommend();
                    }

                }
            }
        }

        // Open recommended setting window (with possible new version prompt)
        // won't do any thing if the window is already opened
        public static void TryOpenRecommendedSettingWindow()
        {
            if (recommendedWindowOpened) { return; }

            windowInstance = GetWindow<VIUVersionCheck>(true, "Vive Input Utility");
            windowInstance.minSize = new Vector2(240f, 550f);
            var rect = windowInstance.position;
            windowInstance.position = new Rect(Mathf.Max(rect.x, 50f), Mathf.Max(rect.y, 50f), rect.width, 200f + (showNewVersion ? 700f : 400f));
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

        private static string GetWebText(UnityWebRequest wr)
        {
#if UNITY_5_4_OR_NEWER
            return wr.downloadHandler.text;
#else
            return wr.text;
#endif
        }

        private static bool TryGetWebHeaderValue(UnityWebRequest wr, string headerKey, out string headerValue)
        {
#if UNITY_5_4_OR_NEWER
            headerValue = wr.GetResponseHeader(headerKey);
            return string.IsNullOrEmpty(headerValue);
#else
            if (wr.responseHeaders == null) { headerValue = string.Empty; return false; }
            return wr.responseHeaders.TryGetValue(headerKey, out headerValue);
#endif
        }

        private static bool UrlSuccess(UnityWebRequest wr)
        {
            try
            {
                if (!string.IsNullOrEmpty(wr.error))
                {
                    // API rate limit exceeded, see https://developer.github.com/v3/#rate-limiting
                    Debug.Log("url:" + wr.url);
                    Debug.Log("error:" + wr.error);
                    Debug.Log(GetWebText(wr));

                    string responseHeader;
                    if (TryGetWebHeaderValue(wr, "X-RateLimit-Limit", out responseHeader))
                    {
                        Debug.Log("X-RateLimit-Limit:" + responseHeader);
                    }
                    if (TryGetWebHeaderValue(wr, "X-RateLimit-Remaining", out responseHeader))
                    {
                        Debug.Log("X-RateLimit-Remaining:" + responseHeader);
                    }
                    if (TryGetWebHeaderValue(wr, "X-RateLimit-Reset", out responseHeader))
                    {
                        Debug.Log("X-RateLimit-Reset:" + TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(double.Parse(responseHeader))).ToString());
                    }
                    VersionCheckLog("Failed. Rate limit exceeded");
                    return false;
                }

                if (Regex.IsMatch(GetWebText(wr), "404 not found", RegexOptions.IgnoreCase))
                {
                    Debug.Log("url:" + wr.url);
                    Debug.Log("error:" + wr.error);
                    Debug.Log(GetWebText(wr));
                    VersionCheckLog("Failed. 404 not found");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                VersionCheckLog("Failed. " + e.ToString());
                return false;
            }

            return true;
        }

        private string GetResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Scripts/Editor".Length) + "Textures/";
        }

        public void OnGUI()
        {
#if UNITY_2017_1_OR_NEWER
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.LabelField("Compiling...");
                return;
            }
#endif
            if (viuLogo == null)
            {
                var currentDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
                var texturePath = currentDir.Substring(0, currentDir.Length - "Scripts/Editor".Length) + "Textures/VIU_logo.png";
                viuLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }

            if (viuLogo != null)
            {
                GUI.DrawTexture(GUILayoutUtility.GetRect(position.width, 124, GUI.skin.box), viuLogo, ScaleMode.ScaleToFit);
            }

            if (showNewVersion)
            {
                EditorGUILayout.HelpBox("New version available:", MessageType.Warning);

                GUILayout.Label("Current version: " + VIUVersion.current);
                GUILayout.Label("New version: " + latestVersion);

                if (!string.IsNullOrEmpty(latestRepoInfo.body))
                {
                    GUILayout.Label("Release notes:");
                    releaseNoteScrollPosition = GUILayout.BeginScrollView(releaseNoteScrollPosition, GUILayout.Height(250f));
                    EditorGUILayout.HelpBox(latestRepoInfo.body, MessageType.None);
                    GUILayout.EndScrollView();
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(new GUIContent("Get Latest Version", "Goto " + pluginUrl)))
                    {
                        Application.OpenURL(pluginUrl);
                    }

                    GUILayout.FlexibleSpace();

                    toggleSkipThisVersion = GUILayout.Toggle(toggleSkipThisVersion, "Do not prompt for this version again.");
                }
                GUILayout.EndHorizontal();
            }

            UpdateIgnoredNotifiedSettingsCount(true);

            if (notifiedSettingsCount > 0)
            {
                GUILayout.EndScrollView();

                if (ignoredSettingsCount > 0)
                {
                    if (GUILayout.Button("Clear All Ignores(" + ignoredSettingsCount + ")"))
                    {
                        foreach (var setting in s_settings) { setting.DeleteIgnore(); }
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Accept All(" + notifiedSettingsCount + ")"))
                    {
                        for (int i = 10; i >= 0 && notifiedSettingsCount > 0; --i)
                        {
                            foreach (var setting in s_settings) { if (!setting.SkipCheck() && !setting.IsIgnored()) { setting.AcceptRecommendValue(); } }

                            VIUSettingsEditor.ApplySDKChanges();

                            UpdateIgnoredNotifiedSettingsCount(false);
                        }
                    }

                    if (GUILayout.Button("Ignore All(" + notifiedSettingsCount + ")"))
                    {
                        foreach (var setting in s_settings) { if (!setting.SkipCheck() && !setting.IsIgnored() && !setting.IsUsingRecommendedValue()) { setting.DoIgnore(); } }
                    }
                }
                GUILayout.EndHorizontal();
            }
            else if (shouldNotifiedSettingsCount > 0)
            {
                EditorGUILayout.HelpBox("Some recommended settings ignored.", MessageType.Warning);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear All Ignores(" + ignoredSettingsCount + ")"))
                {
                    foreach (var setting in s_settings) { setting.DeleteIgnore(); }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All recommended settings applied.", MessageType.Info);

                GUILayout.FlexibleSpace();
            }

            VIUSettingsEditor.ApplySDKChanges();

            if (VIUProjectSettings.hasChanged)
            {
                // save ignore keys
                VIUProjectSettings.Save();
            }

            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            if (viuLogo != null)
            {
                viuLogo = null;
            }

            if (showNewVersion && toggleSkipThisVersion && !string.IsNullOrEmpty(ignoreThisVersionKey))
            {
                showNewVersion = false;
                VIUProjectSettings.AddIgnoreKey(ignoreThisVersionKey);
                VIUProjectSettings.Save();
            }

            if (windowInstance == this)
            {
                windowInstance = null;
            }
        }
    }
}