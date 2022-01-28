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
                    if (!XRPluginManagementUtils.OnlyOneXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, requirdPlatform)) { return false; }
                    if (!IsOpenXRFeatureGroupEnabled(requirdPlatform, WAVE_XR_OPENXR_FEATURE_ID)) { return false; }
                    if (IsOpenXRFeatureGroupEnabled(requirdPlatform, OCULUS_QUEST_OPENXR_FEATURE_ID)) { return false; }
                    return true;
                }
                set
                {
                    if (value) { VIUSettings.activateUnityXRModule = true; }
                    if (value) { XRPluginManagementUtils.SetXRLoaderEnabled(OPENXR_PLUGIN_LOADER_NAME, requirdPlatform, true); }
                    SetOpenXRFeatureGroupEnable(requirdPlatform, WAVE_XR_OPENXR_FEATURE_ID, value);
                    SetOpenXRFeatureGroupEnable(requirdPlatform, OCULUS_QUEST_OPENXR_FEATURE_ID, !value);
                }
            }

            public override void OnPreferenceGUI()
            {
                const string title = "OpenXR Desktop (Experimental)";
                if (canSupport)
                {
                    support = Foldouter.ShowFoldoutBlankWithEnabledToggle(new GUIContent(title), support);
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
        }
    }
}