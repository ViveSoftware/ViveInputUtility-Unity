//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using UnityEngine;

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

    internal class BodyRoleIntReslver : EnumToIntResolver<BodyRole> { public override int Resolve(BodyRole e) { return (int)e; } }

    public class BodyRoleHandler : ViveRole.MapHandler<BodyRole>
    {
        private float[] m_directionPoint = new float[VRModule.MAX_DEVICE_COUNT];
        private float[] m_distanceSqr = new float[VRModule.MAX_DEVICE_COUNT];
        private List<uint> m_sortedDevices = new List<uint>();

        private bool IsTrackingDevice(uint deviceIndex)
        {
            return IsTrackingDevice(VRModule.GetCurrentDeviceState(deviceIndex).deviceClass);
        }

        private bool IsTrackingDevice(VRModuleDeviceClass deviceClass)
        {
            return deviceClass == VRModuleDeviceClass.HMD || deviceClass == VRModuleDeviceClass.Controller || deviceClass == VRModuleDeviceClass.GenericTracker;
        }

        public override void OnAssignedAsCurrentMapHandler() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (!RoleMap.IsDeviceBound(deviceSN) && !IsTrackingDevice(deviceClass)) { return; }

            Refresh();
        }

        public override void OnBindingChanged(string deviceSN, bool previousIsBound, BodyRole previousRole, bool currentIsBound, BodyRole currentRole)
        {
            uint deviceIndex;
            if (!VRModule.TryGetConnectedDeviceIndex(deviceSN, out deviceIndex)) { return; }

            Refresh();
        }

        public void Refresh()
        {
            m_sortedDevices.Clear();

            UnmappingAll();

            if (!VRModule.GetCurrentDeviceState(VRModule.HMD_DEVICE_INDEX).isConnected) { return; }

            MappingRoleIfUnbound(BodyRole.Head, VRModule.HMD_DEVICE_INDEX);

            // get related poses and record controller/tracker devices
            var hmdPose = VivePose.GetPose(0u);
            // preserve only y-axis rotation
            hmdPose.rot = Quaternion.Euler(0f, hmdPose.rot.eulerAngles.y, 0f);
            // move center to half height
            hmdPose.pos = Vector3.Scale(hmdPose.pos, new Vector3(1f, 0.5f, 1f));
            var halfHeight = hmdPose.pos.y;
            var centerPoseInverse = hmdPose.GetInverse();
            for (uint i = 1, imax = VRModule.GetDeviceStateCount(); i < imax; ++i)
            {
                if (!IsTrackingDevice(i)) { continue; }

                var relatedCenterPos = centerPoseInverse.TransformPoint(VRModule.GetCurrentDeviceState(i).pose.pos);
                m_directionPoint[i] = HandRoleHandler.GetDirectionPoint(new Vector2(relatedCenterPos.x, -relatedCenterPos.y));
                m_distanceSqr[i] = relatedCenterPos.sqrMagnitude / (halfHeight * halfHeight);

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
                    // 2 feet found, determine lef/right foot
                    if (m_directionPoint[m_sortedDevices[index]] < m_directionPoint[m_sortedDevices[index - 1]])
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
            var dist = m_distanceSqr[di];
            var dir = m_directionPoint[di];

            return dist > (0.25f * 0.25f) && dir > 3.5f && dir < 4.5f;
        }

        private int CompareDistance(uint d1, uint d2)
        {
            var dd1 = m_distanceSqr[d1];
            var dd2 = m_distanceSqr[d2];

            if (dd1 == dd2) { return 0; }
            if (dd1 < dd2) { return -1; }
            return 1;
        }

        private int CompareDirection(uint d1, uint d2)
        {
            var sd1 = m_directionPoint[d1];
            var sd2 = m_directionPoint[d2];

            if (sd1 == sd2) { return 0; }
            if (sd1 < sd2) { return -1; }
            return 1;
        }
    }
}