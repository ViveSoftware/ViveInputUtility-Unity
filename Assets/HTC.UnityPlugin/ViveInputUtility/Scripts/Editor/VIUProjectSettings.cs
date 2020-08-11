//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class VIUProjectSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        private const string DEFAULT_ASSET_PATH = "Assets/VIUSettings/Editor/Resources/VIUProjectSettings.asset";
        private const string DEFAULT_RESOURCES_PATH = "VIUProjectSettings";

        private static VIUProjectSettings s_instance = null;
        private static string s_defaultAssetPath;
        private static string s_partialActionDirPath;

        public bool HTCRegistryLicenseAccepted
        {
            get
            {
                return m_HTCRegistryLicenseAccepted;
            }
            set
            {
                m_HTCRegistryLicenseAccepted = value;
                EditorUtility.SetDirty(this);
            }
        }

        [SerializeField] 
        private bool m_HTCRegistryLicenseAccepted;

        [SerializeField]
        private List<string> m_ignoreKeys;

        private HashSet<string> m_ignoreKeySet;
        private bool m_isDirty;

        public static VIUProjectSettings Instance
        {
            get
            {
                if (s_instance == null)
                {
                    Load();
                }

                return s_instance;
            }
        }

        public static string defaultAssetPath
        {
            get
            {
                if (s_defaultAssetPath == null)
                {
                    s_defaultAssetPath = DEFAULT_ASSET_PATH;
                }

                return s_defaultAssetPath;
            }
        }

        public static string partialActionDirPath
        {
            get
            {
                if (string.IsNullOrEmpty(s_partialActionDirPath))
                {
                    MonoScript script = MonoScript.FromScriptableObject(Instance);
                    string path = AssetDatabase.GetAssetPath(script);
                    s_partialActionDirPath = Path.GetFullPath(Path.GetDirectoryName(path) + "/../Misc/SteamVRExtension/PartialInputBindings");
                }

                return s_partialActionDirPath;
            }
        }

        public static string partialActionFileName { get { return "actions.json"; } }

        public static bool hasChanged { get { return Instance.m_isDirty; } }

        public void OnBeforeSerialize()
        {
            if (m_isDirty)
            {
                if (m_ignoreKeySet != null && m_ignoreKeySet.Count > 0)
                {
                    if (m_ignoreKeys == null) { m_ignoreKeys = new List<string>(); }
                    m_ignoreKeys.Clear();
                    m_ignoreKeys.AddRange(m_ignoreKeySet);
                }

                EditorUtility.SetDirty(this);

                m_isDirty = false;
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_ignoreKeySet == null) { m_ignoreKeySet = new HashSet<string>(); }
            m_ignoreKeySet.Clear();

            if (m_ignoreKeys != null && m_ignoreKeys.Count > 0)
            {
                for (int i = 0, imax = m_ignoreKeys.Count; i < imax; ++i)
                {
                    if (!string.IsNullOrEmpty(m_ignoreKeys[i]))
                    {
                        m_ignoreKeySet.Add(m_ignoreKeys[i]);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        public static void Load(string path = null)
        {
            if (path == null)
            {
                path = DEFAULT_RESOURCES_PATH;
            }

            if ((s_instance = Resources.Load<VIUProjectSettings>(DEFAULT_RESOURCES_PATH)) == null)
            {
                s_instance = CreateInstance<VIUProjectSettings>();
            }
        }

        public static void Save(string path = null)
        {
            if (path == null)
            {
                path = AssetDatabase.GetAssetPath(Instance);
            }

            if (!string.IsNullOrEmpty(path))
            {
                return;
            }

            path = defaultAssetPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(Instance, path);
        }

        public static bool AddIgnoreKey(string key)
        {
            if (Instance.m_ignoreKeySet == null) { Instance.m_ignoreKeySet = new HashSet<string>(); }
            var changed = Instance.m_ignoreKeySet.Add(key);
            if (changed) { Instance.m_isDirty = true; }
            return changed;
        }

        public static bool RemoveIgnoreKey(string key)
        {
            var changed = Instance.m_ignoreKeySet == null ? false : Instance.m_ignoreKeySet.Remove(key);
            if (changed) { Instance.m_isDirty = true; }
            return changed;
        }

        public static bool HasIgnoreKey(string key)
        {
            return Instance.m_ignoreKeySet == null ? false : Instance.m_ignoreKeySet.Contains(key);
        }
    }
}