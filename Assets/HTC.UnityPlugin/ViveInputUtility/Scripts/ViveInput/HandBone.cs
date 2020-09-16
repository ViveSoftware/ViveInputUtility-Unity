using System;
using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public enum FingerType
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky,
    }

    public enum HandBoneType
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
    }

    [Serializable]
    public class HandBone
    {
        public HandBoneType type;
        public Vector3 position;
        public Quaternion rotation;

        public HandBone(HandBoneType type, Vector3 position, Quaternion rotation)
        {
            this.type = type;
            this.position = position;
            this.rotation = rotation;
        }

        public static int GetMaxCount()
        {
            return EnumUtils.GetMaxValue(typeof(HandBoneType)) + 1;
        }
    }
}