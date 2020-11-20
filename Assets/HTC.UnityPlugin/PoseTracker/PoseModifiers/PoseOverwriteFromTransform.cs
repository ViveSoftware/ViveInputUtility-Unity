//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public class PoseOverwriteFromTransform : BasePoseModifier
    {
        public Transform target;
        public Bool3 overwritePosition = Bool3.AllTrue;
        public Bool3 overwriteRotation = Bool3.AllTrue;

        public override void ModifyPose(ref RigidPose pose, bool useLocal)
        {
            if (overwritePosition.Any)
            {
                var targetPos = useLocal ? transform.InverseTransformPoint(target.position) : target.position;
                pose.pos = overwritePosition.All ? targetPos : Bool3.OverwriteVector3(pose.pos, overwritePosition, targetPos);
            }

            if (overwriteRotation.Any)
            {
                var targetRot = useLocal ? Quaternion.Inverse(transform.rotation) * target.rotation : target.rotation;
                pose.rot = overwriteRotation.All ? targetRot : Quaternion.Euler(Bool3.OverwriteVector3(pose.rot.eulerAngles, overwriteRotation, targetRot.eulerAngles));
            }
        }
    }
}