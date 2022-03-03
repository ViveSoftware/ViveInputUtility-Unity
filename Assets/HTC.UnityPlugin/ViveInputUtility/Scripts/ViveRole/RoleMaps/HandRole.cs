//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// Defines roles for those devices that have buttons
    /// </summary>
    [ViveRoleEnum((int)HandRole.Invalid)]
    public enum HandRole
    {
        Invalid = -1,
        RightHand,
        LeftHand,
        ExternalCamera,
        Controller3 = ExternalCamera,
        Controller4,
        Controller5,
        Controller6,
        Controller7,
        Controller8,
        Controller9,
        Controller10,
        Controller11,
        Controller12,
        Controller13,
        Controller14,
        Controller15,
    }

    internal class HandRoleIntReslver : EnumToIntResolver<HandRole> { public override int Resolve(HandRole e) { return (int)e; } }

    public static class ConvertRoleExtension
    {
        [Obsolete("HandRole and DeviceRole are not related now")]
        public static DeviceRole ToDeviceRole(this HandRole role)
        {
            switch (role)
            {
                case HandRole.RightHand: return DeviceRole.RightHand;
                case HandRole.LeftHand: return DeviceRole.LeftHand;
                case HandRole.Controller3: return DeviceRole.Controller3;
                case HandRole.Controller4: return DeviceRole.Controller4;
                case HandRole.Controller5: return DeviceRole.Controller5;
                case HandRole.Controller6: return DeviceRole.Controller6;
                case HandRole.Controller7: return DeviceRole.Controller7;
                case HandRole.Controller8: return DeviceRole.Controller8;
                case HandRole.Controller9: return DeviceRole.Controller9;
                case HandRole.Controller10: return DeviceRole.Controller10;
                case HandRole.Controller11: return DeviceRole.Controller11;
                case HandRole.Controller12: return DeviceRole.Controller12;
                case HandRole.Controller13: return DeviceRole.Controller13;
                case HandRole.Controller14: return DeviceRole.Controller14;
                case HandRole.Controller15: return DeviceRole.Controller15;
                default: return (DeviceRole)((int)DeviceRole.Hmd - 1); // returns invalid value
            }
        }
    }

    public class HandRoleHandler : ViveRole.MapHandler<HandRole>
    {
#if __VIU_STEAMVR
        private uint[] m_sortedDevices = new uint[VRModule.MAX_DEVICE_COUNT];
#endif
        private List<uint> m_sortedDeviceList = new List<uint>();

        // HandRole only tracks controllers
        private bool IsController(uint deviceIndex)
        {
            return IsController(VRModule.GetCurrentDeviceState(deviceIndex).deviceClass);
        }

        private bool IsController(VRModuleDeviceClass deviceClass)
        {
            return deviceClass == VRModuleDeviceClass.Controller;
        }

        private bool IsTracker(uint deviceIndex)
        {
            return IsTracker(VRModule.GetCurrentDeviceState(deviceIndex).deviceClass);
        }

        private bool IsTracker(VRModuleDeviceClass deviceClass)
        {
            return deviceClass == VRModuleDeviceClass.GenericTracker;
        }

        private bool IsControllerOrTracker(uint deviceIndex)
        {
            var deviceClass = VRModule.GetCurrentDeviceState(deviceIndex).deviceClass;
            return IsController(deviceClass) || IsTracker(deviceClass);
        }

        private bool IsTrackedHand(uint deviceIndex)
        {
            return IsTrackedHand(VRModule.GetCurrentDeviceState(deviceIndex).deviceClass);
        }

        private bool IsTrackedHand(VRModuleDeviceClass deviceClass)
        {
            return deviceClass == VRModuleDeviceClass.TrackedHand;
        }

        public override void OnAssignedAsCurrentMapHandler() { Refresh(); }

        public override void OnTrackedDeviceRoleChanged() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (!RoleMap.IsDeviceBound(deviceSN) && !IsController(deviceClass) && !IsTracker(deviceClass) && !IsTrackedHand(deviceClass)) { return; }

            Refresh();
        }

        public override void OnBindingChanged(string deviceSN, bool previousIsBound, HandRole previousRole, bool currentIsBound, HandRole currentRole)
        {
            uint deviceIndex;
            if (!VRModule.TryGetConnectedDeviceIndex(deviceSN, out deviceIndex)) { return; }

            Refresh();
        }

        public void Refresh()
        {
            MappingLeftRightHands();
            MappingOtherControllers();
        }

        private void MappingLeftRightHands()
        {
            // assign left/right controllers according to the hint
            uint rightIndex, leftIndex;
            var rightBound = RoleMap.IsRoleBound(HandRole.RightHand);
            var leftBound = RoleMap.IsRoleBound(HandRole.LeftHand);
            var deviceCount = VRModule.GetDeviceStateCount();

            if (rightBound)
            {
                rightIndex = RoleMap.GetMappedDeviceByRole(HandRole.RightHand);
            }
            else
            {
                rightIndex = VRModule.GetRightControllerDeviceIndex();
                if (rightIndex >= deviceCount || RoleMap.IsDeviceConnectedAndBound(rightIndex))
                {
                    rightIndex = VRModule.INVALID_DEVICE_INDEX;
                }
            }

            if (leftBound)
            {
                leftIndex = RoleMap.GetMappedDeviceByRole(HandRole.LeftHand);
            }
            else
            {
                leftIndex = VRModule.GetLeftControllerDeviceIndex();
                if (leftIndex >= deviceCount || RoleMap.IsDeviceConnectedAndBound(leftIndex))
                {
                    leftIndex = VRModule.INVALID_DEVICE_INDEX;
                }
            }

            // if not both left/right controllers are assigned, find and assign them with left/right most controller
            if (rightIndex >= deviceCount || leftIndex >= deviceCount)
            {
                for (uint i = 0u, imax = deviceCount; i < imax; ++i)
                {
                    if (i == rightIndex || i == leftIndex) { continue; }
                    var state = VRModule.GetCurrentDeviceState(i);
                    if (state.deviceClass != VRModuleDeviceClass.Controller) { continue; }
                    if (RoleMap.IsDeviceBound(state.serialNumber)) { continue; }
                    m_sortedDeviceList.Add(i);
                }

                if (m_sortedDeviceList.Count > 1)
                {
                    SortDeviceIndicesByDirection(m_sortedDeviceList, VRModule.GetCurrentDeviceState(VRModule.HMD_DEVICE_INDEX).pose);
                    if (rightIndex >= deviceCount) { rightIndex = m_sortedDeviceList[0]; }
                    if (leftIndex >= deviceCount) { leftIndex = m_sortedDeviceList[m_sortedDeviceList.Count - 1]; }
                    m_sortedDeviceList.Clear();
                }
                else if (m_sortedDeviceList.Count == 1)
                {
                    if (rightIndex >= deviceCount) { rightIndex = m_sortedDeviceList[0]; }
                    else if (leftIndex >= deviceCount) { leftIndex = m_sortedDeviceList[0]; }
                    m_sortedDeviceList.Clear();
                }
            }

            if (!rightBound)
            {
                if (rightIndex < deviceCount) { MappingRole(HandRole.RightHand, rightIndex); }
                else { UnmappingRole(HandRole.RightHand); }
            }

            if (!leftBound)
            {
                if (leftIndex < deviceCount) { MappingRole(HandRole.LeftHand, leftIndex); }
                else { UnmappingRole(HandRole.LeftHand); }
            }
        }

        private void MappingOtherControllers()
        {
            var deviceIndex = 0u;
            var nextRole = HandRole.LeftHand;
            var rightIndex = RoleMap.GetMappedDeviceByRole(HandRole.RightHand);
            var leftIndex = RoleMap.GetMappedDeviceByRole(HandRole.LeftHand);
            var exCamIndex = VRModule.INVALID_DEVICE_INDEX;

            // mapping ExternalCamera (skip if already bound)
            if (RoleMap.IsRoleBound(HandRole.ExternalCamera))
            {
                nextRole = HandRole.ExternalCamera;
                exCamIndex = RoleMap.GetMappedDeviceByRole(HandRole.ExternalCamera);
            }
            else
            {
                // mapping first found tracker as ExternalCamera
                for (uint i = 0u, imax = VRModule.GetDeviceStateCount(); deviceIndex < imax; ++deviceIndex)
                {
                    if (VRModule.GetCurrentDeviceState(i).deviceClass != VRModuleDeviceClass.GenericTracker) { continue; }
                    if (i == rightIndex || i == leftIndex) { continue; }

                    exCamIndex = deviceIndex;
                    MappingRole(HandRole.ExternalCamera, deviceIndex);
                    nextRole = HandRole.ExternalCamera;
                    break;
                }
            }

            // mapping other controllers in order of device index
            while (NextUnboundRole(ref nextRole))
            {
                if (NextUnboundCtrlOrTracker(ref deviceIndex, exCamIndex))
                {
                    MappingRole(nextRole, deviceIndex);
                }
                else
                {
                    UnmappingRole(nextRole);
                }
            }
        }

        private bool NextUnboundRole(ref HandRole r)
        {
            while ((r + 1) <= HandRole.Controller15)
            {
                ++r;
                if (RoleMap.IsRoleBound(r)) { continue; }
                return true;
            }
            return false;
        }

        private bool NextUnboundCtrlOrTracker(ref uint i, uint skip = VRModule.INVALID_DEVICE_INDEX)
        {
            var imax = VRModule.GetDeviceStateCount();
            while (i != VRModule.INVALID_DEVICE_INDEX && (i + 1) < imax)
            {
                ++i;
                var state = VRModule.GetCurrentDeviceState(i);
                if (i == skip) { continue; }
                if (IsController(state.deviceClass) && IsTracker(state.deviceClass)) { continue; }
                if (RoleMap.IsDeviceBound(state.serialNumber)) { continue; }
                return true;
            }
            return false;
        }

        private static readonly float[] s_deviceDirPoint = new float[VRModule.MAX_DEVICE_COUNT];
        public static void SortDeviceIndicesByDirection(List<uint> deviceList, RigidPose sortingReference)
        {
            if (deviceList == null || deviceList.Count == 0) { return; }

            for (int i = 0, imax = deviceList.Count; i < imax; ++i)
            {
                var deviceIndex = deviceList[i];
                if (!VRModule.IsValidDeviceIndex(deviceIndex)) { continue; }

                var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);
                if (deviceState.isConnected)
                {
                    var localPos = sortingReference.InverseTransformPoint(deviceState.pose.pos);
                    s_deviceDirPoint[deviceIndex] = GetDirectionPoint(new Vector2(localPos.x, localPos.z));
                }
                else
                {
                    s_deviceDirPoint[deviceIndex] = -1f;
                }
            }

            deviceList.Sort(CompareDirection);
        }

        [Obsolete]
        public static void SortDeviceIndicesByDirection(List<uint> deviceList, PoseTracker.Pose sortingReference)
        {
            SortDeviceIndicesByDirection(deviceList, sortingReference);
        }

        private static int CompareDirection(uint d1, uint d2)
        {
            var d1Point = s_deviceDirPoint[d1];
            var d2Point = s_deviceDirPoint[d2];
            var d1Valid = VRModule.IsValidDeviceIndex(d1) && d1Point >= 0f;
            var d2Valid = VRModule.IsValidDeviceIndex(d2) && d2Point >= 0f;

            if (!d1Valid || !d2Valid)
            {
                if (d1Valid) { return -1; }
                if (d2Valid) { return 1; }

                if (d1 < d2) { return -1; }
                if (d1 > d2) { return 1; }

                return 0;
            }

            if (d1Point < d2Point) { return -1; }
            if (d1Point > d2Point) { return 1; }

            return 0;
        }

        //          Y+
        //          ||
        //   \\  4  || 3   //  
        //     \\   ||   //    
        //    5  \\ ^^ //  2   
        // =========[]========= X+
        //    6  // || \\  1   
        //     //   ||   \\    
        //   //  7  || 0   \\  
        //          ||         
        // less point => right side
        public static float GetDirectionPoint(Vector2 pos)
        {
            var ax = Mathf.Abs(pos.x);
            var ay = Mathf.Abs(pos.y);
            if (pos.x > 0f)
            {
                if (pos.y < 0f)
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
                if (pos.y > 0f)
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
    }
}