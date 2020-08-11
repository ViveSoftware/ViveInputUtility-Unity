#pragma warning disable 0649
using HTC.UnityPlugin.UPMRegistryTool.SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HTC.UnityPlugin.UPMRegistryTool
{
    [CreateAssetMenu(menuName = "RegistryToolSettings", fileName = "RegistryToolSettings")]
    public class RegistryToolSettings : ScriptableObject
    {
        [Serializable]
        public struct ScopedRegistry
        {
            public string name;
            public string url;
            public List<string> scopes;

            public bool Equals(ScopedRegistry otherInfo)
            {
                if (name != otherInfo.name || url != otherInfo.url) { return false; }

                if (scopes == null || otherInfo.scopes == null)
                {
                    if (scopes == null && otherInfo.scopes == null) { return true; }
                    return false;
                }

                if (scopes.Count != otherInfo.scopes.Count) { return false; }

                for (int i = 0, imax = scopes.Count; i < imax; i++)
                {
                    if (scopes[i] != otherInfo.scopes[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [SerializeField]
        public string ProjectManifestPath;
        [SerializeField]
        public string LicenseResourcePath;
        [SerializeField]
        public ScopedRegistry ViveRegistry;

        [NonSerialized]
        public string ViveRegistryHost;
        [NonSerialized]
        public int ViveRegistryPort;

        private const string RESOURCES_PATH = "RegistryToolSettings";
        private static RegistryToolSettings instance;

        public static RegistryToolSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<RegistryToolSettings>(RESOURCES_PATH);

                    if (instance != null)
                    {
                        var match = Regex.Match(instance.ViveRegistry.url, @"^https?:\/\/(.+?)(?::(\d+))?\/?$");
                        instance.ViveRegistryHost = match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : default(string);

                        if (!int.TryParse(match.Groups[2].Value, out instance.ViveRegistryPort)) { instance.ViveRegistryPort = 80; }
                    }
                    else
                    {
                        Debug.LogErrorFormat("RegistrySettings.asset not found. ({0})", RESOURCES_PATH);
                    }
                }

                return instance;
            }
        }

        public static bool IsRegistryExists(ScopedRegistry reg)
        {
            JSONNode manifestNode;
            if (!LoadManifestNode(out manifestNode)) { return false; }

            return IsRegistryExists(manifestNode, reg);
        }

        public static void AddRegistry(ScopedRegistry reg)
        {
            JSONNode manifestNode;
            if (!LoadManifestNode(out manifestNode)) { return; }

            int nameMatchIndex;
            if (!IsRegistryExists(manifestNode, reg, out nameMatchIndex))
            {
                if (nameMatchIndex < 0)
                {
                    AddRegistry(EnsureRegistriesNode(manifestNode), reg);
                }
                else
                {
                    AddRegistryAt(EnsureRegistriesNode(manifestNode), reg, nameMatchIndex);
                }
            }

            SaveManifestNode(manifestNode);
        }

        public static void RemoveRegistry(ScopedRegistry reg)
        {
            JSONNode manifestNode;
            if (!LoadManifestNode(out manifestNode)) { return; }

            int nameMatchIndex;
            IsRegistryExists(manifestNode, reg, out nameMatchIndex);
            if (nameMatchIndex > 0)
            {
                RemoveRegistryAt(manifestNode, nameMatchIndex);
            }

            SaveManifestNode(manifestNode);
        }

        private static bool IsRegistryExists(JSONNode manifestNode, ScopedRegistry reg)
        {
            int i;
            return IsRegistryExists(manifestNode, reg, out i);
        }

        private static bool IsRegistryExists(JSONNode manifestNode, ScopedRegistry reg, out int index)
        {
            index = -1;
            var regsNode = manifestNode["scopedRegistries"];

            if (!regsNode.IsArray) { return false; }
            if (regsNode.Count == 0) { return false; }

            for (int i = 0, imax = regsNode.Count; i < imax; ++i)
            {
                var regNode = regsNode[i];
                var nameNode = regNode["name"];
                var urlNode = regNode["url"];
                var scopesNode = regNode["scopes"];

                if (!nameNode.IsString || nameNode.Value != reg.name) { continue; }
                index = i;

                if (!urlNode.IsString || urlNode.Value != reg.url) { break; }
                if (!scopesNode.IsArray) { break; }
                if (scopesNode.Count != (reg.scopes == null ? 0 : reg.scopes.Count)) { break; }

                var allMatch = true;
                for (int j = scopesNode.Count - 1; j >= 0; --j)
                {
                    var scopeNode = scopesNode[j];
                    if (!scopeNode.IsString) { allMatch = false; break; }
                    if (scopeNode.Value != reg.scopes[j]) { allMatch = false; break; }
                }
                return allMatch;
            }

            return false;
        }

        private static JSONArray EnsureRegistriesNode(JSONNode manifestNode)
        {
            var regsNode = manifestNode["scopedRegistries"];
            if (!regsNode.IsArray)
            {
                manifestNode.Add("scopedRegistries", regsNode = new JSONArray());
            }
            return regsNode.AsArray;
        }

        private static void AddRegistry(JSONArray regsNode, ScopedRegistry reg)
        {
            if (regsNode.Count == 0) { regsNode.Add(new JSONObject()); }
            AddRegistryAt(regsNode, reg, regsNode.Count - 1);
        }

        private static void AddRegistryAt(JSONArray regsNode, ScopedRegistry reg, int index)
        {
            var regNode = regsNode[index];
            var nameNode = regNode["name"];
            var urlNode = regNode["url"];
            var scopesNode = regNode["scopes"];

            if (nameNode.IsString) { nameNode.Value = reg.name; }
            else { regNode.Add("name", new JSONString(reg.name)); }

            if (urlNode.IsString) { urlNode.Value = reg.url; }
            else { regNode.Add("url", new JSONString(reg.url)); }

            if (!scopesNode.IsArray) { regNode.Add("scopes", scopesNode = new JSONArray()); }

            int i = 0;
            foreach (var scope in reg.scopes)
            {
                var scopeNode = scopesNode[i];
                if (scopeNode.IsString) { scopeNode.Value = scope; }
                else { scopesNode[i] = new JSONString(scope); }
                ++i;
            }

            while (scopesNode.Count > i) { scopesNode.Remove(scopesNode.Count - 1); }
        }

        private static void RemoveRegistryAt(JSONNode manifestNode, int index)
        {
            manifestNode["scopedRegistries"].Remove(index);
        }

        private static bool LoadManifestNode(out JSONNode node)
        {
            node = default(JSONNode);

            if (Instance == null) { return false; }

            var json = File.ReadAllText(instance.ProjectManifestPath);
            node = JSON.Parse(json);
            if (!node.IsObject)
            {
                Debug.LogError("Parse pkg manifest from " + instance.ProjectManifestPath + " faild, json=\n" + json, instance);
                return false;
            }

            return true;
        }

        private static void SaveManifestNode(JSONNode node)
        {
            if (Instance == null) { return; }

            File.WriteAllText(instance.ProjectManifestPath, node.ToString(2));
        }
    }
}