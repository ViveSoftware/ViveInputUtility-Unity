using UnityEngine;
using UnityEditor;

#if VIU_XR_GENERAL_SETTINGS
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
#endif

namespace HTC.UnityPlugin.Vive
{
    public static class XRPluginManagementUtils
    {
        public static bool IsXRLoaderEnabled(string loaderName, BuildTargetGroup buildTargetGroup)
        {
#if VIU_XR_GENERAL_SETTINGS
            XRGeneralSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (!xrSettings)
            {
                Debug.LogWarning("Failed to find XRGeneralSettings for build target group: " + buildTargetGroup);
                return false;
            }

            if (!xrSettings.AssignedSettings)
            {
                Debug.LogWarning("No assigned manager settings in the XRGeneralSettings for build target group: " + buildTargetGroup);
                return false;
            }
            
            foreach (XRLoader loader in xrSettings.AssignedSettings.loaders)
            {
                if (loader.name == loaderName)
                {
                    return true;
                }
            }
#endif
            return false;
        }

        public static bool IsAnyXRLoaderEnabled(BuildTargetGroup buildTargetGroup)
        {
#if VIU_XR_GENERAL_SETTINGS
            XRGeneralSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (!xrSettings)
            {
                Debug.LogWarning("Failed to find XRGeneralSettings for build target group: " + buildTargetGroup);
                return false;
            }

            if (!xrSettings.AssignedSettings)
            {
                Debug.LogWarning("No assigned manager settings in the XRGeneralSettings for build target group: " + buildTargetGroup);
                return false;
            }

            return xrSettings.AssignedSettings.loaders.Count > 0;
#endif
            return false;
        }

        public static void SetXRLoaderEnabled(string loaderClassName, BuildTargetGroup buildTargetGroup, bool enabled)
        {
#if VIU_XR_GENERAL_SETTINGS
            XRGeneralSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (!xrSettings)
            {
                Debug.LogWarning("Failed to find XRGeneralSettings for build target group: " + buildTargetGroup);
                return;
            }

            if (enabled)
            {
                if (!XRPackageMetadataStore.AssignLoader(xrSettings.AssignedSettings, loaderClassName, buildTargetGroup))
                {
                    Debug.LogWarning("Failed to assign XR loader: " + loaderClassName);
                }
            }
            else
            {
                if (!XRPackageMetadataStore.RemoveLoader(xrSettings.AssignedSettings, loaderClassName, buildTargetGroup))
                {
                    Debug.LogWarning("Failed to remove XR loader: " + loaderClassName);
                }
            }
#endif
        }
    }
}