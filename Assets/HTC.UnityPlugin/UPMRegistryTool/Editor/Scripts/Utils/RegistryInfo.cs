#if UNITY_2018_1_OR_NEWER
using System;
using HTC.UnityPlugin.UPMRegistryTool.Editor.Utils.SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace HTC.UnityPlugin.UPMRegistryTool.Editor.Utils
{
    [Serializable]
    public struct RegistryInfo
    {
        public const string NAME_KEY = "name";
        public const string URL_KEY = "url";
        public const string SCOPES_KEY = "scopes";

        public string Name;
        public string Url;
        public List<string> Scopes;

        public static RegistryInfo FromJson(JSONObject json)
        {
            RegistryInfo info = new RegistryInfo
            {
                Name = json[NAME_KEY],
                Url = json[URL_KEY],
                Scopes = new List<string>(),
            };

            foreach (JSONNode node in json[SCOPES_KEY].AsArray)
            {
                info.Scopes.Add(node);
            }

            return info;
        }

        public static JSONObject ToJson(RegistryInfo info)
        {
            JSONObject json = new JSONObject();
            json[NAME_KEY] = info.Name;
            json[URL_KEY] = info.Url;

            JSONArray scopes = new JSONArray();
            foreach (string scope in info.Scopes)
            {
                scopes.Add(new JSONString(scope));
            }

            json[SCOPES_KEY] = scopes;

            return json;
        }

        public JSONObject ToJson()
        {
            return ToJson(this);
        }

        public bool Equals(RegistryInfo other)
        {
            return Name == other.Name && Url == other.Url && Scopes.SequenceEqual(other.Scopes);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RegistryInfo))
            {
                return false;
            }

            RegistryInfo other = (RegistryInfo) obj;
            if (!Equals(other))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Scopes != null ? Scopes.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
} 
#endif