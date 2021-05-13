//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public interface IPoseModifier
    {
        bool enabled { get; }
        int priority { get; set; }
        [Obsolete]
        void ModifyPose(ref Pose pose, Transform origin);
        [Obsolete]
        void ModifyPose(ref RigidPose pose, Transform origin);
        void ModifyPose(ref RigidPose pose, bool useLocal);
    }
}