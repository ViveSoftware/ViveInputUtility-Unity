//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR_2_0_0_OR_NEWER
using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

namespace HTC.UnityPlugin.Vive.SteamVRExtension
{
    [Serializable]
    public class VIUSteamVRActionFile : VIUSteamVRLoadJsonFileBase<VIUSteamVRActionFile>
    {
        public List<Action> actions = new List<Action>();
        public List<ActionSet> action_sets = new List<ActionSet>();
        public List<DefaultBinding> default_bindings = new List<DefaultBinding>();
        public List<Localization> localization = new List<Localization>();

        [JsonIgnore]
        private MergableDictionary<Action> m_actionTable;
        [JsonIgnore]
        private MergableDictionary<ActionSet> m_actionSetTable;
        [JsonIgnore]
        private MergableDictionary<DefaultBinding> m_defaultBindingTable;
        [JsonIgnore]
        private MergableDictionary<Localization> m_localizationTable;
        [JsonIgnore]
        private MergableDictionary<VIUSteamVRBindingFile> m_bindingFiles;

        protected override void OnAfterLoaded()
        {
            m_actionTable = actions.ToMergableDictionary();
            m_actionSetTable = action_sets.ToMergableDictionary();
            m_defaultBindingTable = default_bindings.ToMergableDictionary();
            m_localizationTable = localization.ToMergableDictionary();

            m_actionTable.onNewItemWhenMerge += item => actions.Add(item);
            m_actionSetTable.onNewItemWhenMerge += item => action_sets.Add(item);
            m_defaultBindingTable.onNewItemWhenMerge += item => default_bindings.Add(item);
            m_localizationTable.onNewItemWhenMerge += item => localization.Add(item);

            // load binding files
            m_bindingFiles = new MergableDictionary<VIUSteamVRBindingFile>();
            m_bindingFiles.onNewItemWhenMerge += item => item.dirPath = dirPath;
            foreach (var pair in m_defaultBindingTable)
            {
                var controllerType = pair.Key;
                var bindingUrl = pair.Value.binding_url;

                VIUSteamVRBindingFile bindingFile;
                if (VIUSteamVRBindingFile.TryLoad(dirPath, bindingUrl, out bindingFile))
                {
                    m_bindingFiles.Add(controllerType, bindingFile);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Missing default bindings file for " + controllerType + ":" + System.IO.Path.Combine(dirPath, bindingUrl) + "!");
                }
            }
        }

        public bool IsMerged(VIUSteamVRActionFile dst)
        {
            if (!m_actionTable.IsMerged(dst.m_actionTable)) { return false; }
            if (!m_actionSetTable.IsMerged(dst.m_actionSetTable)) { return false; }
            if (!m_defaultBindingTable.IsMerged(dst.m_defaultBindingTable)) { return false; }
            if (!m_localizationTable.IsMerged(dst.m_localizationTable)) { return false; }
            if (!m_bindingFiles.IsMerged(dst.m_bindingFiles)) { return false; }
            return true;
        }

        public void Merge(VIUSteamVRActionFile dst)
        {
            m_actionTable.Merge(dst.m_actionTable);
            m_actionSetTable.Merge(dst.m_actionSetTable);
            m_defaultBindingTable.Merge(dst.m_defaultBindingTable);
            m_localizationTable.Merge(dst.m_localizationTable);
            m_bindingFiles.Merge(dst.m_bindingFiles);
        }

        protected override void OnBeforeSave(string dirPash)
        {
            if (m_bindingFiles == null || m_bindingFiles.Count == 0) { return; }
            foreach (var pair in m_bindingFiles)
            {
                var bindingFile = pair.Value;
                bindingFile.Save(dirPash);
            }
        }

        [Serializable]
        public class Action : IMergable<Action>, IStringKey
        {
            public string name;
            public string type;
            public string scope;
            public string skeleton;
            public string requirement;

            [JsonIgnore]
            public string stringKey { get { return name; } }

            public bool IsMerged(Action obj)
            {
                if (name != obj.name) { return false; }
                if (type != obj.type) { return false; }
                if (scope != obj.scope) { return false; }
                if (skeleton != obj.skeleton) { return false; }
                if (requirement != obj.requirement) { return false; }
                return true;
            }

            public void Merge(Action obj)
            {
                type = obj.type;
                scope = obj.scope;
                skeleton = obj.skeleton;
                requirement = obj.requirement;
            }

            public Action Copy()
            {
                return new Action()
                {
                    name = name,
                    type = type,
                    scope = scope,
                    skeleton = skeleton,
                    requirement = requirement,
                };
            }
        }

        [Serializable]
        public class ActionSet : IMergable<ActionSet>, IStringKey
        {
            public string name;
            public string usage;

            [JsonIgnore]
            public string stringKey { get { return name; } }

            public bool IsMerged(ActionSet obj)
            {
                if (name != obj.name) { return false; }
                if (usage != obj.usage) { return false; }
                return true;
            }

            public void Merge(ActionSet obj)
            {
                usage = obj.usage;
            }

            public ActionSet Copy()
            {
                return new ActionSet()
                {
                    name = name,
                    usage = usage,
                };
            }
        }

        [Serializable]
        public class DefaultBinding : IMergable<DefaultBinding>, IStringKey
        {
            public string controller_type;
            public string binding_url;

            [JsonIgnore]
            public string stringKey { get { return controller_type; } }

            public bool IsMerged(DefaultBinding obj)
            {
                if (controller_type != obj.controller_type) { return false; }
                return true;
            }

            public void Merge(DefaultBinding obj)
            {
                // do nothing, don't override path, use old one
            }

            public DefaultBinding Copy()
            {
                return new DefaultBinding()
                {
                    controller_type = controller_type,
                    binding_url = binding_url,
                };
            }
        }

        [Serializable]
        public class Localization : MergableDictionary, IMergable<Localization>, IStringKey
        {
            [JsonIgnore]
            public string stringKey
            {
                get
                {
                    string lang;
                    return TryGetValue("language_tag", out lang) ? lang : string.Empty;
                }
            }

            public Localization() : base() { }

            public Localization(Localization src) : base(src) { }

            public bool IsMerged(Localization obj)
            {
                return ((MergableDictionary)this).IsMerged(obj);
            }

            Localization IMergable<Localization>.Copy()
            {
                return new Localization(this);
            }

            public void Merge(Localization obj)
            {
                ((MergableDictionary)this).Merge(obj);
            }
        }
    }
}
#endif