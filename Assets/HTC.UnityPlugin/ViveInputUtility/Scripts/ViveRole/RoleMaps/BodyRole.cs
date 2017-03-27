//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    [ViveRoleEnum((int)TrackerRole.Invalid)]
    public enum BodyRole
    {
        Invalid,
        Head,
        RightHand,
        LeftHand,
        RightFoot,
        LeftFoot,
        Hip,
    }

    public class BodyRoleHandler : ViveRole.MapHandler<BodyRole>
    {
        private float[] m_direction = new float[ViveRole.MAX_DEVICE_COUNT];
        private float[] m_distance = new float[ViveRole.MAX_DEVICE_COUNT];
        private List<uint> m_sortedDevices = new List<uint>();

        public override void OnInitialize() { Refresh(); }

        public override void OnBindingChanged(BodyRole role, string deviceSN, bool bound)
        {
            if (!bound)
            {
                Refresh();
            }
        }

        public override void OnConnectedDeviceChanged(uint deviceIndex, ETrackedDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (connected)
            {
                Refresh();
            }
            else
            {
                UnmappingDevice(deviceIndex);
            }
        }

        public override void OnTrackedDeviceRoleChanged() { Refresh(); }

        private bool IsControllerOrTracker(uint deviceIndex)
        {
            var deviceClass = ViveRole.GetDeviceClass(deviceIndex);
            return deviceClass == ETrackedDeviceClass.Controller || deviceClass == ETrackedDeviceClass.GenericTracker;
        }

        public void Refresh()
        {
            m_sortedDevices.Clear();

            UnmappingAll();

            MappingRoleIfUnbound(BodyRole.Head, 0u);

            // get related poses and record controller/tracker devices
            var hmdPose = VivePose.GetPose(0u);
            // preserve only y-axis rotation
            hmdPose.rot = Quaternion.Euler(0f, hmdPose.rot.eulerAngles.y, 0f);
            // move center to half height
            hmdPose.pos = Vector3.Scale(hmdPose.pos, new Vector3(1f, 0.5f, 1f));
            var halfHeight = hmdPose.pos.y;
            var centerPoseInverse = hmdPose.GetInverse();
            for (uint i = 1; i < ViveRole.MAX_DEVICE_COUNT; ++i)
            {
                if (!IsControllerOrTracker(i)) { continue; }

                var relatedCenterPos = (centerPoseInverse * VivePose.GetPose(i)).pos;
                var normalizedPosOnPlane = Vector3.ProjectOnPlane(relatedCenterPos, Vector3.forward) / halfHeight; // normalize
                m_direction[i] = GetDirection(normalizedPosOnPlane);
                m_distance[i] = normalizedPosOnPlane.magnitude;

                m_sortedDevices.Add(i);
            }

            if (m_sortedDevices.Count == 0)
            {
                return;
            }

            var index = m_sortedDevices.Count - 1; // pointing last index
            // find 2 feet, should be most farest 2 devices
            m_sortedDevices.Sort(CompareDistance);
            if (IsFoot(m_sortedDevices[index]))
            {
                if (m_sortedDevices.Count <= 1)
                {
                    MappingRoleIfUnbound(BodyRole.RightFoot, m_sortedDevices[index]);
                    return;
                }

                if (!IsFoot(m_sortedDevices[index - 1]))
                {
                    // only 1 foot found
                    MappingRoleIfUnbound(BodyRole.RightFoot, m_sortedDevices[index]);
                    m_sortedDevices.RemoveAt(index--);
                    if (index < 0) { return; }
                }
                else
                {
                    // 2 feet found, deside lef/right foot
                    if (m_direction[m_sortedDevices[index]] < m_direction[m_sortedDevices[index - 1]])
                    {
                        MappingRoleIfUnbound(BodyRole.RightFoot, m_sortedDevices[index]);
                        MappingRoleIfUnbound(BodyRole.LeftFoot, m_sortedDevices[index - 1]);
                    }
                    else
                    {
                        MappingRoleIfUnbound(BodyRole.RightFoot, m_sortedDevices[index - 1]);
                        MappingRoleIfUnbound(BodyRole.LeftFoot, m_sortedDevices[index]);
                    }

                    m_sortedDevices.RemoveAt(index--);
                    m_sortedDevices.RemoveAt(index--);
                    if (index < 0) { return; }
                }
            }

            // find 2 hands, should be most left and most right device
            m_sortedDevices.Sort(CompareDirection);

            // right most device as right hand
            MappingRoleIfUnbound(BodyRole.RightHand, m_sortedDevices[0]);
            if (m_sortedDevices.Count == 1) { return; }

            // left most device as left hand
            MappingRoleIfUnbound(BodyRole.LeftHand, m_sortedDevices[index]);
            if (m_sortedDevices.Count == 2) { return; }

            // middle one as hip
            MappingRoleIfUnbound(BodyRole.Hip, m_sortedDevices[index / 2]);
        }

        private bool IsFoot(uint di)
        {
            var dist = m_distance[di];
            var dir = m_direction[di];

            return dist > 0.25f && dir > 3.5f && dir < 4.5f;
        }

        // less score => right, large score => left
        private float GetDirection(Vector2 pos)
        {
            var ax = Mathf.Abs(pos.x);
            var ay = Mathf.Abs(pos.y);
            if (pos.x > 0f)
            {
                if (pos.y > 0f)
                {
                    if (ax < ay)
                    {
                        return 0f + (ax / ay);
                    }
                    else
                    {
                        return 1f + (1f - ay / ax);
                    }
                }
                else
                {
                    if (ax > ay)
                    {
                        return 2f + (ay / ax);
                    }
                    else
                    {
                        return 3f + (1f - ax / ay);
                    }
                }
            }
            else
            {
                if (pos.y < 0f)
                {
                    if (ax < ay)
                    {
                        return 4f + (ax / ay);
                    }
                    else
                    {
                        return 5f + (1f - ay / ax);
                    }
                }
                else
                {
                    if (ax > ay)
                    {
                        return 6f + (ay / ax);
                    }
                    else
                    {
                        return 7f + (1 - ax / ay);
                    }
                }
            }
        }

        private int CompareDistance(uint d1, uint d2)
        {
            var dd1 = m_distance[d1];
            var dd2 = m_distance[d2];

            if (dd1 == dd2) { return 0; }
            if (dd1 < dd2) { return -1; }
            return 1;
        }

        private int CompareDirection(uint d1, uint d2)
        {
            var sd1 = m_direction[d1];
            var sd2 = m_direction[d2];

            if (sd1 == sd2) { return 0; }
            if (sd1 < sd2) { return -1; }
            return 1;
        }
    }
}