//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public class PoseFreezer : BasePoseModifier
    {
        public bool freezePositionX = false;
        public bool freezePositionY = false;
        public bool freezePositionZ = false;

        public bool freezeRotationX = true;
        public bool freezeRotationY = false;
        public bool freezeRotationZ = true;

        public override void ModifyPose(ref RigidPose pose, Transform origin)
        {
            Vector3 freezePos;
            Vector3 freezeEuler;

            if (freezePositionX || freezePositionY || freezePositionZ)
            {
                if (origin != null && origin != transform.parent)
                {
                    freezePos = origin.InverseTransformPoint(transform.position);
                }
                else
                {
                    freezePos = transform.localPosition;
                }

                if (freezePositionX) { pose.pos.x = freezePos.x; }
                if (freezePositionY) { pose.pos.y = freezePos.y; }
                if (freezePositionZ) { pose.pos.z = freezePos.z; }
            }

            if (freezeRotationX || freezeRotationY || freezeRotationZ)
            {
                if (origin != null && origin != transform.parent)
                {
                    freezeEuler = (Quaternion.Inverse(origin.rotation) * transform.rotation).eulerAngles;
                }
                else
                {
                    freezeEuler = transform.localEulerAngles;
                }

                var poseEuler = pose.rot.eulerAngles;
                if (freezeRotationX) { poseEuler.x = freezeEuler.x; }
                if (freezeRotationY) { poseEuler.y = freezeEuler.y; }
                if (freezeRotationZ) { poseEuler.z = freezeEuler.z; }
                pose.rot = Quaternion.Euler(poseEuler);
            }
        }
    }
}