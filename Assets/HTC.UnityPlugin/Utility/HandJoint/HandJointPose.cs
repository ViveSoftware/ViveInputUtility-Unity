//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [Serializable]
    public struct HandJointPose
    {
        public HandJointName name;
        public RigidPose pose;

        public HandJointPose(HandJointName name, Vector3 position, Quaternion rotation)
        {
            this.name = name;
            pose = new RigidPose(position, rotation);
        }

        public bool IsValid()
        {
            return name != HandJointName.None;
        }

        public static int GetMaxCount()
        {
            return EnumUtils.GetMaxValue(typeof(HandJointName)); // Exclude HandJointName.None
        }

        public static int NameToIndex(HandJointName joint)
        {
            return (int) joint - 1; // Since HandJointName.None = 0
        }
    }
}