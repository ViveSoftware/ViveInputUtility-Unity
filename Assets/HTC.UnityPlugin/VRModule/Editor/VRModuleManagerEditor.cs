//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HTC.UnityPlugin.Vive;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal.VR;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

#if UNITY_2017_3_OR_NEWER
using UnityEditor.Compilation;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    // This script manage define symbols used by VIU
    public class VRModuleManagerEditor : AssetPostprocessor
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
            public string[] symbols = null;
            public string[] reqTypeNames = null;
            public string[] reqAnyTypeNames = null;
            public string[] reqFileNames = null;
            public string[] reqAnyFileNames = null;
            public ReqFieldInfo[] reqFields = null;
            public ReqFieldInfo[] reqAnyFields = null;
            public ReqMethodInfo[] reqMethods = null;
            public ReqMethodInfo[] reqAnyMethods = null;
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

                if (reqAnyFields != null)
                {
                    foreach (var field in reqAnyFields)
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

                if (reqAnyMethods != null)
                {
                    foreach (var method in reqAnyMethods)
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

                if (reqFileNames != null)
                {
                    foreach (var requiredFile in reqFileNames)
                    {
                        if (!DoesFileExist(requiredFile))
                        {
                            return false;
                        }
                    }
                }

                if (reqAnyFileNames != null)
                {
                    var found = false;

                    foreach (var requiredFile in reqAnyFileNames)
                    {
                        var files = Directory.GetFiles(Application.dataPath, requiredFile, SearchOption.AllDirectories);
                        if (files != null && files.Length > 0)
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

                if (reqAnyFields != null)
                {
                    var found = false;

                    foreach (var field in reqAnyFields)
                    {
                        Type type;
                        if (!s_foundTypes.TryGetValue(field.typeName, out type)) { continue; }
                        if (type.GetField(field.name, field.bindingAttr) == null) { continue; }

                        found = true;
                        break;
                    }

                    if (!found) { return false; }
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

                if (reqAnyMethods != null)
                {
                    var found = false;

                    foreach (var method in reqAnyMethods)
                    {
                        Type type;
                        if (!s_foundTypes.TryGetValue(method.typeName, out type)) { continue; }

                        if (method.argTypeNames == null)
                        {
                            continue;
                        }

                        bool isAllArgTypesFound = true;
                        var argTypes = new Type[method.argTypeNames.Length];
                        for (int i = argTypes.Length - 1; i >= 0; --i)
                        {
                            if (!s_foundTypes.TryGetValue(method.argTypeNames[i], out argTypes[i]))
                            {
                                isAllArgTypesFound = false;
                                break;
                            }
                        }

                        if (!isAllArgTypesFound)
                        {
                            continue;
                        }

                        if (type.GetMethod(method.name, method.bindingAttr, null, CallingConventions.Any, argTypes, method.argModifiers ?? new ParameterModifier[0]) == null) { continue; }

                        found = true;
                        break;
                    }

                    if (!found) { return false; }
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
        private static HashSet<string> s_referencedAssemblyNameSet;

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
                validateFunc = (req) => true,
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_PACKAGE",
                validateFunc = (req) => VIUSettingsEditor.PackageManagerHelper.IsPackageInList(VIUSettingsEditor.VIUPackageName),
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_SIUMULATOR_SUPPORT",
                validateFunc = (req) => Vive.VIUSettingsEditor.supportSimulator,
            });

            // Obsolete symbol, will be removed in all condition
            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_EXTERNAL_CAMERA_SWITCH",
                validateFunc = (req) => false,
            });

            // Obsolete symbol, will be removed in all condition
            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_BINDING_INTERFACE_SWITCH",
                validateFunc = (req) => false,
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
            if (!s_isUpdatingScriptingDefineSymbols)
            {
                s_isUpdatingScriptingDefineSymbols = true;
                EditorApplication.update += DoUpdateScriptingDefineSymbols;
            }
        }

        // From UnityEditor.AssetPostprocessor
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in deletedAssets)
            {
                string deletedFileName = Path.GetFileName(assetPath);
                bool isFound = s_symbolReqList.Exists((req) =>
                {
                    if (req == null || req.reqFileNames == null)
                    {
                        return false;
                    }

                    foreach (string fileName in req.reqFileNames)
                    {
                        if (fileName == deletedFileName)
                        {
                            return true;
                        }
                    }

                    return false;
                });

                if (isFound)
                {
                    if (!s_delayCallRemoveRegistered)
                    {
                        s_delayCallRemoveRegistered = true;
                        EditorApplication.delayCall += RemoveAllVIUSymbols;
                    }
                    break;
                }
            }
        }

        private static bool s_isUpdatingScriptingDefineSymbols = false;
        private static void DoUpdateScriptingDefineSymbols()
        {
            if (EditorApplication.isPlaying) { EditorApplication.update -= DoUpdateScriptingDefineSymbols; return; }

            // some symbolRequirement depends on installed packages (only works when UNITY_2018_1_OR_NEWER)
            Vive.VIUSettingsEditor.PackageManagerHelper.PreparePackageList();

            if (Vive.VIUSettingsEditor.PackageManagerHelper.isPreparingList) { return; }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsReferenced(assembly))
                {
                    continue;
                }

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
            var validSymbols = new HashSet<string>();
            var invalidSymbols = new HashSet<string>();

            foreach (var symbolReq in s_symbolReqList)
            {
                if (symbolReq.Validate())
                {
                    if (!string.IsNullOrEmpty(symbolReq.symbol))
                    {
                        invalidSymbols.Remove(symbolReq.symbol);
                        validSymbols.Add(symbolReq.symbol);
                    }
                    if (symbolReq.symbols != null)
                    {
                        foreach (var symbol in symbolReq.symbols)
                        {
                            if (!string.IsNullOrEmpty(symbol))
                            {
                                invalidSymbols.Remove(symbol);
                                validSymbols.Add(symbol);
                            }
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(symbolReq.symbol) && !validSymbols.Contains(symbolReq.symbol))
                    {
                        invalidSymbols.Add(symbolReq.symbol);
                    }
                    if (symbolReq.symbols != null)
                    {
                        foreach (var symbol in symbolReq.symbols)
                        {
                            if (!string.IsNullOrEmpty(symbol) && !validSymbols.Contains(symbol))
                            {
                                invalidSymbols.Add(symbol);
                            }
                        }
                    }
                }
            }

            foreach (var symbol in invalidSymbols)
            {
                if (defineSymbols.RemoveAll((s) => s == symbol) > 0)
                {
                    defineSymbolsChanged = true;
                }
            }

            foreach (var symbol in validSymbols)
            {
                if (!defineSymbols.Contains(symbol))
                {
                    defineSymbols.Add(symbol);
                    defineSymbolsChanged = true;
                }
            }

            if (defineSymbolsChanged)
            {
                SetDefineSymbols(defineSymbols);
            }

            SymbolRequirement.ResetFoundTypes();

            s_isUpdatingScriptingDefineSymbols = false;
            EditorApplication.update -= DoUpdateScriptingDefineSymbols;
        }

        private static bool s_delayCallRemoveRegistered;

        private static void RemoveAllVIUSymbols()
        {
            s_delayCallRemoveRegistered = false;
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

        private static bool IsReferenced(Assembly assembly)
        {
            return GetReferencedAssemblyNameSet().Contains(assembly.GetName().Name);
        }

        private static HashSet<string> GetReferencedAssemblyNameSet()
        {
            if (s_referencedAssemblyNameSet != null)
            {
                return s_referencedAssemblyNameSet;
            }

            s_referencedAssemblyNameSet = new HashSet<string>();
            Assembly playerAssembly = typeof(VRModule).Assembly;
            Assembly editorAssembly = typeof(VRModuleManagerEditor).Assembly;

            // C# player referenced assemblies
            foreach (AssemblyName asmName in playerAssembly.GetReferencedAssemblies())
            {
                s_referencedAssemblyNameSet.Add(asmName.Name);
            }

            // C# editor referenced assemblies
            foreach (AssemblyName asmName in editorAssembly.GetReferencedAssemblies())
            {
                s_referencedAssemblyNameSet.Add(asmName.Name);
            }

#if UNITY_2018_1_OR_NEWER
            // Unity player referenced assemblies
            UnityEditor.Compilation.Assembly playerUnityAsm = FindUnityAssembly(playerAssembly.GetName().Name, AssembliesType.Player);
            if (playerUnityAsm != null)
            {
                foreach (UnityEditor.Compilation.Assembly asm in playerUnityAsm.assemblyReferences)
                {
                    s_referencedAssemblyNameSet.Add(asm.name);
                }
            }
            else
            {
                Debug.LogWarning("Player assembly not found.");
            }

            // Unity editor referenced assemblies
            UnityEditor.Compilation.Assembly editorUnityAsm = FindUnityAssembly(editorAssembly.GetName().Name, AssembliesType.Editor);
            if (editorUnityAsm != null)
            {
                foreach (UnityEditor.Compilation.Assembly asm in editorUnityAsm.assemblyReferences)
                {
                    s_referencedAssemblyNameSet.Add(asm.name);
                }
            }
            else
            {
                Debug.LogWarning("Editor assembly not found.");
            }
#elif UNITY_2017_3_OR_NEWER
            UnityEditor.Compilation.Assembly[] assemblies = CompilationPipeline.GetAssemblies();
            foreach (UnityEditor.Compilation.Assembly asm in assemblies)
            {
                s_referencedAssemblyNameSet.Add(asm.name);
            }
#endif

            return s_referencedAssemblyNameSet;
        }

#if UNITY_2018_1_OR_NEWER
        private static UnityEditor.Compilation.Assembly FindUnityAssembly(string name, AssembliesType type)
        {
            UnityEditor.Compilation.Assembly foundAssembly = null;
            UnityEditor.Compilation.Assembly[] assemblies = CompilationPipeline.GetAssemblies(type);
            foreach (UnityEditor.Compilation.Assembly asm in assemblies)
            {
                if (asm.name == name)
                {
                    foundAssembly = asm;
                    break;
                }
            }

            return foundAssembly;
        }
#endif

        private static bool DoesFileExist(string fileName)
        {
            string[] fileNamesInAsset = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories);
            if (fileNamesInAsset != null && fileNamesInAsset.Length > 0)
            {
                return true;
            }
#if UNITY_2018_1_OR_NEWER
            PackageCollection packages = VIUSettingsEditor.PackageManagerHelper.GetPackageList();
            foreach (UnityEditor.PackageManager.PackageInfo package in packages)
            {
                if (package == null)
                {
                    continue;
                }

                if (package.source == PackageSource.BuiltIn)
                {
                    continue;
                }

                var resolvedPath = package.resolvedPath.Trim();
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    continue;
                }

                string[] fileNamesInPackage = Directory.GetFiles(resolvedPath, fileName, SearchOption.AllDirectories);
                if (fileNamesInPackage != null && fileNamesInPackage.Length > 0)
                {
                    return true;
                }
            }
#endif
            return false;
        }
    }
}