//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR_2_0_0_OR_NEWER
using System;

namespace HTC.UnityPlugin.Vive.SteamVRExtension
{
    [Serializable]
    public class VIUSteamVRBindingFile : VIUSteamVRLoadJsonFileBase<VIUSteamVRBindingFile>, IMergable<VIUSteamVRBindingFile>
    {
        public string app_key;
        public string controller_type;
        public string description;
        public string name;
        public MergableDictionary<ActionList> bindings = new MergableDictionary<ActionList>();

        public bool IsMerged(VIUSteamVRBindingFile dst)
        {
            if (!bindings.IsMerged(dst.bindings)) { return false; }
            return true;
        }

        public void Merge(VIUSteamVRBindingFile dst)
        {
            bindings.Merge(dst.bindings);
        }

        public VIUSteamVRBindingFile Copy()
        {
            return new VIUSteamVRBindingFile()
            {
                dirPath = dirPath,
                fileName = fileName,

                app_key = app_key,
                controller_type = controller_type,
                description = description,
                name = name,
                bindings = bindings.Copy(),
            };
        }

        [Serializable]
        public class ActionList : IMergable<ActionList>
        {
            public OverridableList<Chords> chords = new OverridableList<Chords>();
            public OverridableList<Source> sources = new OverridableList<Source>();
            public OverridableList<StandardBinding> poses = new OverridableList<StandardBinding>();
            public OverridableList<StandardBinding> haptics = new OverridableList<StandardBinding>();
            public OverridableList<StandardBinding> skeleton = new OverridableList<StandardBinding>();

            public bool IsMerged(ActionList obj)
            {
                if (!chords.IsMerged(obj.chords)) { return false; }
                if (!sources.IsMerged(obj.sources)) { return false; }
                if (!poses.IsMerged(obj.poses)) { return false; }
                if (!haptics.IsMerged(obj.haptics)) { return false; }
                if (!skeleton.IsMerged(obj.skeleton)) { return false; }
                return true;
            }

            public ActionList Copy()
            {
                return new ActionList()
                {
                    chords = chords.Copy(),
                    poses = poses.Copy(),
                    haptics = haptics.Copy(),
                    sources = sources.Copy(),
                    skeleton = skeleton.Copy(),
                };
            }

            public void Merge(ActionList obj)
            {
                chords.Merge(obj.chords);
                sources.Merge(obj.sources);
                poses.Merge(obj.poses);
                haptics.Merge(obj.haptics);
                skeleton.Merge(obj.skeleton);
            }

            [Serializable]
            public class Chords : IMergable<Chords>
            {
                public string output;
                public OverridableDictionary<OverridableDictionary> inputs = new OverridableDictionary<OverridableDictionary>();

                public bool IsMerged(Chords obj)
                {
                    if (output != obj.output) { return false; }
                    if (!inputs.IsMerged(obj.inputs)) { return false; }
                    return true;
                }

                public Chords Copy()
                {
                    return new Chords()
                    {
                        output = output,
                        inputs = inputs.Copy(),
                    };
                }

                public void Merge(Chords obj) { throw new NotImplementedException(); }
            }

            [Serializable]
            public class Source : IMergable<Source>
            {
                public string path;
                public string mode;
                public OverridableDictionary parameters = new OverridableDictionary();
                public OverridableDictionary<OverridableDictionary> inputs = new OverridableDictionary<OverridableDictionary>();

                public bool IsMerged(Source obj)
                {
                    if (path != obj.path) { return false; }
                    if (mode != obj.mode) { return false; }
                    if (!parameters.IsMerged(obj.parameters)) { return false; }
                    if (!inputs.IsMerged(obj.inputs)) { return false; }
                    return true;
                }

                public Source Copy()
                {
                    return new Source()
                    {
                        path = path,
                        mode = mode,
                        parameters = parameters.Copy(),
                        inputs = inputs.Copy(),
                    };
                }

                public void Merge(Source obj) { throw new NotImplementedException(); }
            }

            [Serializable]
            public class StandardBinding : IMergable<StandardBinding>
            {
                public string output;
                public string path;

                public bool IsMerged(StandardBinding obj)
                {
                    if (output != obj.output) { return false; }
                    if (path != obj.path) { return false; }
                    return true;
                }

                public StandardBinding Copy()
                {
                    return new StandardBinding()
                    {
                        output = output,
                        path = path,
                    };
                }

                public void Merge(StandardBinding obj) { throw new NotImplementedException(); }
            }
        }
    }
}
#endif