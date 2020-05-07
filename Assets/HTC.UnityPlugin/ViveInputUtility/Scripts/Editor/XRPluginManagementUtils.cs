using UnityEditor;

#if VIU_XR_GENERAL_SETTINGS
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
#endif

namespace HTC.UnityPlugin.Vive
{
    public static class XRPluginManagementUtils
    {
        public static bool IsXRLoaderEnabled(string loaderName)
        {
#if VIU_XR_GENERAL_SETTINGS
            if (!XRGeneralSettings.Instance || !XRGeneralSettings.Instance.Manager)
            {
                return false;
            }

            foreach (XRLoader loader in XRGeneralSettings.Instance.Manager.loaders)
            {
                if (loader.name == loaderName)
                {
                    return true;
                }
            }
#endif
            return false;
        }

        public static bool IsAnyXRLoaderEnabled()
        {
#if VIU_XR_GENERAL_SETTINGS
            if (!XRGeneralSettings.Instance || !XRGeneralSettings.Instance.Manager)
            {
                return false;
            }

            return XRGeneralSettings.Instance.Manager.loaders.Count > 0;
#endif
            return false;
        }

        public static void SetXRLoaderEnabled(string loaderClassName, BuildTargetGroup buildTargetGroup, bool enabled)
        {
#if VIU_XR_GENERAL_SETTINGS
            if (!XRGeneralSettings.Instance)
            {
                return;
            }

            if (enabled)
            {
                XRPackageMetadataStore.AssignLoader(XRGeneralSettings.Instance.AssignedSettings, loaderClassName, buildTargetGroup);
            }
            else
            {
                XRPackageMetadataStore.RemoveLoader(XRGeneralSettings.Instance.AssignedSettings, loaderClassName, buildTargetGroup);
            }
#endif
        }
    }
}