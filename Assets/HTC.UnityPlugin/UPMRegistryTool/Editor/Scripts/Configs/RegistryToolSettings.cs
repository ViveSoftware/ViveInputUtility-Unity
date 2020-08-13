using HTC.UnityPlugin.UPMRegistryTool.Editor.Utils;
using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.UPMRegistryTool.Editor.Configs
{
    [CreateAssetMenu(fileName = "RegistryToolSettings", menuName = "TEST")]
    public class RegistryToolSettings : ScriptableObject
    {
        private const string RESOURCES_PATH = "RegistryToolSettings";

        private static RegistryToolSettings PrivateInstance;

        public string ProjectManifestPath;
        public bool AutoCheckEnabled = true;
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

        public void SetAutoCheckEnabled(bool enabled)
        {
            AutoCheckEnabled = enabled;
            EditorUtility.SetDirty(this);
        }

        private void Init()
        {
            Match match = Regex.Match(Registry.Url, @"^https?:\/\/(.+?)(?::(\d+))?\/?$");
            RegistryHost = match.Groups[1].Value;

            RegistryPort = 80;
            if (int.TryParse(match.Groups[2].Value, out int port))
            {
                RegistryPort = port;
            }
        }
    }
}
