//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public class PoseDirection : BasePoseModifier
    {
        public Transform from;
        public Transform to;
        public Transform upward;

        public override void ModifyPose(ref RigidPose pose, bool useLocal)
        {
            Vector3 f, u;

            if (to == null)
            {
                f = from.forward;
            }
            else
            {
                f = to.position - from.position;
                if (f.sqrMagnitude <= Mathf.Epsilon * Mathf.Epsilon) { f = from.forward; }
            }

            if (upward == null)
            {

                u = GetWroldPoseRot(ref pose, useLocal) * Vector3.up;
            }
            else
            {
                u = upward.position - from.position;
                if (u.sqrMagnitude <= Mathf.Epsilon * Mathf.Epsilon) { u = GetWroldPoseRot(ref pose, useLocal) * Vector3.up; }
            }

            pose.rot = Quaternion.LookRotation(f, u);
            if (useLocal && transform.parent != null) { pose.rot = Quaternion.Inverse(transform.parent.rotation) * pose.rot; }
        }

        private Quaternion GetWroldPoseRot(ref RigidPose pose, bool useLocal)
        {
            if (useLocal && transform.parent != null)
            {
                return transform.parent.rotation * pose.rot;
            }
            else
            {
                return pose.rot;
            }
        }
    }
}