//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public class PoseTracker : BasePoseTracker
    {
        public Transform target;

        protected virtual void LateUpdate()
        {
            TrackPose(new RigidPose(target, true), target.parent);
        }
    }
}