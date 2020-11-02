//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class FitHmdHeight : MonoBehaviour, INewPoseListener
    {
        [SerializeField] private float m_distance;
        [SerializeField] private float m_height;

        public void BeforeNewPoses()
        {
            
        }

        public void OnNewPoses()
        {
            
        }

        public void AfterNewPoses()
        {
            if (!VivePose.IsValid(DeviceRole.Hmd))
            {
                return;
            }

            RigidPose hmdPose = VivePose.GetPose(DeviceRole.Hmd);
            Vector3 pos = hmdPose.pos;
            pos += hmdPose.forward * m_distance;
            pos.y += m_height;
            transform.localPosition = pos;

            Vector3 lookAtTarget = new Vector3(hmdPose.pos.x, pos.y, hmdPose.pos.z);
            transform.LookAt(lookAtTarget, Vector3.up);

            VivePose.RemoveNewPosesListener(this);
        }

        private void Start()
        {
            VivePose.AddNewPosesListener(this);
        }
    }
}