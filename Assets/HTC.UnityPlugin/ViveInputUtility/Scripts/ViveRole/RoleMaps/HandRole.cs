//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

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
            var rightIndex = VRModule.GetRightControllerDeviceIndex();
            var leftIndex = VRModule.GetLeftControllerDeviceIndex();

            if (VRModule.GetCurrentDeviceState(rightIndex).isConnected)
            {
                MappingRoleIfUnbound(HandRole.RightHand, rightIndex);
                rightIndex = RoleMap.GetMappedDeviceByRole(HandRole.RightHand);
            }
            else if (RoleMap.IsRoleBound(HandRole.RightHand))
            {
                rightIndex = RoleMap.GetMappedDeviceByRole(HandRole.RightHand);
            }
            else
            {
                rightIndex = VRModule.INVALID_DEVICE_INDEX;
            }

            if (VRModule.GetCurrentDeviceState(leftIndex).isConnected && leftIndex != rightIndex)
            {
                MappingRoleIfUnbound(HandRole.LeftHand, leftIndex);
                leftIndex = RoleMap.GetMappedDeviceByRole(HandRole.LeftHand);
            }
            else if (RoleMap.IsRoleBound(HandRole.LeftHand))
            {
                leftIndex = RoleMap.GetMappedDeviceByRole(HandRole.LeftHand);
            }
            else
            {
                leftIndex = VRModule.INVALID_DEVICE_INDEX;
            }

            // if not both left/right controllers are assigned, find and assign them with left/right most controller
            if (!VRModule.IsValidDeviceIndex(rightIndex) || !VRModule.IsValidDeviceIndex(leftIndex))
            {
                // find right to left sorted controllers
                // FIXME: GetSortedTrackedDeviceIndicesOfClass doesn't return correct devices count right after device connected
#if __VIU_STEAMVR
                if (VRModule.activeModule == SupportedVRModule.SteamVR)
                {
                    var count = 0;
                    var system = Valve.VR.OpenVR.System;
                    if (system != null)
                    {
                        count = (int)system.GetSortedTrackedDeviceIndicesOfClass(Valve.VR.ETrackedDeviceClass.Controller, m_sortedDevices, Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd);
                    }

                    foreach (var deviceIndex in m_sortedDevices)
                    {
                        if (m_sortedDeviceList.Count >= count) { break; }
                        if (IsController(deviceIndex) && deviceIndex != rightIndex && deviceIndex != leftIndex && !RoleMap.IsDeviceConnectedAndBound(deviceIndex))
                        {
                            m_sortedDeviceList.Add(deviceIndex);
                        }
                    }
                }
                else
#endif
                {
                    for (uint deviceIndex = 1u, imax = VRModule.GetDeviceStateCount(); deviceIndex < imax; ++deviceIndex)
                    {
                        if (IsController(deviceIndex) && deviceIndex != rightIndex && deviceIndex != leftIndex && !RoleMap.IsDeviceConnectedAndBound(deviceIndex))
                        {
                            m_sortedDeviceList.Add(deviceIndex);
                        }
                    }

                    if (m_sortedDeviceList.Count > 1)
                    {
                        SortDeviceIndicesByDirection(m_sortedDeviceList, VRModule.GetCurrentDeviceState(VRModule.HMD_DEVICE_INDEX).pose);
                    }
                }

                if (m_sortedDeviceList.Count > 0 && !VRModule.IsValidDeviceIndex(rightIndex))
                {
                    rightIndex = m_sortedDeviceList[0];
                    m_sortedDeviceList.RemoveAt(0);
                    // mapping right most controller
                    MappingRole(HandRole.RightHand, rightIndex);
                }

                if (m_sortedDeviceList.Count > 0 && !VRModule.IsValidDeviceIndex(leftIndex))
                {
                    leftIndex = m_sortedDeviceList[m_sortedDeviceList.Count - 1];
                    // mapping left most controller
                    MappingRole(HandRole.LeftHand, leftIndex);
                }

                m_sortedDeviceList.Clear();
            }

            if (!VRModule.IsValidDeviceIndex(rightIndex)) { UnmappingRole(HandRole.RightHand); }
            if (!VRModule.IsValidDeviceIndex(leftIndex)) { UnmappingRole(HandRole.LeftHand); }
        }

        private void MappingOtherControllers()
        {
            // mapping other controllers in order of device index
            var deviceIndex = 0u;
            var firstFoundTracker = VRModule.INVALID_DEVICE_INDEX;
            var rightIndex = RoleMap.GetMappedDeviceByRole(HandRole.RightHand);
            var leftIndex = RoleMap.GetMappedDeviceByRole(HandRole.LeftHand);

            for (var role = RoleInfo.MinValidRole; role <= RoleInfo.MaxValidRole; ++role)
            {
                if (!RoleInfo.IsValidRole(role)) { continue; }
                if (role == HandRole.RightHand || role == HandRole.LeftHand) { continue; }
                if (RoleMap.IsRoleBound(role)) { continue; }

                // find next valid device
                if (VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    while (!IsController(deviceIndex) || RoleMap.IsDeviceConnectedAndBound(deviceIndex) || deviceIndex == rightIndex || deviceIndex == leftIndex)
                    {
                        if (!VRModule.IsValidDeviceIndex(firstFoundTracker) && IsTracker(deviceIndex)) { firstFoundTracker = deviceIndex; }
                        if (!VRModule.IsValidDeviceIndex(++deviceIndex)) { break; }
                    }
                }

                if (VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    MappingRole(role, deviceIndex++);
                }
                else
                {
                    UnmappingRole(role);
                }
            }

            // if external camera is not mapped, try mapping first found tracker
            if (!RoleMap.IsRoleMapped(HandRole.ExternalCamera) && VRModule.IsValidDeviceIndex(firstFoundTracker) && !RoleMap.IsDeviceConnectedAndBound(firstFoundTracker))
            {
                MappingRole(HandRole.ExternalCamera, firstFoundTracker);
            }
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