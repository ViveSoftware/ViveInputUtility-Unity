//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    // This script manage define symbols used by VIU
    public class VRModuleManagerEditor : UnityEditor.AssetModificationProcessor
#if UNITY_2017_1_OR_NEWER
        , UnityEditor.Build.IActiveBuildTargetChanged
#endif
    {
        public class SymbolRequirement
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

            public string symbol = string.Empty;
            public string[] reqTypeNames = null;
            public string[] reqAnyTypeNames = null;
            public string[] reqFileNames = null;
            public ReqFieldInfo[] reqFields = null;
            public ReqMethodInfo[] reqMethods = null;
            public Func<SymbolRequirement, bool> validateFunc = null;

            public static Dictionary<string, Type> s_foundTypes;

            public static void ResetFoundTypes()
            {
                if (s_foundTypes != null)
                {
                    s_foundTypes.Clear();
                }
            }

            public void FindRequiredTypesInAssembly(Assembly assembly)
            {
                if (reqTypeNames != null)
                {
                    foreach (var name in reqTypeNames)
                    {
                        TryAddTypeFromAssembly(name, assembly);
                    }
                }

                if (reqAnyTypeNames != null)
                {
                    foreach (var name in reqAnyTypeNames)
                    {
                        TryAddTypeFromAssembly(name, assembly);
                    }
                }

                if (reqFields != null)
                {
                    foreach (var field in reqFields)
                    {
                        TryAddTypeFromAssembly(field.typeName, assembly);
                    }
                }

                if (reqMethods != null)
                {
                    foreach (var method in reqMethods)
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
                if (s_foundTypes == null) { s_foundTypes = new Dictionary<string, Type>(); }
                s_foundTypes.Add(name, type);
                return true;
            }

            private bool RequiredTypeFound(string name)
            {
                return s_foundTypes == null ? false : s_foundTypes.ContainsKey(name);
            }

            public bool Validate()
            {
                if (s_foundTypes == null) { return false; }

                if (reqTypeNames != null)
                {
                    foreach (var name in reqTypeNames)
                    {
                        if (!s_foundTypes.ContainsKey(name)) { return false; }
                    }
                }

                if (reqAnyTypeNames != null)
                {
                    var found = false;

                    foreach (var name in reqAnyTypeNames)
                    {
                        if (s_foundTypes.ContainsKey(name))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found) { return false; }
                }

                if (reqFields != null)
                {
                    foreach (var field in reqFields)
                    {
                        Type type;
                        if (!s_foundTypes.TryGetValue(field.typeName, out type)) { return false; }
                        if (type.GetField(field.name, field.bindingAttr) == null) { return false; }
                    }
                }

                if (reqMethods != null)
                {
                    foreach (var method in reqMethods)
                    {
                        Type type;
                        if (!s_foundTypes.TryGetValue(method.typeName, out type)) { return false; }

                        var argTypes = new Type[method.argTypeNames == null ? 0 : method.argTypeNames.Length];
                        for (int i = argTypes.Length - 1; i >= 0; --i)
                        {
                            if (!s_foundTypes.TryGetValue(method.argTypeNames[i], out argTypes[i])) { return false; }
                        }

                        if (type.GetMethod(method.name, method.bindingAttr, null, CallingConventions.Any, argTypes, method.argModifiers ?? new ParameterModifier[0]) == null) { return false; }
                    }
                }

                if (reqFileNames != null)
                {
                    foreach (var requiredFile in reqFileNames)
                    {
                        var files = Directory.GetFiles(Application.dataPath, requiredFile, SearchOption.AllDirectories);
                        if (files == null || files.Length == 0) { return false; }
                    }
                }

                if (validateFunc != null)
                {
                    if (!validateFunc(this)) { return false; }
                }

                return true;
            }
        }

        public abstract class SymbolRequirementCollection : List<SymbolRequirement> { }

        private static List<SymbolRequirement> s_symbolReqList;

        static VRModuleManagerEditor()
        {
            s_symbolReqList = new List<SymbolRequirement>();

            foreach (var type in Assembly.GetAssembly(typeof(SymbolRequirementCollection)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(SymbolRequirementCollection))))
            {
                s_symbolReqList.AddRange((SymbolRequirementCollection)Activator.CreateInstance(type));
            }

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_PLUGIN",
                reqTypeNames = new string[] { "HTC.UnityPlugin.Vive.ViveInput" },
                reqFileNames = new string[] { "ViveInput.cs", "VRModuleManagerEditor.cs" },
            });

            // Obsolete symbol, will be removed in all condition
            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_EXTERNAL_CAMERA_SWITCH",
                reqFileNames = new string[] { "" },
            });

            // Obsolete symbol, will be removed in all condition
            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_BINDING_INTERFACE_SWITCH",
                reqFileNames = new string[] { "" },
            });

#if !UNITY_2017_1_OR_NEWER
            EditorUserBuildSettings.activeBuildTargetChanged += UpdateScriptingDefineSymbols;
#endif
        }

#if UNITY_2017_1_OR_NEWER
        public int callbackOrder { get { return 0; } }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            UpdateScriptingDefineSymbols();
        }
#endif

        [DidReloadScripts]
        public static void UpdateScriptingDefineSymbols()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var symbolReq in s_symbolReqList)
                    {
                        symbolReq.FindRequiredTypesInAssembly(assembly);
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogWarning(e);
                    Debug.LogWarning("load assembly " + assembly.FullName + " fail");
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            var defineSymbols = GetDefineSymbols();
            var defineSymbolsChanged = false;

            foreach (var symbolReq in s_symbolReqList)
            {
                if (symbolReq.Validate())
                {
                    if (!defineSymbols.Contains(symbolReq.symbol))
                    {
                        defineSymbols.Add(symbolReq.symbol);
                        defineSymbolsChanged = true;
                    }
                }
                else
                {
                    if (defineSymbols.RemoveAll((symbol) => symbol == symbolReq.symbol) > 0)
                    {
                        defineSymbolsChanged = true;
                    }
                }
            }

            if (defineSymbolsChanged)
            {
                SetDefineSymbols(defineSymbols);
            }

            SymbolRequirement.ResetFoundTypes();
        }

        private static bool s_delayCallRemoveRegistered;

        // This is called when ever an asset deleted
        // If the deleted asset include sdk files, then remove all symbols defined by VIU
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            var fullPath = Application.dataPath + "/../" + assetPath;
            var isDir = Directory.Exists(fullPath); // otherwise, removed asset is file
            var reqFileFound = false;

            foreach (var symbolReq in s_symbolReqList)
            {
                if (symbolReq == null || symbolReq.reqFileNames == null) { continue; }

                foreach (var reqFileName in symbolReq.reqFileNames)
                {
                    if (isDir)
                    {
                        var files = Directory.GetFiles(fullPath, reqFileName, SearchOption.AllDirectories);
                        reqFileFound = files != null && files.Length > 0;
                    }
                    else
                    {
                        reqFileFound = Path.GetFileName(fullPath) == reqFileName;
                    }

                    if (reqFileFound)
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

            var defineSymbols = GetDefineSymbols();

            foreach (var symbolReq in s_symbolReqList)
            {
                defineSymbols.Remove(symbolReq.symbol);
            }

            SetDefineSymbols(defineSymbols);
        }

        private static List<string> GetDefineSymbols()
        {
            return new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)).Split(';'));
        }

        private static void SetDefineSymbols(List<string> symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), string.Join(";", symbols.ToArray()));
        }
    }
}