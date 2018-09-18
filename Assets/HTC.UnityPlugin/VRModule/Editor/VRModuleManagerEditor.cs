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
    // This script manage define symbols used by VIU
    public class VRModuleManagerEditor : UnityEditor.AssetModificationProcessor
#if UNITY_2017_1_OR_NEWER
        , UnityEditor.Build.IActiveBuildTargetChanged
#endif
    {
        private class SymbolRequirement
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

        private static List<SymbolRequirement> s_symbolReqList;

        static VRModuleManagerEditor()
        {
            s_symbolReqList = new List<SymbolRequirement>();

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_PLUGIN",
                reqTypeNames = new string[] { "HTC.UnityPlugin.Vive.ViveInput" },
                reqFileNames = new string[] { "ViveInput.cs", "VRModuleManagerEditor.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR",
                reqTypeNames = new string[] { "SteamVR" },
                reqFileNames = new string[] { "SteamVR.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_1_1",
                reqTypeNames = new string[] { "SteamVR_Utils+Event" },
                reqFileNames = new string[] { "SteamVR_Utils.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_0_OR_NEWER",
                reqTypeNames = new string[] { "SteamVR_Events" },
                reqFileNames = new string[] { "SteamVR_Events.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_1_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                         typeName = "SteamVR_Events",
                         name = "System",
                         argTypeNames = new string[] { "Valve.VR.EVREventType" },
                         bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
                reqFileNames = new string[] { "SteamVR_Events.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_2_OR_NEWER",
                reqFields = new SymbolRequirement.ReqFieldInfo[]
                {
                    new SymbolRequirement.ReqFieldInfo()
                    {
                        typeName = "SteamVR_ExternalCamera+Config",
                        name = "r",
                        bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                reqFileNames = new string[] { "SteamVR_ExternalCamera.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_3_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                         typeName = "Valve.VR.CVRSystem",
                         name = "IsInputAvailable",
                         bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                reqFileNames = new string[] { "openvr_api.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR",
                reqTypeNames = new string[] { "OVRInput" },
                reqFileNames = new string[] { "OVRInput.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_GOOGLEVR",
                reqTypeNames = new string[] { "GvrUnitySdkVersion" },
                reqFileNames = new string[] { "GvrUnitySdkVersion.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR",
                reqTypeNames = new string[] { "WaveVR" },
                reqFileNames = new string[] { "WaveVR.cs" },
            });

            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_2_0_32_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                        typeName = "wvr.Interop",
                        name = "WVR_GetInputDeviceState",
                        argTypeNames = new string[]
                        {
                            "wvr.WVR_DeviceType",
                            "System.UInt32",
                            "System.UInt32&",
                            "System.UInt32&",
                            "wvr.WVR_AnalogState_t[]",
                            "System.UInt32",
                        },
                        bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
                reqFileNames = new string[] { "wvr.cs" },
            });
            
            s_symbolReqList.Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_2_1_0_OR_NEWER",
                reqTypeNames = new string[] { "wvr.WVR_InputId" },
                validateFunc = (req) =>
                {
                    Type wvrInputIdType;
                    if (SymbolRequirement.s_foundTypes.TryGetValue("wvr.WVR_InputId", out wvrInputIdType) && wvrInputIdType.IsEnum)
                    {
                        if (Enum.IsDefined(wvrInputIdType, "WVR_InputId_Alias1_Digital_Trigger"))
                        {
                            return true;
                        }
                    }
                    return false;
                },
                reqFileNames = new string[] { "wvr.cs" },
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