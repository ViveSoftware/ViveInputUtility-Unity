//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public enum Finger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky,
    }

    public enum HandJointName
    {
        Palm,
        Wrist,
        ThumbMetacarpal,
        ThumbProximal,
        ThumbDistal,
        ThumbTip,
        IndexMetacarpal,
        IndexProximal,
        IndexIntermediate,
        IndexDistal,
        IndexTip,
        MiddleMetacarpal,
        MiddleProximal,
        MiddleIntermediate,
        MiddleDistal,
        MiddleTip,
        RingMetacarpal,
        RingProximal,
        RingIntermediate,
        RingDistal,
        RingTip,
        PinkyMetacarpal,
        PinkyProximal,
        PinkyIntermediate,
        PinkyDistal,
        PinkyTip,
        None,
    }

    [Serializable]
    public struct HandJoint
    {
        public HandJointName name;
        public Vector3 position;
        public Quaternion rotation;

        public HandJoint(HandJointName name, Vector3 position, Quaternion rotation)
        {
            this.name = name;
            this.position = position;
            this.rotation = rotation;
        }

        public bool IsValid()
        {
            return name != HandJointName.None;
        }

        public static int GetMaxCount()
        {
            return EnumUtils.GetMaxValue(typeof(HandJointName)); // Exclude HandJointName.None
        }
    }
}