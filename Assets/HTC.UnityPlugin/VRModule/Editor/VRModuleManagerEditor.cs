//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class VRModuleManagerEditor
    {
        private struct VrSdkInfo
        {
            public string scriptingDefineSymble;
            public string requiredClassName;

            public VrSdkInfo(string scriptingDefineSymble, string requiredClassName)
            {
                this.scriptingDefineSymble = scriptingDefineSymble;
                this.requiredClassName = requiredClassName;
            }
        }

        private static List<VrSdkInfo> s_supportedSdkInfoList;

        static VRModuleManagerEditor()
        {
            s_supportedSdkInfoList = new List<VrSdkInfo>();
            s_supportedSdkInfoList.Add(new VrSdkInfo("VIU_STEAMVR", "SteamVR"));
            s_supportedSdkInfoList.Add(new VrSdkInfo("VIU_OCULUSVR", "OVRInput"));
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
    }
}