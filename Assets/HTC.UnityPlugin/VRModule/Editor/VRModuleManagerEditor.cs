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
                public Type[] argTypes = null;
                public ParameterModifier[] argModifiers = null;
            }

            public string scriptingDefineSymble = string.Empty;
            public string[] requiredTypeNames = null;
            public string requiredScriptFileName = string.Empty;
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
                        if (type.GetMethod(method.name, method.bindingAttr, null, CallingConventions.Any, method.argTypes ?? new Type[0], method.argModifiers ?? new ParameterModifier[0]) == null) { return false; }
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
                requiredScriptFileName = "ViveInput.cs",
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_STEAMVR",
                requiredTypeNames = new string[] { "SteamVR" },
                requiredScriptFileName = "SteamVR.cs",
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
                requiredScriptFileName = "SteamVR.cs",
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_OCULUSVR",
                requiredTypeNames = new string[] { "OVRInput" },
                requiredScriptFileName = "OVRInput.cs",
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_GOOGLEVR",
                requiredTypeNames = new string[] { "GvrUnitySdkVersion" },
                requiredScriptFileName = "GvrUnitySdkVersion.cs",
            });

            s_supportedSdkInfoList.Add(new VrSdkInfo()
            {
                scriptingDefineSymble = "VIU_WAVEVR",
                requiredTypeNames = new string[] { "WaveVR" },
                requiredScriptFileName = "WaveVR.cs",
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

        private static bool s_delayRemoved;
        private static List<string> s_symbolsToRemove;
        // This is called when ever an asset deleted
        // If the deleted asset include sdk files, then remove the related symbol
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            var fullPath = Application.dataPath + "/../" + assetPath;

            if (Directory.Exists(fullPath))
            {
                // is directory
                for (int i = 0, imax = s_supportedSdkInfoList.Count; i < imax; ++i)
                {
                    var requiredFiles = Directory.GetFiles(fullPath, s_supportedSdkInfoList[i].requiredScriptFileName, SearchOption.AllDirectories);
                    if (requiredFiles != null && requiredFiles.Length > 0)
                    {
                        if (s_symbolsToRemove == null) { s_symbolsToRemove = new List<string>(); }
                        s_symbolsToRemove.Add(s_supportedSdkInfoList[i].scriptingDefineSymble);
                    }
                }
            }
            else
            {
                // is file
                for (int i = 0, imax = s_supportedSdkInfoList.Count; i < imax; ++i)
                {
                    if (Path.GetFileName(fullPath) == s_supportedSdkInfoList[i].requiredScriptFileName)
                    {
                        if (s_symbolsToRemove == null) { s_symbolsToRemove = new List<string>(); }
                        s_symbolsToRemove.Add(s_supportedSdkInfoList[i].scriptingDefineSymble);
                    }
                }
            }

            if (!s_delayRemoved && s_symbolsToRemove != null && s_symbolsToRemove.Count > 0)
            {
                s_delayRemoved = true;
                EditorApplication.delayCall += RemoveSymbolsIfSDKDeleted;
            }

            return AssetDeleteResult.DidNotDelete;
        }

        // Should only called at once
        private static void RemoveSymbolsIfSDKDeleted()
        {
            EditorApplication.delayCall -= RemoveSymbolsIfSDKDeleted;

            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            var symbolsList = new List<string>(scriptingDefineSymbols.Split(';'));

            var removed = symbolsList.RemoveAll((symbol) => s_symbolsToRemove.Contains(symbol)) > 0;

            s_symbolsToRemove.Clear();

            if (removed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), string.Join(";", symbolsList.ToArray()));
            }
        }
    }
}