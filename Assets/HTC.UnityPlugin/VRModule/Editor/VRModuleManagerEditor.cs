//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class VRModuleManagerEditor : UnityEditor.AssetModificationProcessor
    {
        private class VrSdkInfo
        {
            public class ReqFieldInfo
            {
                public string typeName = string.Empty;
                public string name = string.Empty;
                public BindingFlags bindingAttr = BindingFlags.Default;
            }

            public class ReqMethodInfo
            {
                public string typeName = string.Empty;
                public string name = string.Empty;
                public BindingFlags bindingAttr;
                public string[] argTypeNames = null;
                public ParameterModifier[] argModifiers = null;
            }

            public string scriptingDefineSymble = string.Empty;
            public string[] requiredTypeNames = null;
            public string[] requiredScriptFileNames = null;
            public ReqFieldInfo[] requiredFields = null;
            public ReqMethodInfo[] requiredMethods = null;

            private Dictionary<string, Type> m_foundTypes;

            public void FindRequiredTypesInAssembly(Assembly assembly)
            {
                if (requiredTypeNames != null)
                {
                    foreach (var name in requiredTypeNames)
                    {
                        TryAddTypeFromAssembly(name, assembly);
                    }
                }

                if (requiredFields != null)
                {
                    foreach (var field in requiredFields)
                    {
                        TryAddTypeFromAssembly(field.typeName, assembly);
                    }
                }

                if (requiredMethods != null)
                {
                    foreach (var method in requiredMethods)
                    {
                        TryAddTypeFromAssembly(method.typeName, assembly);

                        if (method.argTypeNames != null)
                        {
                            foreach (var typeName in method.argTypeNames)
                            {
                                TryAddTypeFromAssembly(typeName, assembly);
                            }
                        }
                    }
                }
            }

            private bool TryAddTypeFromAssembly(string name, Assembly assembly)
            {
                if (string.IsNullOrEmpty(name) || RequiredTypeFound(name)) { return false; }
                var type = assembly.GetType(name);
                if (type == null) { return false; }
                if (m_foundTypes == null) { m_foundTypes = new Dictionary<string, Type>(); }
                m_foundTypes.Add(name, type);
                return true;
            }

            private bool RequiredTypeFound(string name)
            {
                return m_foundTypes == null ? false : m_foundTypes.ContainsKey(name);
            }

            public bool SdkFound()
            {
                if (m_foundTypes == null) { return false; }

                foreach (var name in requiredTypeNames)
                {
                    if (!m_foundTypes.ContainsKey(name)) { return false; }
                }

                if (requiredFields != null)
                {
                    foreach (var field in requiredFields)
                    {
                        Type type;
                        if (!m_foundTypes.TryGetValue(field.typeName, out type)) { return false; }
                        if (type.GetField(field.name, field.bindingAttr) == null) { return false; }
                    }
                }

                if (requiredMethods != null)
                {
                    foreach (var method in requiredMethods)
                    {
                        Type type;
                        if (!m_foundTypes.TryGetValue(method.typeName, out type)) { return false; }

                        var argTypes = new Type[method.argTypeNames == null ? 0 : method.argTypeNames.Length];
                        for (int i = argTypes.Length - 1; i >= 0; --i)
                        {
                            if (!m_foundTypes.TryGetValue(method.argTypeNames[i], out argTypes[i])) { return false; }
                        }

                        if (type.GetMethod(method.name, method.bindingAttr, null, CallingConventions.Any, argTypes, method.argModifiers ?? new ParameterModifier[0]) == null) { return false; }
                    }
                }

                if (requiredScriptFileNames != null)
                {
                    foreach (var requiredFile in requiredScriptFileNames)
                    {
                        var files = Directory.GetFiles(Application.dataPath, requiredFile, SearchOption.AllDirectories);
                        if (files == null || files.Length == 0) { return false; }
                    }
                }

                return true;
            }
        }

        private static List<VrSdkInfo> s_supportedSdkInfoList;

        static VRModuleManagerEditor()
        {
            s_supportedSdkInfoList = new List<VrSdkInfo>();

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_PLUGIN",
                requiredTypeNames = new string[] { "HTC.UnityPlugin.Vive.ViveInput" },
                requiredScriptFileNames = new string[] { "ViveInput.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_STEAMVR",
                requiredTypeNames = new string[] { "SteamVR" },
                requiredScriptFileNames = new string[] { "SteamVR.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_STEAMVR_1_2_0_OR_NEWER",
                requiredTypeNames = new string[] { "SteamVR_Events" },
                requiredScriptFileNames = new string[] { "SteamVR_Events.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_STEAMVR_1_2_1_OR_NEWER",
                requiredTypeNames = new string[] { "SteamVR_Events" },
                requiredMethods = new VrSdkInfo.ReqMethodInfo[]
                {
                    new VrSdkInfo.ReqMethodInfo()
                    {
                         typeName = "SteamVR_Events",
                         name = "System",
                         argTypeNames = new string[] { "Valve.VR.EVREventType" },
                         bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
                requiredScriptFileNames = new string[] { "SteamVR_Events.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_STEAMVR_1_2_2_OR_NEWER",
                requiredTypeNames = new string[] { "SteamVR_ExternalCamera+Config" },
                requiredFields = new VrSdkInfo.ReqFieldInfo[]
                {
                    new VrSdkInfo.ReqFieldInfo()
                    {
                        typeName = "SteamVR_ExternalCamera+Config",
                        name = "r",
                        bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                requiredScriptFileNames = new string[] { "SteamVR_ExternalCamera.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_STEAMVR_1_2_3_OR_NEWER",
                requiredTypeNames = new string[] { "Valve.VR.CVRSystem" },
                requiredMethods = new VrSdkInfo.ReqMethodInfo[]
                {
                    new VrSdkInfo.ReqMethodInfo()
                    {
                         typeName = "Valve.VR.CVRSystem",
                         name = "IsInputAvailable",
                         bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                requiredScriptFileNames = new string[] { "openvr_api.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_OCULUSVR",
                requiredTypeNames = new string[] { "OVRInput" },
                requiredScriptFileNames = new string[] { "OVRInput.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_GOOGLEVR",
                requiredTypeNames = new string[] { "GvrUnitySdkVersion" },
                requiredScriptFileNames = new string[] { "GvrUnitySdkVersion.cs" },
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_WAVEVR",
                requiredTypeNames = new string[] { "WaveVR" },
                requiredScriptFileNames = new string[] { "WaveVR.cs" },
            });
        }

        [DidReloadScripts]
        private static void UpdateScriptingDefineSymbols()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                for (int i = 0, imax = s_supportedSdkInfoList.Count; i < imax; ++i)
                {
                    s_supportedSdkInfoList[i].FindRequiredTypesInAssembly(assembly);
                }
            }

            var symbolListChanged = false;
            var symbolList = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';'));

            for (int i = 0, imax = s_supportedSdkInfoList.Count; i < imax; ++i)
            {
                if (s_supportedSdkInfoList[i].SdkFound())
                {
                    if (!symbolList.Contains(s_supportedSdkInfoList[i].scriptingDefineSymble))
                    {
                        symbolList.Add(s_supportedSdkInfoList[i].scriptingDefineSymble);
                        symbolListChanged = true;
                    }
                }
                else
                {
                    if (symbolList.RemoveAll((symbol) => symbol == s_supportedSdkInfoList[i].scriptingDefineSymble) > 0)
                    {
                        symbolListChanged = true;
                    }
                }
            }

            if (symbolListChanged)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", symbolList.ToArray()));
            }
        }

        private static bool s_delayCallRemoveRegistered;
        // This is called when ever an asset deleted
        // If the deleted asset include sdk files, then remove all symbols defined by VIU
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            var fullPath = Application.dataPath + "/../" + assetPath;
            var isDir = Directory.Exists(fullPath); // otherwise, removed asset is file
            var requiredFileFound = false;

            foreach (var sdkInfo in s_supportedSdkInfoList)
            {
                foreach (var requiredFile in sdkInfo.requiredScriptFileNames)
                {
                    if (isDir)
                    {
                        var files = Directory.GetFiles(fullPath, requiredFile, SearchOption.AllDirectories);
                        requiredFileFound = files != null && files.Length > 0;
                    }
                    else
                    {
                        requiredFileFound = Path.GetFileName(fullPath) == requiredFile;
                    }

                    if (requiredFileFound)
                    {
                        if (!s_delayCallRemoveRegistered)
                        {
                            s_delayCallRemoveRegistered = true;
                            EditorApplication.delayCall += RemoveAllVIUSymbols;
                        }

                        return AssetDeleteResult.DidNotDelete;
                    }
                }
            }

            return AssetDeleteResult.DidNotDelete;
        }

        private static void RemoveAllVIUSymbols()
        {
            EditorApplication.delayCall -= RemoveAllVIUSymbols;

            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            var definedSymbols = new List<string>(scriptingDefineSymbols.Split(';'));

            foreach (var sdkInfo in s_supportedSdkInfoList)
            {
                definedSymbols.Remove(sdkInfo.scriptingDefineSymble);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), string.Join(";", definedSymbols.ToArray()));
        }
    }
}