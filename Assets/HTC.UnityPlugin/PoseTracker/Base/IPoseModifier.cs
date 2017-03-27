//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public interface IPoseModifier
    {
        bool enabled { get; }
        int priority { get; set; }
        void ModifyPose(ref Pose pose, Transform origin);
    }
}