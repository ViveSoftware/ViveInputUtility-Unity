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
            MergeActionSets(src, dst);
            MergeActions(src, dst);
            MergeLocalization(src, dst);
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

        public static int MergeActionSets(SimpleActionFile src, SimpleActionFile dst)
        {
            int count = 0;

            foreach (var newSet in src.action_sets)
            {
                if (dst.action_sets.Any(setInCurrent => newSet.name == setInCurrent.name) == false)
                {
                    ActionSet actionset = new ActionSet();
                    actionset.name = newSet.name;
                    actionset.usage = newSet.usage;
                    dst.action_sets.Add(actionset);
                    count++;
                }
            }

            return count;
        }

        public static int MergeActions(SimpleActionFile src, SimpleActionFile dst)
        {
            int count = 0;

            foreach (var newAction in src.actions)
            {
                if (dst.actions.Any(actionInCurrent => newAction.name == actionInCurrent.name) == false)
                {
                    Action action = new Action();
                    action.name = newAction.name;
                    action.type = newAction.type;
                    action.scope = newAction.scope;
                    action.skeleton = newAction.skeleton;
                    action.requirement = newAction.requirement;
                    dst.actions.Add(action);
                    count++;
                }
                else
                {
                    Action existingAction = dst.actions.First(actionInCurrent => newAction.name == actionInCurrent.name);

                    //todo: better merge? should we overwrite?
                    existingAction.type = newAction.type;
                    existingAction.scope = newAction.scope;
                    existingAction.skeleton = newAction.skeleton;
                    existingAction.requirement = newAction.requirement;
                }
            }

            return count;
        }

        public static int MergeLocalization(SimpleActionFile src, SimpleActionFile dst)
        {
            int count = 0;

            foreach (var newLocalDictionary in src.localization)
            {
                string newLanguage = FindLanguageInDictionary(newLocalDictionary);

                if (string.IsNullOrEmpty(newLanguage))
                {
                    Debug.LogError("Actions file is missing a language tag");
                    continue;
                }

                int currentLanguage = -1;
                for (int currentLanguageIndex = 0; currentLanguageIndex < src.localization.Count; currentLanguageIndex++)
                {
                    string language = FindLanguageInDictionary(src.localization[currentLanguageIndex]);
                    if (newLanguage == language)
                    {
                        currentLanguage = currentLanguageIndex;
                        break;
                    }
                }

                if (currentLanguage == -1)
                {
                    Dictionary<string, string> newDictionary = new Dictionary<string, string>();
                    foreach (var element in newLocalDictionary)
                    {
                        newDictionary.Add(element.Key, element.Value);
                        count++;
                    }

                    src.localization.Add(newDictionary);
                }
                else
                {
                    foreach (var element in newLocalDictionary)
                    {
                        Dictionary<string, string> currentDictionary = dst.localization[currentLanguage];
                        bool exists = currentDictionary.Any(inCurrent => inCurrent.Key == element.Key);

                        if (exists)
                        {
                            //todo: should we overwrite?
                            currentDictionary[element.Key] = element.Value;
                        }
                        else
                        {
                            currentDictionary.Add(element.Key, element.Value);
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private static string FindLanguageInDictionary(Dictionary<string, string> dictionary)
        {
            foreach (var localizationMember in dictionary)
            {
                if (localizationMember.Key == Localization.languageTagKeyName)
                    return localizationMember.Value;
            }

            return null;
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

        [Serializable]
        public class Localization
        {
            public const string languageTagKeyName = "language_tag";
            public string language;
            public Dictionary<string, string> items = new Dictionary<string, string>();
        }
    }
}
#endif