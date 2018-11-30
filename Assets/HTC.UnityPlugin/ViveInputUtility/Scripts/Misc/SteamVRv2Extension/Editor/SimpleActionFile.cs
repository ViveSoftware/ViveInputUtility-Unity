//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR_2_1_0_OR_NEWER
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;
using System.Linq;

namespace HTC.UnityPlugin.Vive.SteamVRv2Extension
{
    [Serializable]
    public class SimpleActionFile
    {
        private static Dictionary<string, DateTime> s_fileLastModTime;
        private static Dictionary<string, SimpleActionFile> s_fileCache;

        public List<Action> actions = new List<Action>();
        public List<ActionSet> action_sets = new List<ActionSet>();
        public List<DefaultBinding> default_bindings = new List<DefaultBinding>();
        public List<Dictionary<string, string>> localization = new List<Dictionary<string, string>>();

        [JsonIgnore]
        public bool verified;
        [JsonIgnore]
        public string fullPath;

        private Dictionary<string, Action> m_actionTable;
        private Dictionary<string, ActionSet> m_actionSetTable;
        private Dictionary<string, Dictionary<string, string>> m_localizationTable;

        public static bool TryLoad(string fullPath, out SimpleActionFile actionFile)
        {
            DateTime cachedLastModTime;
            if (s_fileCache != null && s_fileLastModTime != null && s_fileLastModTime.TryGetValue(fullPath, out cachedLastModTime))
            {
                if (!File.Exists(fullPath))
                {
                    s_fileLastModTime.Remove(fullPath);
                    s_fileCache.Remove(fullPath);
                }
                else
                {
                    var lastModTime = File.GetLastWriteTime(fullPath);
                    if (lastModTime == cachedLastModTime && s_fileCache.TryGetValue(fullPath, out actionFile) && actionFile != null)
                    {
                        return true;
                    }
                }
            }

            if (!File.Exists(fullPath))
            {
                actionFile = null;
                return false;
            }

            actionFile = JsonConvert.DeserializeObject<SimpleActionFile>(File.ReadAllText(fullPath));

            actionFile.fullPath = fullPath;

            actionFile.m_actionTable = new Dictionary<string, Action>();
            foreach (var action in actionFile.actions)
            {
                if (actionFile.m_actionTable.ContainsKey(action.name))
                {
                    Debug.LogWarning("Duplicate action(" + action.name + ") found in " + fullPath);
                }
                else
                {
                    actionFile.m_actionTable.Add(action.name, action);
                }
            }

            actionFile.m_actionSetTable = new Dictionary<string, ActionSet>();
            foreach (var actionSet in actionFile.action_sets)
            {
                if (actionFile.m_actionSetTable.ContainsKey(actionSet.name))
                {
                    Debug.LogWarning("Duplicate actionSet(" + actionSet.name + ") found in " + fullPath);
                }
                else
                {
                    actionFile.m_actionSetTable.Add(actionSet.name, actionSet);
                }
            }

            actionFile.m_localizationTable = new Dictionary<string, Dictionary<string, string>>();
            foreach (var loc in actionFile.localization)
            {
                string language;
                if (loc.TryGetValue("language_tag", out language) && !string.IsNullOrEmpty(language))
                {
                    if (actionFile.m_localizationTable.ContainsKey(language))
                    {
                        Debug.LogWarning("Duplicate lanquage(" + language + ") found in " + fullPath);
                    }
                    else
                    {
                        actionFile.m_localizationTable.Add(language, loc);
                    }
                }
            }

            if (s_fileLastModTime == null) { s_fileLastModTime = new Dictionary<string, DateTime>(); }
            if (s_fileCache == null) { s_fileCache = new Dictionary<string, SimpleActionFile>(); }

            s_fileLastModTime[fullPath] = File.GetLastWriteTime(fullPath);
            s_fileCache[fullPath] = actionFile;
            return true;
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            File.WriteAllText(fullPath, json);

            if (s_fileLastModTime == null) { s_fileLastModTime = new Dictionary<string, DateTime>(); }
            if (s_fileCache == null) { s_fileCache = new Dictionary<string, SimpleActionFile>(); }

            s_fileLastModTime[fullPath] = File.GetLastWriteTime(fullPath);
            s_fileCache[fullPath] = this;
        }

        public static void Merge(SimpleActionFile src, SimpleActionFile dst)
        {
            // merge localization
            // merge action set
            // merge action
        }

        public static bool Includes(SimpleActionFile first, SimpleActionFile second, out string falseReason)
        {
            foreach (var pair in second.m_actionTable)
            {
                var actionName = pair.Key;
                var secondAction = pair.Value;
                Action firstAction;
                if (!first.m_actionTable.TryGetValue(actionName, out firstAction))
                {
                    falseReason = actionName + " not found";
                    return false;
                }

                if (firstAction.type != secondAction.type)
                {
                    falseReason = actionName + ".type should be " + secondAction.type + " instead of " + firstAction.type;
                    return false;
                }
            }

            foreach (var pair in second.m_actionSetTable)
            {
                var actionSetName = pair.Key;
                var secondActionSet = pair.Value;
                ActionSet firstActionSet;
                if (!first.m_actionSetTable.TryGetValue(actionSetName, out firstActionSet))
                {
                    falseReason = actionSetName + " not found";
                    return false;
                }

                if (firstActionSet.usage != secondActionSet.usage)
                {
                    falseReason = actionSetName + ".usage should be " + secondActionSet.usage + " instead of " + firstActionSet.usage;
                    return false;
                }
            }

            foreach (var pair in second.m_localizationTable)
            {
                var language = pair.Key;
                var secondLocTbl = pair.Value;
                Dictionary<string, string> firstTbl;
                if (!first.m_localizationTable.TryGetValue(language, out firstTbl))
                {
                    falseReason = language + " not found";
                    return false;
                }

                foreach (var locItemPair in secondLocTbl)
                {
                    var locItemKey = locItemPair.Key;
                    var secondLocItemValue = locItemPair.Value;
                    string firstItemValue;
                    if (!firstTbl.TryGetValue(locItemKey, out firstItemValue))
                    {
                        falseReason = language + "." + locItemKey + " not found";
                        return false;
                    }
                    if (firstItemValue != secondLocItemValue)
                    {
                        falseReason = language + "." + locItemKey + " should be " + secondLocItemValue + " instead of " + firstItemValue;
                        return false;
                    }
                }
            }

            falseReason = string.Empty;
            return true;
        }

        [Serializable]
        public class Action
        {
            public string name;
            public string type;
            public string scope;
            public string skeleton;
            public string requirement;
        }

        [Serializable]
        public class ActionSet
        {
            public string name;
            public string usage;
        }

        [Serializable]
        public class DefaultBinding
        {
            public string name;
            public string usage;
        }
    }
}
#endif