//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

namespace HTC.UnityPlugin.Utility
{
    public enum HandJointName
    {
        Palm,
        Wrist,
        ThumbTrapezium,
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

    internal class HandJointNameReslver : EnumToIntResolver<HandJointName> { public override int Resolve(HandJointName e) { return (int)e; } }
}