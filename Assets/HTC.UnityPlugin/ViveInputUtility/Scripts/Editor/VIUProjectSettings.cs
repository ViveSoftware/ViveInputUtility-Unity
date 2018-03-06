//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class VIUProjectSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        private static VIUProjectSettings s_instance = null;
        private static string s_defaultAssetPath;

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
                    var ms = MonoScript.FromScriptableObject(CreateInstance<VIUProjectSettings>());
                    var msPath = AssetDatabase.GetAssetPath(ms);
                    s_defaultAssetPath = System.IO.Path.ChangeExtension(msPath, "asset");
                }

                return s_defaultAssetPath;
            }
        }

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
                path = defaultAssetPath;
            }

            if ((s_instance = AssetDatabase.LoadAssetAtPath<VIUProjectSettings>(path)) == null)
            {
                s_instance = CreateInstance<VIUProjectSettings>();
            }
        }

        public static void Save(string path = null)
        {
            if (path == null)
            {
                path = defaultAssetPath;
            }

            if (s_instance == null)
            {
                Load(path);
            }

            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(s_instance)))
            {
                return;
            }

            AssetDatabase.CreateAsset(s_instance, path);
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