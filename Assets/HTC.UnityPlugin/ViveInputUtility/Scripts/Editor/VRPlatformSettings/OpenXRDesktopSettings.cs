using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public static partial class VIUSettingsEditor
    {
        private class OpenXRDesktopSettings : VRPlatformSetting
        {
            public override int order { get { return 4; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Standalone; } }

            public override bool canSupport
            {
                get
                {
                    if (activeBuildTargetGroup != requirdPlatform) { return false; }
                    if (!PackageManagerHelper.IsPackageInList(OPENXR_PLUGIN_PACKAGE_NAME)) { return false; }
                    return true;
                }
            }

            public override bool support
            {
                get
                {
                    if (!canSupport) { return false; }
                    if (!VIUSettings.activateUnityXRModule) { return false; }
                    if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, requirdPlatform)) { return false; }
                    return true;
                }
                set
                {
                    if (value) { VIUSettings.activateUnityXRModule = true; }
                    XRPluginManagementUtils.SetXRLoaderEnabled(OPENXR_PLUGIN_LOADER_TYPE, requirdPlatform, value);
                }
            }

            public override void OnPreferenceGUI()
            {
                const string title = "OpenXR Desktop (Experimental)";
                if (canSupport)
                {
                    var wasSupported = support;
                    var shouldSupport = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(title), support);
                    if (wasSupported != shouldSupport)
                    {
                        support = shouldSupport;
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    Foldouter.ShowFoldoutBlank();

                    if (activeBuildTargetGroup != requirdPlatform)
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title, requirdPlatform + " platform required."), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();
                        ShowSwitchPlatformButton(requirdPlatform, BuildTarget.StandaloneWindows64);
                    }
                    else if (!XRPluginManagementUtils.IsXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, requirdPlatform))
                    {
                        GUI.enabled = false;
                        ShowToggle(new GUIContent(title), false, GUILayout.Width(230f));
                        GUI.enabled = true;
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Enable OpenXR Loader", "Enable OpenXR Loader in XR Plug-in Management"), GUILayout.ExpandWidth(false)))
                        {
                            if (!ShowXRPluginManagementSection())
                            {
                                Debug.LogError("Fail opening XR Plug-in Management page, please enable OpenXR Loader manually.");
                            }
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }

        private static bool IsOpenXRFeatureGroupEnabled(BuildTargetGroup target, string id)
        {
#if VIU_OPENXR
            var featureSetting = UnityEditor.XR.OpenXR.Features.FeatureHelpers.GetFeatureWithIdForBuildTarget(target, id);
            return featureSetting != null && featureSetting.enabled;
#else
                return false;
#endif
        }

        private static void SetOpenXRFeatureGroupEnable(BuildTargetGroup target, string id, bool value)
        {
#if VIU_OPENXR
            var featureSetting = UnityEditor.XR.OpenXR.Features.FeatureHelpers.GetFeatureWithIdForBuildTarget(target, id);
            if (featureSetting != null)
            {
                featureSetting.enabled = value;
            }
#endif
        }

        private static bool ShowXRPluginManagementSection()
        {
#if VIU_OPENXR
            try
            {
                var projectSettingsType = Assembly.GetAssembly(typeof(SettingsProvider)).GetType("UnityEditor.ProjectSettingsWindow");
                var window = EditorWindow.GetWindow(projectSettingsType);
                window.Show();
                var selectMethod = projectSettingsType.GetMethod("SelectProviderByName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                selectMethod.Invoke(window, new object[] { "Project/XR Plug-in Management" });
            }
            catch
            {
                return false;
            }
            return true;
#else
            return false;
#endif
        }
    }
}