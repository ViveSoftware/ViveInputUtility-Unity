//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    public class PoseEaser : BasePoseModifier
    {
        // similar to equation y=1-(0.01^x) where 0<x<1
        private static AnimationCurve curve = new AnimationCurve(new Keyframe[] {
            new Keyframe(0f, 0f, 4.203674f, 4.203674f),
            new Keyframe(0.202865f, 0.5948543f, 1.790932f, 1.790932f),
            new Keyframe(0.3988017f, 0.8385032f, 0.8143054f, 0.8143054f),
            new Keyframe(1f, 0.99f, 0f, 0f),
        });

        public float duration = 0.15f;

        private bool firstPose = true;
        private RigidPose prevPose;

        public bool easePositionX = true;
        public bool easePositionY = true;
        public bool easePositionZ = true;

        public bool easeRotationX = true;
        public bool easeRotationY = true;
        public bool easeRotationZ = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetFirstPose();
        }

        public override void ModifyPose(ref RigidPose pose, bool useLocal)
        {
            if (firstPose)
            {
                firstPose = false;
            }
            else
            {
                var deltaTime = Time.unscaledDeltaTime;
                if (deltaTime < duration)
                {
                    var easedPose = RigidPose.Lerp(prevPose, pose, curve.Evaluate(deltaTime / duration));

                    if (!easePositionX || !easePositionY || !easePositionZ)
                    {
                        var originPos = pose.pos;
                        var easedPos = easedPose.pos;
                        if (!easePositionX) { easedPos.x = originPos.x; }
                        if (!easePositionY) { easedPos.y = originPos.y; }
                        if (!easePositionZ) { easedPos.z = originPos.z; }
                        easedPose.pos = easedPos;
                    }

                    if (!easeRotationX || !easeRotationY || !easeRotationZ)
                    {
                        var originEuler = pose.rot.eulerAngles;
                        var easedEuler = easedPose.rot.eulerAngles;
                        if (!easeRotationX) { easedEuler.x = originEuler.x; }
                        if (!easeRotationY) { easedEuler.y = originEuler.y; }
                        if (!easeRotationZ) { easedEuler.z = originEuler.z; }
                        easedPose.rot = Quaternion.Euler(easedEuler);
                    }

                    pose = easedPose;
                }
            }

            prevPose = pose;
        }

        public void ResetFirstPose() { firstPose = true; }
    }
}