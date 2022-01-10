//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

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

        public override void ModifyPose(ref RigidPose pose, bool useLocal)
        {
            if (freezePositionX || freezePositionY || freezePositionZ)
            {
                var freezePos = useLocal ? transform.localPosition : transform.position;
                if (freezePositionX) { pose.pos.x = freezePos.x; }
                if (freezePositionY) { pose.pos.y = freezePos.y; }
                if (freezePositionZ) { pose.pos.z = freezePos.z; }
            }

            if (freezeRotationX || freezeRotationY || freezeRotationZ)
            {
                var freezeEuler = useLocal ? transform.localEulerAngles : transform.eulerAngles;
                var poseEuler = pose.rot.eulerAngles;
                if (freezeRotationX) { poseEuler.x = freezeEuler.x; }
                if (freezeRotationY) { poseEuler.y = freezeEuler.y; }
                if (freezeRotationZ) { poseEuler.z = freezeEuler.z; }
                pose.rot = Quaternion.Euler(poseEuler);
            }
        }
    }
}