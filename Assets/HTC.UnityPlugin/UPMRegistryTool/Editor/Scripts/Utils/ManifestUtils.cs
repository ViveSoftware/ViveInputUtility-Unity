#if UNITY_2018_1_OR_NEWER
using HTC.UnityPlugin.UPMRegistryTool.Editor.Configs;
using HTC.UnityPlugin.UPMRegistryTool.Editor.Utils.SimpleJSON;
using System.IO;

namespace HTC.UnityPlugin.UPMRegistryTool.Editor.Utils
{
    public static class ManifestUtils
    {
        public const string SCOPED_REGISTRIES_KEY = "scopedRegistries";
        public const int JSON_INDENT_SPACE_COUNT = 2;

        public static bool CheckRegistryExists(RegistryInfo registryInfo)
        {
            JSONObject manifestJson = LoadProjectManifest();
            if (!manifestJson.HasKey(SCOPED_REGISTRIES_KEY))
            {
                return false;
            }

            JSONArray registries = manifestJson[SCOPED_REGISTRIES_KEY].AsArray;
            foreach (JSONNode regNode in registries)
            {
                RegistryInfo regInfo = RegistryInfo.FromJson(regNode.AsObject);
                if (registryInfo.Equals(regInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public static void AddRegistry(RegistryInfo registryInfo)
        {
            RemoveRegistry(registryInfo.Name);

            JSONObject manifestJson = LoadProjectManifest();
            if (!manifestJson.HasKey(SCOPED_REGISTRIES_KEY))
            {
                manifestJson.Add(SCOPED_REGISTRIES_KEY, new JSONArray());
            }

            JSONArray registries = manifestJson[SCOPED_REGISTRIES_KEY].AsArray;
            registries.Add(registryInfo.ToJson());

            Save(manifestJson);
        }

        public static void RemoveRegistry(string registryName)
        {
            JSONObject manifestJson = LoadProjectManifest();
            if (!manifestJson.HasKey(SCOPED_REGISTRIES_KEY))
            {
                return;
            }

            JSONArray registries = manifestJson[SCOPED_REGISTRIES_KEY].AsArray;
            for (int i = registries.Count - 1; i >= 0; i--)
            {
                RegistryInfo reg = RegistryInfo.FromJson(registries[i].AsObject);
                if (reg.Name == registryName)
                {
                    registries.Remove(i);
                }
            }

            Save(manifestJson);
        }

        private static JSONObject LoadProjectManifest()
        {
            string manifestString = File.ReadAllText(RegistryToolSettings.Instance().ProjectManifestPath);
            JSONObject manifestJson = JSONNode.Parse(manifestString).AsObject;

            return manifestJson;
        }

        private static void Save(JSONObject newJson)
        {
            string jsonString = newJson.ToString(JSON_INDENT_SPACE_COUNT);
            File.WriteAllText(RegistryToolSettings.Instance().ProjectManifestPath, jsonString);
        }
    }
} 
#endif