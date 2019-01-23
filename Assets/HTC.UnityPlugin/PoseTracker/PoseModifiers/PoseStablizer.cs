//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public class PoseStablizer : BasePoseModifier
    {
        public float positionThreshold = 0.0005f; // meter
        public float rotationThreshold = 0.5f; // degree

        private bool firstPose = true;
        private RigidPose prevPose;

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetFirstPose();
        }

        public override void ModifyPose(ref RigidPose pose, Transform origin)
        {
            if (firstPose)
            {
                firstPose = false;
            }
            else
            {
                Vector3 posDiff = prevPose.pos - pose.pos;
                if (positionThreshold > 0f || posDiff.sqrMagnitude > positionThreshold * positionThreshold)
                {
                    pose.pos = pose.pos + Vector3.ClampMagnitude(posDiff, positionThreshold);
                }
                else
                {
                    pose.pos = prevPose.pos;
                }

                if (rotationThreshold > 0f || Quaternion.Angle(pose.rot, prevPose.rot) > rotationThreshold)
                {
                    pose.rot = Quaternion.RotateTowards(pose.rot, prevPose.rot, rotationThreshold);
                }
                else
                {
                    pose.rot = prevPose.rot;
                }
            }

            prevPose = pose;
        }

        public void ResetFirstPose() { firstPose = true; }
    }
}