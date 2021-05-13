//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("VIU/Device Tracker/Vive Joint Pose Tracker")]
    public class ViveJointPoseTracker : BasePoseTracker, INewPoseListener, IViveRoleComponent
    {
        [Serializable]
        public class UnityEventBool : UnityEvent<bool> { }

        private bool m_isValid;

        [SerializeField]
        private Transform m_origin;
        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private HandJointName m_joint = HandJointName.IndexTip;
        [SerializeField]
        private UnityEventBool m_onIsValidChanged;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public Transform origin { get { return m_origin; } set { m_origin = value; } }

        public UnityEventBool onIsValidChanged { get { return onIsValidChanged; } }

        public bool isPoseValid
        {
            get
            {
                return isRoleJointValid && VivePose.GetAllHandJoints(m_viveRole)[m_joint].isValid;
            }
        }

        private bool TryGetValidPose(out RigidPose pose)
        {
            pose = default(RigidPose);
            if (!isRoleJointValid) { return false; }

            var p = VivePose.GetAllHandJoints(m_viveRole)[m_joint];
            if (!p.isValid) { return false; }

            pose = p.pose;
            return true;
        }

        private bool isRoleJointValid
        {
            get
            {
                if (m_viveRole == null) { return false; }
                if (VivePose.GetHandJointCount(m_viveRole) == 0) { return false; }
                if (m_joint < EnumArrayBase<HandJointName>.StaticMin) { return false; }
                if (m_joint > EnumArrayBase<HandJointName>.StaticMax) { return false; }
                return true;
            }
        }

        protected void SetIsValid(bool value, bool forceSet = false)
        {
            if (ChangeProp.Set(ref m_isValid, value) || forceSet)
            {
                if (m_onIsValidChanged != null)
                {
                    m_onIsValidChanged.Invoke(value);
                }
            }
        }

        protected virtual void Start()
        {
            SetIsValid(isPoseValid, true);
        }

        protected virtual void OnEnable()
        {
            VivePose.AddNewPosesListener(this);
        }

        protected virtual void OnDisable()
        {
            VivePose.RemoveNewPosesListener(this);

            SetIsValid(false);
        }

        public virtual void BeforeNewPoses() { }

        public virtual void OnNewPoses()
        {
            RigidPose pose;
            if (TryGetValidPose(out pose))
            {
                if (m_origin != null && m_origin != transform.parent)
                {
                    pose = new RigidPose(m_origin.transform) * pose;
                    TrackPose(pose, false);
                }
                else
                {
                    TrackPose(pose, true);
                }
                SetIsValid(true);
            }
            else
            {
                SetIsValid(false);
            }
        }

        public virtual void AfterNewPoses() { }
    }
}