//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public abstract class BasePoseTracker : MonoBehaviour, IPoseTracker
    {
        public Vector3 posOffset;
        public Vector3 rotOffset;

        private OrderedIndexedSet<IPoseModifier> modifierSet;

        public void AddModifier(IPoseModifier obj)
        {
            if (obj == null) { return; }

            if (modifierSet == null)
            {
                modifierSet = new OrderedIndexedSet<IPoseModifier>();
                modifierSet.Add(obj);
            }
            else if (!modifierSet.Contains(obj))
            {
                // insert obj with right priority order
                if (obj.priority > modifierSet[modifierSet.Count - 1].priority)
                {
                    modifierSet.Add(obj);
                }
                else
                {
                    for (int i = 0, imax = modifierSet.Count; i < imax; ++i)
                    {
                        if (obj.priority <= modifierSet[i].priority)
                        {
                            modifierSet.Insert(i, obj);
                            break;
                        }
                    }
                }
            }
        }

        public bool RemoveModifier(IPoseModifier obj)
        {
            return modifierSet == null ? false : modifierSet.Remove(obj);
        }

        [Obsolete]
        protected void TrackPose(Pose pose, Transform origin = null)
        {
            TrackPose((RigidPose)pose, origin);
        }

        protected void TrackPose(RigidPose pose, Transform origin = null)
        {
            pose = pose * new RigidPose(posOffset, Quaternion.Euler(rotOffset));
            ModifyPose(ref pose, origin);
            RigidPose.SetPose(transform, pose, origin);
        }

        [Obsolete]
        protected void ModifyPose(ref Pose pose, Transform origin)
        {
            var rigidPose = (RigidPose)pose;
            ModifyPose(ref rigidPose, origin);
            pose = rigidPose;
        }

        protected void ModifyPose(ref RigidPose pose, Transform origin)
        {
            if (modifierSet == null) { return; }

            for (int i = 0, imax = modifierSet.Count; i < imax; ++i)
            {
                if (!modifierSet[i].enabled) { continue; }
                modifierSet[i].ModifyPose(ref pose, origin);
            }
        }
    }
}