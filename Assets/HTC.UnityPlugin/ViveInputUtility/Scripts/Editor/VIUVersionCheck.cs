//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.IO;
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

        private interface IPropSetting
        {
            bool IsIgnored();
            bool IsUsingRecommendedValue();
            void DoDrawRecommend();
            void AcceptRecommendValue();
            void DoIgnore();
            void DeleteIgnore();
        }

        private class PropSetting<T> : IPropSetting
        {
            private const string fmtTitle = "{0} (current = {1})";
            private const string fmtRecommendBtn = "Use recommended ({0})";
            private const string fmtRecommendBtnWithPosefix = "Use recommended ({0}) - {1}";

            private string m_settingTitle;
            private string m_settingTrimedTitle;
            private string ignoreKey { get { return editorPrefsPrefix + m_settingTrimedTitle; } }

            public string settingTitle { get { return m_settingTitle; } set { m_settingTitle = value; m_settingTrimedTitle = value.Replace(" ", ""); } }
            public string recommendBtnPostfix;
            public string toolTip = string.Empty;
            public Func<T> currentValueFunc = null;
            public Action<T> setValueFunc = null;
            public T recommendedValue = default(T);

            public bool IsIgnored() { return EditorPrefs.HasKey(ignoreKey); }

            public bool IsUsingRecommendedValue() { return EqualityComparer<T>.Default.Equals(currentValueFunc(), recommendedValue); }

            public void DoDrawRecommend()
            {
                GUILayout.Label(new GUIContent(string.Format(fmtTitle, settingTitle, currentValueFunc()), toolTip));

                GUILayout.BeginHorizontal();

                bool recommendBtnClicked;
                if (string.IsNullOrEmpty(recommendBtnPostfix))
                {
                    recommendBtnClicked = GUILayout.Button(new GUIContent(string.Format(fmtRecommendBtn, recommendedValue), toolTip));
                }
                else
                {
                    recommendBtnClicked = GUILayout.Button(new GUIContent(string.Format(fmtRecommendBtnWithPosefix, recommendedValue, recommendBtnPostfix), toolTip));
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
                setValueFunc(recommendedValue);
            }

            public void DoIgnore()
            {
                EditorPrefs.SetBool(ignoreKey, true);
            }

            public void DeleteIgnore()
            {
                EditorPrefs.DeleteKey(ignoreKey);
            }
        }

        public const string VIU_BINDING_INTERFACE_SWITCH_SYMBOL = "VIU_BINDING_INTERFACE_SWITCH";
        public const string VIU_EXTERNAL_CAMERA_SWITCH_SYMBOL = "VIU_EXTERNAL_CAMERA_SWITCH";

        public const string lastestVersionUrl = "https://api.github.com/repos/ViveSoftware/ViveInputUtility-Unity/releases/latest";
        public const string pluginUrl = "https://github.com/ViveSoftware/ViveInputUtility-Unity/releases";
        public const double versionCheckIntervalMinutes = 60.0;

        // On Windows, PlaterSetting is stored at \HKEY_CURRENT_USER\Software\Unity Technologies\Unity Editor 5.x
        private static string editorPrefsPrefix;
        private static string nextVersionCheckTimeKey;
        private static string fmtIgnoreUpdateKey;
        private static string ignoreThisVersionKey;

        private const string BIND_UI_SWITCH_TOOLTIP = "When enabled, pressing RightShift + B to open the binding interface in play mode.";
        private const string EX_CAM_UI_SWITCH_TOOLTIP = "When enabled, pressing RightShift + M to toggle the quad view while external camera config file exist.";
        private static bool s_waitingForCompile;

        private static bool completeCheckVersionFlow = false;
        private static WWW www;
        private static RepoInfo latestRepoInfo;
        private static Version latestVersion;
        private static VIUVersionCheck window;
        private static Vector2 releaseNoteScrollPosition;
        private static Vector2 settingScrollPosition;
        private static bool showNewVersion;

        private static bool toggleSkipThisVersion = false;
#if VIU_BINDING_INTERFACE_SWITCH
        private static bool toggleBindUISwithState = true;
#else
        private static bool toggleBindUISwithState = false;
#endif
#if VIU_EXTERNAL_CAMERA_SWITCH
        private static bool toggleExCamSwithState = true;
#else
        private static bool toggleExCamSwithState = false;
#endif

        private static IPropSetting[] s_settings = new IPropSetting[]
        {
            new PropSetting<bool>()
            {
            settingTitle = "Binding Interface",
            recommendBtnPostfix = "requires re-compiling",
            toolTip = BIND_UI_SWITCH_TOOLTIP + " You can change this option later in Edit -> Preferences... -> VIU Settings.",
            currentValueFunc = () => toggleBindUISwithState,
            setValueFunc = (v) => toggleBindUISwithState = v,
#if VIU_STEAMVR
            recommendedValue = true,
#else
            recommendedValue = false,
#endif
            },

            new PropSetting<bool>()
            {
            settingTitle = "External Camera Interface",
            recommendBtnPostfix = "requires re-compiling",
            toolTip = EX_CAM_UI_SWITCH_TOOLTIP + " You can change this option later in Edit -> Preferences... -> VIU Settings.",
            currentValueFunc = () => toggleExCamSwithState,
            setValueFunc = (v) => toggleExCamSwithState = v,
#if VIU_STEAMVR
            recommendedValue = true,
#else
            recommendedValue = false,
#endif
            },

#if !VIU_STEAMVR

            new PropSetting<bool>()
            {
            settingTitle = "Show Unity Splashscreen",
#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
			currentValueFunc = () => PlayerSettings.showUnitySplashScreen,
            setValueFunc = (v) => PlayerSettings.showUnitySplashScreen = v,
#else
			currentValueFunc = () => PlayerSettings.SplashScreen.show,
            setValueFunc = (v) => PlayerSettings.SplashScreen.show = v,
#endif
            recommendedValue = false,
            },

            new PropSetting<bool>()
            {
            settingTitle = "Default Is Fullscreen",
            currentValueFunc = () => PlayerSettings.defaultIsFullScreen,
            setValueFunc = (v) => PlayerSettings.defaultIsFullScreen = v,
            recommendedValue = false,
            },

            new PropSetting<Vector2>()
            {
            settingTitle = "Default Screen Size",
            currentValueFunc = () => new Vector2(PlayerSettings.defaultScreenWidth, PlayerSettings.defaultScreenHeight),
            setValueFunc = (v) => { PlayerSettings.defaultScreenWidth = (int)v.x; PlayerSettings.defaultScreenHeight = (int)v.y; },
            recommendedValue = new Vector2(1024f, 768f),
            },

            new PropSetting<bool>()
            {
            settingTitle = "Run In Background",
            currentValueFunc = () => PlayerSettings.runInBackground,
            setValueFunc = (v) => PlayerSettings.runInBackground = v,
            recommendedValue = true,
            },

            new PropSetting<ResolutionDialogSetting>()
            {
            settingTitle = "Display Resolution Dialog",
            currentValueFunc = () => PlayerSettings.displayResolutionDialog,
            setValueFunc = (v) => PlayerSettings.displayResolutionDialog = v,
            recommendedValue = ResolutionDialogSetting.HiddenByDefault,
            },

            new PropSetting<bool>()
            {
            settingTitle = "Resizable Window",
            currentValueFunc = () => PlayerSettings.resizableWindow,
            setValueFunc = (v) => PlayerSettings.resizableWindow = v,
            recommendedValue = true,
            },

            new PropSetting<D3D11FullscreenMode>()
            {
            settingTitle = "D3D11 Fullscreen Mode",
            currentValueFunc = () => PlayerSettings.d3d11FullscreenMode,
            setValueFunc = (v) => PlayerSettings.d3d11FullscreenMode = v,
            recommendedValue = D3D11FullscreenMode.FullscreenWindow,
            },

            new PropSetting<bool>()
            {
            settingTitle = "Visible In Background",
            currentValueFunc = () => PlayerSettings.visibleInBackground,
            setValueFunc = (v) => PlayerSettings.visibleInBackground = v,
            recommendedValue = true,
            },

#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
            new PropSetting<RenderingPath>()
            {
            settingTitle = "Rendering Path",
            recommendBtnPostfix = "required for MSAA",
            currentValueFunc = () => PlayerSettings.renderingPath,
            setValueFunc = (v) => PlayerSettings.renderingPath = v,
            recommendedValue = RenderingPath.Forward,
            },
#endif

            new PropSetting<ColorSpace>()
            {
            settingTitle = "Color Space",
            recommendBtnPostfix = "requires reloading scene",
            currentValueFunc = () => PlayerSettings.colorSpace,
            setValueFunc = (v) => PlayerSettings.colorSpace = v,
            recommendedValue = ColorSpace.Linear,
            },

#if !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
            new PropSetting<bool>()
            {
            settingTitle = "GPU Skinning",
            currentValueFunc = () => PlayerSettings.gpuSkinning ,
            setValueFunc = (v) =>PlayerSettings.gpuSkinning  = v,
            recommendedValue = true,
            },
#endif

#if (UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
            new PropSetting<bool>()
            {
            settingTitle = "Stereoscopic Rendering",
            currentValueFunc = () => PlayerSettings.stereoscopic3D,
            setValueFunc = (v) => PlayerSettings.stereoscopic3D = v,
            recommendedValue = false,
            },
#endif

#if UNITY_STANDALONE && (VIU_OCULUS || UNITY_5_5_OR_NEWER)
            new PropSetting<bool>()
            {
            settingTitle = "Virtual Reality Support",
            currentValueFunc = () => PlayerSettings.virtualRealitySupported,
            setValueFunc = (v) => PlayerSettings.virtualRealitySupported = v,
            recommendedValue = true,
            },
#endif

#endif // !VIU_STEAMVR
        };

        private Texture2D viuLogo;

        static VIUVersionCheck()
        {
            EditorApplication.update += CheckVersionAndSettings;
        }

        // check vive input utility version on github
        private static void CheckVersionAndSettings()
        {
            if (string.IsNullOrEmpty(editorPrefsPrefix))
            {
                editorPrefsPrefix = "ViveInputUtility." + PlayerSettings.productGUID + ".";
                nextVersionCheckTimeKey = editorPrefsPrefix + "LastVersionCheckTime";
                fmtIgnoreUpdateKey = editorPrefsPrefix + "DoNotShowUpdate.v{0}";

                // Force refresh preference window so it won't stuck in "re-compinling" state
                if (GUIUtility.hotControl == 0)
                {
                    var prefWindow = GetWindow<EditorWindow>("Unity Preferences", false);
                    if (prefWindow != null && prefWindow.titleContent.text == "Unity Preferences")
                    {
                        prefWindow.Repaint();
                    }
                }
            }

            // fetch new version info from github release site
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

                // parse latestVersion and ignoreThisVersionKey
                if (!string.IsNullOrEmpty(latestRepoInfo.tag_name))
                {
                    try
                    {
                        latestVersion = new Version(Regex.Replace(latestRepoInfo.tag_name, "[^0-9\\.]", string.Empty));
                        ignoreThisVersionKey = string.Format(fmtIgnoreUpdateKey, latestVersion.ToString());
                    }
                    catch
                    {
                        latestVersion = default(Version);
                        ignoreThisVersionKey = string.Empty;
                    }
                }

                www.Dispose();
                www = null;

                completeCheckVersionFlow = true;
            }

            showNewVersion = !string.IsNullOrEmpty(ignoreThisVersionKey) && !EditorPrefs.HasKey(ignoreThisVersionKey) && latestVersion > VIUVersion.current;

            // check if their is setting that not using recommended value and not ignored
            var recommendCount = 0; // not ignored and not using recommended value
            foreach (var setting in s_settings)
            {
                if (!setting.IsIgnored() && !setting.IsUsingRecommendedValue())
                {
                    ++recommendCount;
                }
            }

            if (showNewVersion || recommendCount > 0)
            {
                window = GetWindow<VIUVersionCheck>(true, "Vive Input Utility");
                window.minSize = new Vector2(240f, 550f);

                var rect = window.position;
                window.position = new Rect(Mathf.Max(rect.x, 50f), Mathf.Max(rect.y, 50f), rect.width, 200f + (showNewVersion ? 700f : 400f));
            }

            EditorApplication.update -= CheckVersionAndSettings;
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

        private string GetResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Scripts/Editor".Length) + "Textures/";
        }

        public void OnGUI()
        {
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

            var notRecommendedCount = 0;
            var ignoredCount = 0; // ignored and not using recommended value
            var drawCount = 0; // not ignored and not using recommended value

            foreach (var setting in s_settings)
            {
                if (setting.IsIgnored()) { ++ignoredCount; }

                if (setting.IsUsingRecommendedValue()) { continue; }
                else { ++notRecommendedCount; }

                if (!setting.IsIgnored())
                {
                    if (drawCount == 0)
                    {
                        EditorGUILayout.HelpBox("Recommended project settings:", MessageType.Warning);

                        settingScrollPosition = GUILayout.BeginScrollView(settingScrollPosition, GUILayout.ExpandHeight(true));
                    }

                    ++drawCount;
                    setting.DoDrawRecommend();
                }
            }

            if (drawCount > 0)
            {
                GUILayout.EndScrollView();

                if (ignoredCount > 0)
                {
                    if (GUILayout.Button("Clear All Ignores(" + ignoredCount + ")"))
                    {
                        foreach (var setting in s_settings) { setting.DeleteIgnore(); }
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Accept All(" + drawCount + ")"))
                    {
                        foreach (var setting in s_settings) { if (!setting.IsIgnored()) { setting.AcceptRecommendValue(); } }
                    }

                    if (GUILayout.Button("Ignore All(" + drawCount + ")"))
                    {
                        foreach (var setting in s_settings) { if (!setting.IsIgnored() && !setting.IsUsingRecommendedValue()) { setting.DoIgnore(); } }
                    }
                }
                GUILayout.EndHorizontal();
            }
            else if (notRecommendedCount > 0)
            {
                EditorGUILayout.HelpBox("Some recommended settings ignored.", MessageType.Warning);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear All Ignores(" + ignoredCount + ")"))
                {
                    foreach (var setting in s_settings) { setting.DeleteIgnore(); }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All recommended settings applied.", MessageType.Info);

                GUILayout.FlexibleSpace();
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
                EditorPrefs.SetBool(ignoreThisVersionKey, true);
            }

            if (
#if VIU_BINDING_INTERFACE_SWITCH
                !toggleBindUISwithState
#else
                toggleBindUISwithState
#endif
                ||
#if VIU_EXTERNAL_CAMERA_SWITCH
                !toggleExCamSwithState
#else
                toggleExCamSwithState
#endif
                )
            {
                EditSymbols(
                    new EditSymbolArg() { symbol = VIU_BINDING_INTERFACE_SWITCH_SYMBOL, enable = toggleBindUISwithState },
                    new EditSymbolArg() { symbol = VIU_EXTERNAL_CAMERA_SWITCH_SYMBOL, enable = toggleExCamSwithState }
                );
            }
        }

        private struct EditSymbolArg
        {
            public string symbol;
            public bool enable;
        }

        private static void EditSymbols(params EditSymbolArg[] args)
        {
            var symbolChanged = false;
            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var symbolsList = new List<string>(scriptingDefineSymbols.Split(';'));

            foreach (var arg in args)
            {
                if (arg.enable)
                {
                    if (!symbolsList.Contains(arg.symbol))
                    {
                        symbolsList.Add(arg.symbol);
                        symbolChanged = true;
                    }
                }
                else
                {
                    if (symbolsList.RemoveAll(s => s == arg.symbol) > 0)
                    {
                        symbolChanged = true;
                    }
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

        [PreferenceItem("VIU Settings")]
        public static void OnVIUPreferenceGUI()
        {
            EditorGUILayout.LabelField("Vive Input Utility v" + VIUVersion.current);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("Checkout Latest Version", "Goto " + pluginUrl)))
                {
                    Application.OpenURL(pluginUrl);
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            if (!s_waitingForCompile)
            {
                bool toggleValue;

                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();
                {
                    toggleValue = EditorGUILayout.ToggleLeft(new GUIContent("", BIND_UI_SWITCH_TOOLTIP),
#if VIU_BINDING_INTERFACE_SWITCH
                    true
#else
                    false
#endif
                    , GUILayout.MaxWidth(15f));
                    EditorGUILayout.LabelField(new GUIContent("Enable Binding Interface Switch - requires re-compiling", BIND_UI_SWITCH_TOOLTIP));
                }
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    s_waitingForCompile = true;
                    EditSymbols(new EditSymbolArg() { symbol = VIU_BINDING_INTERFACE_SWITCH_SYMBOL, enable = toggleValue });
                    return;
                }

                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();
                {
                    toggleValue = EditorGUILayout.ToggleLeft(new GUIContent("", EX_CAM_UI_SWITCH_TOOLTIP),
#if VIU_EXTERNAL_CAMERA_SWITCH
                    true
#else
                    false
#endif
                    , GUILayout.MaxWidth(15f));
                    EditorGUILayout.LabelField(new GUIContent("Enable External Camera Switch - requires re-compiling", EX_CAM_UI_SWITCH_TOOLTIP));
                }
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    s_waitingForCompile = true;
                    EditSymbols(new EditSymbolArg() { symbol = VIU_EXTERNAL_CAMERA_SWITCH_SYMBOL, enable = toggleValue });
                    return;
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Re-compiling...");
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
        }
    }
}