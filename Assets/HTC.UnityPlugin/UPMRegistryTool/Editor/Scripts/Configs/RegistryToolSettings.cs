#if UNITY_2018_1_OR_NEWER
using HTC.UnityPlugin.UPMRegistryTool.Editor.Utils;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HTC.UnityPlugin.UPMRegistryTool.Editor.Configs
{
    public class RegistryToolSettings : ScriptableObject
    {
        private const string RESOURCES_PATH = "RegistryToolSettings";

        private static RegistryToolSettings PrivateInstance;

        public string ProjectManifestPath;
        public RegistryInfo Registry;

        [NonSerialized] public string RegistryHost;
        [NonSerialized] public int RegistryPort;

        public static RegistryToolSettings Instance()
        {
            if (PrivateInstance == null)
            {
                PrivateInstance = Resources.Load<RegistryToolSettings>(RESOURCES_PATH);
                PrivateInstance.Init();
            }

            return PrivateInstance;
        }

        private void Init()
        {
            Match match = Regex.Match(Registry.Url, @"^https?:\/\/(.+?)(?::(\d+))?\/?$");
            RegistryHost = match.Groups[1].Value;

            int port = 0;
            RegistryPort = 80;
            if (int.TryParse(match.Groups[2].Value, out port))
            {
                RegistryPort = port;
            }
        }
    }
}
#endif