//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class VRModuleManagerEditor : UnityEditor.AssetModificationProcessor
    {
        private struct VrSdkInfo
        {
            public string scriptingDefineSymble;
            public string requiredClassName;
            public string requiredScriptFileName;

            public VrSdkInfo(string scriptingDefineSymble, string requiredClassName, string requiredScriptFileName)
            {
                this.scriptingDefineSymble = scriptingDefineSymble;
                this.requiredClassName = requiredClassName;
                this.requiredScriptFileName = requiredScriptFileName;
            }
        }

        private static List<VrSdkInfo> s_supportedSdkInfoList;

        static VRModuleManagerEditor()
        {
            s_supportedSdkInfoList = new List<VrSdkInfo>();
            s_supportedSdkInfoList.Add(new VrSdkInfo("VIU_PLUGIN", "HTC.UnityPlugin.Vive.ViveInput", "ViveInput.cs"));
            s_supportedSdkInfoList.Add(new VrSdkInfo("VIU_STEAMVR", "SteamVR", "SteamVR.cs"));
            s_supportedSdkInfoList.Add(new VrSdkInfo("VIU_OCULUSVR", "OVRInput", "OVRInput.cs"));
        }

        [DidReloadScripts]
        private static void UpdateScriptingDefineSymbols()
        {
            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var symbolsList = new List<string>(scriptingDefineSymbols.Split(';'));

            for (int i = 0, imax = s_supportedSdkInfoList.Count; i < imax; ++i)
            {
                if (ClassFoundInAssemblies(s_supportedSdkInfoList[i].requiredClassName))
                {
                    if (!symbolsList.Contains(s_supportedSdkInfoList[i].scriptingDefineSymble))
                    {
                        symbolsList.Add(s_supportedSdkInfoList[i].scriptingDefineSymble);
                    }
                }
                else
                {
                    symbolsList.RemoveAll((symbol) => symbol == s_supportedSdkInfoList[i].scriptingDefineSymble);
                }
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", symbolsList.ToArray()));
        }

        private static bool ClassFoundInAssemblies(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(className) != null) { return true; }
            }

            return false;
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

            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var symbolsList = new List<string>(scriptingDefineSymbols.Split(';'));

            var removed = symbolsList.RemoveAll((symbol) => s_symbolsToRemove.Contains(symbol)) > 0;

            s_symbolsToRemove.Clear();

            if (removed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", symbolsList.ToArray()));
            }
        }
    }
}