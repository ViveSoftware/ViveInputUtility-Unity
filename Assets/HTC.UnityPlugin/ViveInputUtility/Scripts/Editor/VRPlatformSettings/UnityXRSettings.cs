//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if VIU_OPENXR
using UnityEditor.XR.OpenXR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#endif

namespace HTC.UnityPlugin.Vive
{
    public class UnityXRRecommendedSettings : VIUVersionCheck.RecommendedSettingCollection
    {
#if VIU_OPENXR
        List<OpenXRFeature.ValidationRule> s_tempOpenXRValidationIssues = new List<OpenXRFeature.ValidationRule>();
#endif

        public UnityXRRecommendedSettings()
        {
#if VIU_OPENXR && VIU_XR_GENERAL_SETTINGS
            Add(new VIUVersionCheck.RecommendedSetting<int>
            {
                settingTitle = "Review OpenXR Project Validation Issues",
                skipCheckFunc = () => !VIUSettingsEditor.PackageManagerHelper.IsPackageInList(VIUSettingsEditor.OPENXR_PLUGIN_PACKAGE_NAME) || !XRPluginManagementUtils.IsXRLoaderEnabled(UnityXRModule.OPENXR_LOADER_NAME, UnityXRModule.OPENXR_LOADER_CLASS_NAME, VIUSettingsEditor.activeBuildTargetGroup),
                currentValueFunc = () => {
                    OpenXRProjectValidation.GetCurrentValidationIssues(s_tempOpenXRValidationIssues, VIUSettingsEditor.activeBuildTargetGroup);
                    return s_tempOpenXRValidationIssues.Count;
                },
                setValueFunc = (int value) =>
                {
                    try
                    {
                        Assembly openXREditorAsm = null;
                        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (asm.GetName().Name == "Unity.XR.OpenXR.Editor")
                            {
                                openXREditorAsm = asm;
                                break;
                            }
                        }

                        MethodInfo openWindowMethod = openXREditorAsm.GetType("UnityEditor.XR.OpenXR.OpenXRProjectValidationRulesSetup", true).GetMethod("ShowWindow", BindingFlags.NonPublic | BindingFlags.Static);
                        openWindowMethod.Invoke(null, new object[] {VIUSettingsEditor.activeBuildTargetGroup});
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Failed to open OpenXRProjectValidationRulesSetup: " + e);
                    }
                },
                recommendedValue = 0,
            });

            Add(new VIUVersionCheck.RecommendedSetting<bool>
            {
                settingTitle = "Enable All Feature Sets",
                skipCheckFunc = () => !VIUSettingsEditor.PackageManagerHelper.IsPackageInList(VIUSettingsEditor.OPENXR_PLUGIN_PACKAGE_NAME) || !XRPluginManagementUtils.IsXRLoaderEnabled(UnityXRModule.OPENXR_LOADER_NAME, UnityXRModule.OPENXR_LOADER_CLASS_NAME, VIUSettingsEditor.activeBuildTargetGroup),
                currentValueFunc = () =>
                {
                    return OpenXRSettings.ActiveBuildTargetInstance.GetFeatures<OpenXRInteractionFeature>().All(feature => feature.enabled);
                },
                setValueFunc = (bool value) =>
                {
                    if (!value)
                    {
                        return;
                    }

                    OpenXRFeature[] features = OpenXRSettings.ActiveBuildTargetInstance.GetFeatures<OpenXRInteractionFeature>();
                    foreach (OpenXRFeature feature in features)
                    {
                        feature.enabled = true;
                    }
                },
                recommendedValue = true,
            });
#endif
        }
    }
}