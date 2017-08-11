//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
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

        public const string nextVersionCheckTimeKey = "ViveInputUtility.lastVersionCheckTime";

        private const string lastestVersionUrl = "https://api.github.com/repos/ViveSoftware/ViveInputUtility-Unity/releases/latest";
        private const string pluginUrl = "https://github.com/ViveSoftware/ViveInputUtility-Unity/releases";
        private const string doNotShowKey = "ViveInputUtility.DoNotShowUpdate.v{0}";
        private const double versionCheckIntervalMinutes = 60.0;

        private static bool gotVersion = false;
        private static WWW www;
        private static RepoInfo latestRepoInfo;
        private static Version latestVersion;
        private static VIUVersionCheck window;

        static VIUVersionCheck()
        {
            EditorApplication.update += CheckVersion;
        }

        // check vive input utility version on github
        private static void CheckVersion()
        {
            if (EditorPrefs.HasKey(nextVersionCheckTimeKey) && DateTime.UtcNow < UtcDateTimeFromStr(EditorPrefs.GetString(nextVersionCheckTimeKey)))
            {
                // cool down not ready, skip version check
                //Debug.Log("Now: " + DateTime.UtcNow);
                //Debug.Log("Next checking time: " + UtcDateTimeFromStr(EditorPrefs.GetString(nextVersionCheckTimeKey)));
            }
            else if (!gotVersion)
            {
                if (www == null)
                {
                    www = new WWW(lastestVersionUrl);
                }

                if (!www.isDone) { return; }

                if (UrlSuccess(www))
                {
                    EditorPrefs.SetString(nextVersionCheckTimeKey, UtcDateTimeToStr(DateTime.UtcNow.AddMinutes(versionCheckIntervalMinutes)));

                    latestRepoInfo = JsonUtility.FromJson<RepoInfo>(www.text);
                }

                gotVersion = true;

                www.Dispose();
                www = null;

                if (ShouldDisplay())
                {
                    window = GetWindow<VIUVersionCheck>(true, "Vive Input Utility");
                    window.minSize = new Vector2(320, 440);
                }
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

        private static bool ShouldDisplay()
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

            if (EditorPrefs.HasKey(string.Format(doNotShowKey, latestVersion.ToString()))) { return false; }

            return true;
        }

        private Vector2 scrollPosition;
        private bool toggleState;

        public void OnGUI()
        {
            EditorGUILayout.HelpBox("A new version of the Vive Input Utility is available!", MessageType.Warning);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                GUILayout.Label("Current version: " + VIUVersion.current);
                GUILayout.Label("New version: " + latestVersion);

                if (!string.IsNullOrEmpty(latestRepoInfo.body))
                {
                    GUILayout.Label("Release notes:");
                    EditorGUILayout.HelpBox(latestRepoInfo.body, MessageType.Info);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Get Latest Version"))
            {
                Application.OpenURL(pluginUrl);
            }

            EditorGUI.BeginChangeCheck();

            var doNotShow = GUILayout.Toggle(toggleState, "Do not prompt for this version again.");
            if (EditorGUI.EndChangeCheck())
            {
                toggleState = doNotShow;
                var key = string.Format(doNotShowKey, latestVersion);
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
}