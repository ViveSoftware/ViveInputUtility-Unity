//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public class PoseDirection : BasePoseModifier
    {
        public Transform from;
        public Transform to;
        public Transform upwards;

        public override void ModifyPose(ref RigidPose pose, bool useLocal)
        {
            if (from != null && to != null)
            {
                var f = to.position - from.position;
                var u = upwards == null ? Vector3.zero : (upwards.position - transform.position);
                if (u.sqrMagnitude <= Mathf.Epsilon * Mathf.Epsilon) { u = to.up; }
                pose.rot = Quaternion.LookRotation(f, u);
                if (useLocal || transform.parent != null) { pose.rot = Quaternion.Inverse(transform.parent.rotation) * pose.rot; }
            }
        }
    }
}