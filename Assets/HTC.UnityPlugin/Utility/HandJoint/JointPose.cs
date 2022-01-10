//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [Serializable]
    public struct JointPose
    {
        public bool isValid;
        public RigidPose pose;
        public JointPose(Vector3 position, Quaternion rotation)
        {
            this.isValid = true;
            this.pose = new RigidPose(position, rotation);
        }
        public JointPose(RigidPose pose)
        {
            this.isValid = true;
            this.pose = pose;
        }
    }
}