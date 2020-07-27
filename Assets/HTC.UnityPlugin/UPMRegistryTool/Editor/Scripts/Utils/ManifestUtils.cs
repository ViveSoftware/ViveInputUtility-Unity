#if UNITY_2018_1_OR_NEWER
using HTC.UPMRegistryTool.Editor.Configs;
using HTC.ViveInputUtility.Newtonsoft.Json;
using HTC.ViveInputUtility.Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace HTC.UPMRegistryTool.Editor.Utils
{
    public static class ManifestUtils
    {
        public static bool CheckRegistryExists(RegistrySettings.RegistryInfo registryInfo)
        {
            JObject manifestJson = LoadProjectManifest();
            if (!manifestJson.ContainsKey("scopedRegistries"))
            {
                return false;
            }

            IList<JToken> registries = (IList<JToken>)manifestJson["scopedRegistries"];
            foreach (JToken regToken in registries)
            {
                RegistrySettings.RegistryInfo regInfo = JsonConvert.DeserializeObject<RegistrySettings.RegistryInfo>(regToken.ToString());
                if (registryInfo.Equals(regInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public static void AddRegistry(RegistrySettings.RegistryInfo registryInfo)
        {
            RemoveRegistry(registryInfo.Name);

            JObject manifestJson = LoadProjectManifest();
            if (!manifestJson.ContainsKey("scopedRegistries"))
            {
                manifestJson.Add("scopedRegistries", new JArray());
            }

            IList<JToken> registries = (IList<JToken>)manifestJson["scopedRegistries"];
            JToken newToken = JToken.Parse(JsonConvert.SerializeObject(registryInfo));
            registries.Add(newToken);

            SaveProjectManifest(manifestJson.ToString());
        }

        public static void RemoveRegistry(string registryName)
        {
            JObject manifestJson = LoadProjectManifest();
            if (!manifestJson.ContainsKey("scopedRegistries"))
            {
                return;
            }

            IList<JToken> registries = (IList<JToken>)manifestJson["scopedRegistries"];
            for (int i = registries.Count - 1; i >= 0; i--)
            {
                JToken registryToken = registries[i];
                RegistrySettings.RegistryInfo registry = JsonConvert.DeserializeObject<RegistrySettings.RegistryInfo>(registryToken.ToString());
                if (registry.Name == registryName)
                {
                    registries.RemoveAt(i);
                }
            }

            SaveProjectManifest(manifestJson.ToString());
        }

        private static JObject LoadProjectManifest()
        {
            string manifestString = File.ReadAllText(RegistrySettings.Instance().ProjectManifestPath);
            JObject manifestJson = JObject.Parse(manifestString);

            return manifestJson;
        }

        private static void SaveProjectManifest(string content)
        {
            File.WriteAllText(RegistrySettings.Instance().ProjectManifestPath, content);
        }
    }
} 
#endif