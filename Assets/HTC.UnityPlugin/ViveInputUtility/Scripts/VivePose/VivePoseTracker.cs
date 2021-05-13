//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("VIU/Device Tracker/Vive Pose Tracker (Transform)", 7)]
    // Simple component to track Vive devices.
    public class VivePoseTracker : BasePoseTracker, INewPoseListener, IViveRoleComponent
    {
        [Serializable]
        public class UnityEventBool : UnityEvent<bool> { }

        private bool m_isValid;

        [SerializeField]
        [FormerlySerializedAs("origin")]
        private Transform m_origin;
        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        [FormerlySerializedAs("onIsValidChanged")]
        private UnityEventBool m_onIsValidChanged;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public bool isPoseValid { get { return m_isValid; } }

        public Transform origin { get { return m_origin; } set { m_origin = value; } }

        public UnityEventBool onIsValidChanged { get { return m_onIsValidChanged; } }

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
            SetIsValid(VivePose.IsValid(m_viveRole), true);
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
            var deviceIndex = m_viveRole.GetDeviceIndex();
            var isValid = VivePose.IsValid(deviceIndex);

            if (isValid)
            {
                var pose = VivePose.GetPose(deviceIndex);
                if (m_origin != null && m_origin != transform.parent)
                {
                    pose = new RigidPose(m_origin.transform) * pose;
                    TrackPose(pose, false);
                }
                else
                {
                    TrackPose(pose, true);
                }
            }

            SetIsValid(isValid);
        }

        public virtual void AfterNewPoses() { }
    }
}