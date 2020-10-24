//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class HandJointUpdater : MonoBehaviour, RenderModelHook.ICustomModel
    {
        public enum Joint
        {
            Palm = HandJointName.Palm,
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

        public static Joint ToJoint(HandJointName name) { return (Joint)name; }

        private EnumArray<HandJointName, RigidPose> jointPoses = new EnumArray<HandJointName, RigidPose>();

        private void Update()
        {
            
        }

        public void OnAfterModelCreated(RenderModelHook hook)
        {
            throw new NotImplementedException();
        }

        public bool OnBeforeModelActivated(RenderModelHook hook)
        {
            throw new NotImplementedException();
        }

        public bool OnBeforeModelDeactivated(RenderModelHook hook)
        {
            throw new NotImplementedException();
        }
    }
}