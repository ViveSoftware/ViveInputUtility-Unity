//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class FollowingPanel : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] private float m_distance;
        [SerializeField] private float m_height;
        [SerializeField] private float m_speedCoef;

#pragma warning restore 0649

        private Vector3 m_targetPosition;

        private void Awake()
        {
            m_targetPosition = transform.position;
        }

        private void Update()
        {
            RigidPose hmdPose = VivePose.GetPose(DeviceRole.Hmd);
            Vector3 pos = hmdPose.pos;
            pos += hmdPose.forward * m_distance;
            pos.y += m_height;
            m_targetPosition = pos;

            transform.position = Vector3.Lerp(transform.position, m_targetPosition, Time.deltaTime * m_speedCoef);
            transform.LookAt(hmdPose.pos, Vector3.up);
        }
    }
}