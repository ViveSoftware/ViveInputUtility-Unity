using HTC.Newtonsoft.Json;
using HTC.Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HTC.UPMRegistryTool.Editor.Configs
{
    public class RegistrySettings
    {
        public struct RegistryInfo
        {
            [JsonProperty("name")]
            public string Name;

            [JsonProperty("url")]
            public string Url;

            [JsonProperty("scopes")]
            public IList<string> Scopes;

            public bool Equals(RegistryInfo otherInfo)
            {
                if (Name != otherInfo.Name || Url != otherInfo.Url)
                {
                    return false;
                }

                if (Scopes == null || otherInfo.Scopes == null)
                {
                    return false;
                }

                if (Scopes.Count != otherInfo.Scopes.Count)
                {
                    return false;
                }

                for (int i = 0; i < Scopes.Count; i++)
                {
                    if (Scopes[i] != otherInfo.Scopes[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private const string RESOURCES_PATH = "RegistrySettings";
        private static RegistrySettings PrivateInstance;

        [JsonProperty]
        public string ProjectManifestPath;

        [JsonProperty] 
        public string LicenseResourcePath;

        [JsonProperty]
        public RegistryInfo Registry;

        public string RegistryHost;
        public int RegistryPort;

        public static RegistrySettings Instance()
        {
            if (PrivateInstance == null)
            {
                TextAsset jsonAsset = Resources.Load<TextAsset>(RESOURCES_PATH);
                if (jsonAsset)
                {
                    string settingString = jsonAsset.ToString();
                    PrivateInstance = JsonConvert.DeserializeObject<RegistrySettings>(settingString);
                }
                else
                {
                    Debug.LogErrorFormat("RegistrySettings.json not found. ({0})", RESOURCES_PATH);
                    PrivateInstance = new RegistrySettings();
                }

                PrivateInstance.Init();
            }

            return PrivateInstance;
        }

        public string GetLicenseURL()
        {
            Object fileObj = Resources.Load(LicenseResourcePath);
            string assetPath = AssetDatabase.GetAssetPath(fileObj);
            string fullPath = Path.GetFullPath(assetPath);

            return "file://" + fullPath;
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
