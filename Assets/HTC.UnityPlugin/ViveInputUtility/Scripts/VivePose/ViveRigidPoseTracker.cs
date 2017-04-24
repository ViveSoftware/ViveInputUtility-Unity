//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("HTC/Vive/Vive Rigid Pose Tracker")]
    public class ViveRigidPoseTracker : VivePoseTracker
    {
        public const float MIN_FOLLOWING_DURATION = 0.02f;
        public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
        public const float MAX_FOLLOWING_DURATION = 0.5f;

        private Rigidbody rigid;
        private Pose targetPose;
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
                // applying velocity
                var diffPos = targetPose.pos - rigid.position;
                if (Mathf.Approximately(diffPos.sqrMagnitude, 0f))
                {
                    rigid.velocity = Vector3.zero;
                }
                else
                {
                    rigid.velocity = diffPos / Mathf.Clamp(followingDuration, MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION);
                }

                // applying angular velocity
                float angle;
                Vector3 axis;
                (targetPose.rot * Quaternion.Inverse(rigid.rotation)).ToAngleAxis(out angle, out axis);
                while (angle > 360f) { angle -= 360f; }

                if (Mathf.Approximately(angle, 0f) || float.IsNaN(axis.x))
                {
                    rigid.angularVelocity = Vector3.zero;
                }
                else
                {
                    angle *= Mathf.Deg2Rad / Mathf.Clamp(followingDuration, MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION); // convert to radius speed
                    if (rigid.maxAngularVelocity < angle) { rigid.maxAngularVelocity = angle; }
                    rigid.angularVelocity = axis * angle;
                }
            }
        }

        public override void OnNewPoses()
        {
            var deviceIndex = viveRole.GetDeviceIndex();

            // set targetPose to device pose
            targetPose = VivePose.GetPose(deviceIndex) * new Pose(posOffset, Quaternion.Euler(rotOffset));
            ModifyPose(ref targetPose, origin);

            // transform to world space
            var o = origin != null ? origin : transform.parent;
            if (o != null)
            {
                targetPose = new Pose(o) * targetPose;
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