//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using HTC.UnityPlugin.Utility;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("VIU/Device Tracker/Vive Rigid Pose Tracker (Rigidbody)", 8)]
    [RequireComponent(typeof(Rigidbody))]
    public class ViveRigidPoseTracker : VivePoseTracker
    {
        public const float MIN_FOLLOWING_DURATION = 0.02f;
        public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
        public const float MAX_FOLLOWING_DURATION = 0.5f;

        private Rigidbody rigid;
        private RigidPose targetPose;
        private bool m_snap;

        [SerializeField]
        private bool m_snapOnEnable = true;
        [Range(MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION)]
        public float followingDuration = DEFAULT_FOLLOWING_DURATION;

        public bool snapOnEnable { get { return m_snapOnEnable; } set { m_snapOnEnable = value; } }

        protected override void Start()
        {
            base.Start();
            rigid = GetComponent<Rigidbody>();
            rigid.useGravity = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_snapOnEnable) { m_snap = true; }
        }

        protected virtual void FixedUpdate()
        {
            if (isPoseValid)
            {
                RigidPose.SetRigidbodyVelocity(rigid, rigid.position, targetPose.pos, followingDuration);
                RigidPose.SetRigidbodyAngularVelocity(rigid, rigid.rotation, targetPose.rot, followingDuration);
            }
            else
            {
                rigid.velocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
            }
        }

        protected override void OnDisable()
        {
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            base.OnDisable();
        }

        public override void OnNewPoses()
        {
            var deviceIndex = viveRole.GetDeviceIndex();

            // set targetPose to device pose
            targetPose = VivePose.GetPose(deviceIndex) * new RigidPose(posOffset, Quaternion.Euler(rotOffset));
            if (origin != null && origin != transform.parent)
            {
                targetPose = new RigidPose(origin.transform) * targetPose;
                ModifyPose(ref targetPose, false);
            }
            else
            {
                ModifyPose(ref targetPose, true);
            }

            // transform to world space
            var o = origin != null ? origin : transform.parent;
            if (o != null)
            {
                targetPose = new RigidPose(o) * targetPose;
                targetPose.pos.Scale(o.localScale);
            }

            if (m_snap)
            {
                m_snap = false;
                transform.position = targetPose.pos;
                transform.rotation = targetPose.rot;
            }

            SetIsValid(VivePose.IsValid(deviceIndex));
        }
    }
}