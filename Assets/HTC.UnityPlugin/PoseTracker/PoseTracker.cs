//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    [AddComponentMenu("HTC/Pose Tracker/Pose Tracker")]
    public class PoseTracker : BasePoseTracker
    {
        public Transform target;

        protected virtual void LateUpdate()
        {
            TrackPose(new RigidPose(target, true), target.parent);
        }
    }
}