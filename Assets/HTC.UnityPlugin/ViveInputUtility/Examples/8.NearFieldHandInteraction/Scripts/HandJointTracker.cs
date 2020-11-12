//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class HandJointTracker : MonoBehaviour
    {
        public enum Handness
        {
            LeftHand,
            RightHand,
        }

        public Handness handness;
        public HandJointName jointName;
        public UnityEvent<bool> poseStatusChanged;

        private bool m_isPoseValid;

        public bool isPoseValid
        {
            get { return m_isPoseValid; }
        }

        protected virtual void Update()
        {
            TrackedHandRole handRole = handness == Handness.LeftHand ? TrackedHandRole.LeftHand : TrackedHandRole.RightHand;

            JointPose jointPose;
            if (VivePose.TryGetHandJointPoseEx(handRole, jointName, out jointPose))
            {
                transform.localPosition = jointPose.pose.pos;
                transform.localRotation = jointPose.pose.rot;

                if (!m_isPoseValid)
                {
                    m_isPoseValid = true;
                    InvokePoseStatusChangeEvent(m_isPoseValid);
                }
            }
            else
            {
                if (m_isPoseValid)
                {
                    m_isPoseValid = false;
                    InvokePoseStatusChangeEvent(m_isPoseValid);
                }
            }
        }

        protected void InvokePoseStatusChangeEvent(bool isValid)
        {
            if (poseStatusChanged == null)
            {
                return;
            }

            poseStatusChanged.Invoke(isValid);
        }
    }
}