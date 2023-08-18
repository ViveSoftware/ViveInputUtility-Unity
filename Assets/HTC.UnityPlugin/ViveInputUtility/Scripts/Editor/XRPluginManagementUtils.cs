//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0618
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

#if VIU_XR_GENERAL_SETTINGS
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif

namespace HTC.UnityPlugin.Vive
{
    public static class XRPluginManagementUtils
    {
        public static bool IsXRLoaderEnabled(string loaderName, string loaderClassName, BuildTargetGroup buildTargetGroup)
        {
#if VIU_XR_GENERAL_SETTINGS
            XRGeneralSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (!xrSettings)
            {
                return false;
            }

            if (!xrSettings.AssignedSettings)
            {
                return false;
            }

            foreach (XRLoader loader in xrSettings.AssignedSettings.loaders)
            {
                if (loader.name == loaderName || loader.name == loaderClassName)
                {
                    return true;
                }
            }
#endif
            return false;
        }

        public static bool OnlyOneXRLoaderEnabled(string loaderName, string loaderClassName, BuildTargetGroup buildTargetGroup)
        {
#if VIU_XR_GENERAL_SETTINGS
            XRGeneralSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (!xrSettings)
            {
                return false;
            }

            if (!xrSettings.AssignedSettings)
            {
                return false;
            }

            var loaders = xrSettings.AssignedSettings.loaders;
            return loaders.Count == 1 && (loaders[0].name == loaderName || loaders[0].name == loaderClassName);
#else
            return false;
#endif
        }

        public static bool IsAnyXRLoaderEnabled(BuildTargetGroup buildTargetGroup)
        {
#if VIU_XR_GENERAL_SETTINGS
            XRGeneralSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (!xrSettings)
            {
                return false;
            }

            if (!xrSettings.AssignedSettings)
            {
                return false;
            }

            return xrSettings.AssignedSettings.loaders.Count > 0;
#else
            return false;
#endif
        }

        public static void SetXRLoaderEnabled(string loaderClassName, BuildTargetGroup buildTargetGroup, bool enabled)
        {
#if VIU_XR_GENERAL_SETTINGS
            MethodInfo method = Type.GetType("UnityEditor.XR.Management.XRSettingsManager, Unity.XR.Management.Editor")
                .GetProperty("currentSettings", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true);
            XRGeneralSettingsPerBuildTarget generalSettings = (XRGeneralSettingsPerBuildTarget)method.Invoke(null, new object[] { });

            XRGeneralSettings xrSettings = generalSettings.SettingsForBuildTarget(buildTargetGroup);

            if (xrSettings == null)
            {
                xrSettings = ScriptableObject.CreateInstance<XRGeneralSettings>() as XRGeneralSettings;
                generalSettings.SetSettingsForBuildTarget(buildTargetGroup, xrSettings);
                xrSettings.name = $"{buildTargetGroup.ToString()} Settings";
                AssetDatabase.AddObjectToAsset(xrSettings, AssetDatabase.GetAssetOrScenePath(generalSettings));
            }

            var serializedSettingsObject = new SerializedObject(xrSettings);
            SerializedProperty loaderProp = serializedSettingsObject.FindProperty("m_LoaderManagerInstance");
            if (loaderProp.objectReferenceValue == null)
            {
                var xrManagerSettings = ScriptableObject.CreateInstance<XRManagerSettings>() as XRManagerSettings;
                xrManagerSettings.name = $"{buildTargetGroup.ToString()} Providers";
                AssetDatabase.AddObjectToAsset(xrManagerSettings, AssetDatabase.GetAssetOrScenePath(generalSettings));
                loaderProp.objectReferenceValue = xrManagerSettings;
                serializedSettingsObject.ApplyModifiedProperties();
            }

            if (enabled)
            {
                if (!AssignLoader(xrSettings.AssignedSettings, loaderClassName, buildTargetGroup))
                {
                    Debug.LogWarning("Failed to assign XR loader: " + loaderClassName);
                }
            }
            else
            {
                if (!RemoveLoader(xrSettings.AssignedSettings, loaderClassName, buildTargetGroup))
                {
                    Debug.LogWarning("Failed to remove XR loader: " + loaderClassName);
                }
            }
#endif
        }

#if VIU_XR_GENERAL_SETTINGS
        private static readonly string[] s_loaderBlockList = { "DummyLoader", "SampleLoader", "XRLoaderHelper" };

        private static bool AssignLoader(XRManagerSettings settings, string loaderTypeName, BuildTargetGroup buildTargetGroup)
        {
#if VIU_XR_PACKAGE_METADATA_STORE
            return UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.AssignLoader(settings, loaderTypeName, buildTargetGroup);
#else
            var instance = GetInstanceOfTypeWithNameFromAssetDatabase(loaderTypeName);
            if (instance == null || !(instance is XRLoader))
            {
                instance = CreateScriptableObjectInstance(loaderTypeName, GetAssetPathForComponents(new string[] {"XR", "Loaders"}));
                if (instance == null)
                    return false;
            }

            List<XRLoader> assignedLoaders = new List<XRLoader>(settings.loaders);
            XRLoader newLoader = instance as XRLoader;

            if (!assignedLoaders.Contains(newLoader))
            {
                assignedLoaders.Add(newLoader);
                settings.loaders.Clear();

                List<string> allLoaderTypeNames = GetAllLoaderTypeNames();
                foreach (var typeName in allLoaderTypeNames)
                {
                    var newInstance = GetInstanceOfTypeWithNameFromAssetDatabase(typeName) as XRLoader;

                    if (newInstance != null && assignedLoaders.Contains(newInstance))
                    {
                        settings.loaders.Add(newInstance);
                    }
                }

                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            return true;
#endif
        }

        private static bool RemoveLoader(XRManagerSettings settings, string loaderTypeName, BuildTargetGroup buildTargetGroup)
        {
#if VIU_XR_PACKAGE_METADATA_STORE
            return UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.RemoveLoader(settings, loaderTypeName, buildTargetGroup);
#else
            var instance = GetInstanceOfTypeWithNameFromAssetDatabase(loaderTypeName);
            if (instance == null || !(instance is XRLoader))
                return false;

            XRLoader loader = instance as XRLoader;

            if (settings.loaders.Contains(loader))
            {
                settings.loaders.Remove(loader);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            return true;
#endif
        }

        private static ScriptableObject GetInstanceOfTypeWithNameFromAssetDatabase(string typeName)
        {
            string[] assetGUIDs = AssetDatabase.FindAssets(string.Format("t:{0}", typeName));
            if (assetGUIDs.Any())
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject));

                return asset as ScriptableObject;
            }

            return null;
        }

        private static ScriptableObject CreateScriptableObjectInstance(string typeName, string path)
        {
            ScriptableObject obj = ScriptableObject.CreateInstance(typeName) as ScriptableObject;
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string fileName = string.Format("{0}.asset", TypeNameToString(typeName));
                    string targetPath = Path.Combine(path, fileName);
                    AssetDatabase.CreateAsset(obj, targetPath);

                    return obj;
                }
            }

            Debug.LogError($"We were unable to create an instance of the requested type {typeName}. Please make sure that all packages are updated to support this version of XR Plug-In Management. See the Unity documentation for XR Plug-In Management for information on resolving this issue.");

            return null;
        }

        private static string GetAssetPathForComponents(string[] pathComponents, string root = "Assets")
        {
            if (pathComponents.Length <= 0)
                return null;

            string path = root;
            foreach (var pc in pathComponents)
            {
                string subFolder = Path.Combine(path, pc);
                bool shouldCreate = true;
                foreach (var f in AssetDatabase.GetSubFolders(path))
                {
                    if (string.Compare(Path.GetFullPath(f), Path.GetFullPath(subFolder), true) == 0)
                    {
                        shouldCreate = false;
                        break;
                    }
                }

                if (shouldCreate)
                    AssetDatabase.CreateFolder(path, pc);
                path = subFolder;
            }

            return path;
        }

        private static string TypeNameToString(Type type)
        {
            return type == null ? "" : TypeNameToString(type.FullName);
        }

        private static string TypeNameToString(string type)
        {
            string[] typeParts = type.Split(new char[] { '.' });
            if (!typeParts.Any())
                return String.Empty;

            string[] words = Regex.Matches(typeParts.Last(), "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();
            return string.Join(" ", words);
        }

        private static List<string> GetAllLoaderTypeNames()
        {
            List<string> loaderTypeNames = new List<string>();
            var loaderTypes = TypeCache.GetTypesDerivedFrom(typeof(XRLoader));
            foreach (Type loaderType in loaderTypes)
            {
                if (loaderType.IsAbstract)
                    continue;

                if (s_loaderBlockList.Contains(loaderType.Name))
                    continue;

                loaderTypeNames.Add(loaderType.Name);
            }

            return loaderTypeNames;
        }
#endif
    }
}