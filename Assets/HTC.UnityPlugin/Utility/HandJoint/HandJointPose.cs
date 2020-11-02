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
            return (int)joint - 1; // Since HandJointName.None = 0
        }

        public static void AssignToArray(HandJointPose[] joints, HandJointName jointName, Vector3 position, Quaternion rotation)
        {
            if (joints == null)
            {
                return;
            }

            if (joints.Length < GetMaxCount())
            {
                return;
            }

            if (jointName == HandJointName.None)
            {
                return;
            }

            joints[NameToIndex(jointName)] = new HandJointPose(jointName, position, rotation);
        }
    }

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