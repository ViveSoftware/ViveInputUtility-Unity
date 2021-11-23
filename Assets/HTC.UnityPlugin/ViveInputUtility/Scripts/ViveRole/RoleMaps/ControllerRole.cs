//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// Only mapping to device with Controller class
    /// Won't map to Tracker or TrackedHand
    /// </summary>
    [ViveRoleEnum((int)ControllerRole.Invalid)]
    public enum ControllerRole
    {
        Invalid = -1,
        RightHand,
        LeftHand,
        Controller3,
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

    internal class ControllerRoleIntReslver : EnumToIntResolver<ControllerRole> { public override int Resolve(ControllerRole e) { return (int)e; } }


    public class ControllerRoleHandler : ViveRole.MapHandler<ControllerRole>
    {
        private List<uint> m_sortedDeviceList = new List<uint>();

        public override void OnAssignedAsCurrentMapHandler() { Refresh(); }

        public override void OnTrackedDeviceRoleChanged() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (deviceClass == VRModuleDeviceClass.Controller) { Refresh(); }
        }

        public override void OnBindingChanged(string deviceSN, bool previousIsBound, ControllerRole previousRole, bool currentIsBound, ControllerRole currentRole) { Refresh(); }

        public void Refresh()
        {
            // find right/left controller index
            var rightIndex = VRModule.INVALID_DEVICE_INDEX;
            var leftIndex = VRModule.INVALID_DEVICE_INDEX;
            if (RoleMap.IsRoleValueBound((int)ControllerRole.RightHand))
            {
                rightIndex = RoleMap.GetMappedDeviceByRoleValue((int)ControllerRole.RightHand);
            }
            else
            {
                var index = VRModule.GetRightControllerDeviceIndex();
                if (VRModule.GetDeviceState(index).deviceClass == VRModuleDeviceClass.Controller)
                {
                    rightIndex = index;
                }
            }
            if (RoleMap.IsRoleValueBound((int)ControllerRole.LeftHand))
            {
                leftIndex = RoleMap.GetMappedDeviceByRoleValue((int)ControllerRole.LeftHand);
            }
            else
            {
                var index = VRModule.GetLeftControllerDeviceIndex();
                if (VRModule.GetDeviceState(index).deviceClass == VRModuleDeviceClass.Controller)
                {
                    leftIndex = index;
                }
            }
            if (leftIndex == rightIndex) { leftIndex = VRModule.INVALID_DEVICE_INDEX; }

            // find all other unbound controller class devices
            for (uint i = 0u, imax = VRModule.GetDeviceStateCount(); i < imax; ++i)
            {
                if (i == rightIndex) { continue; }
                if (i == leftIndex) { continue; }

                var device = VRModule.GetDeviceState(i);
                if (!device.isConnected) { continue; }
                if (device.deviceClass != VRModuleDeviceClass.Controller) { continue; }
                if (RoleMap.IsDeviceBound(device.serialNumber)) { continue; }

                m_sortedDeviceList.Add(i);
            }

            // if module didn't hint left/right controllers
            // find left/right most device in m_sortedDeviceList and assigned to leftIndex/rightIndex
            if (m_sortedDeviceList.Count > 0)
            {
                HandRoleHandler.SortDeviceIndicesByDirection(m_sortedDeviceList, VRModule.GetCurrentDeviceState(VRModule.HMD_DEVICE_INDEX).pose);

                if (rightIndex == VRModule.INVALID_DEVICE_INDEX && !RoleMap.IsRoleBound(ControllerRole.RightHand))
                {
                    rightIndex = m_sortedDeviceList[0];
                    m_sortedDeviceList.RemoveAt(0);
                }

                if (m_sortedDeviceList.Count > 0 && leftIndex == VRModule.INVALID_DEVICE_INDEX && !RoleMap.IsRoleBound(ControllerRole.RightHand))
                {
                    leftIndex = m_sortedDeviceList[m_sortedDeviceList.Count - 1];
                    m_sortedDeviceList.RemoveAt(m_sortedDeviceList.Count - 1);
                }
            }

            if (rightIndex != VRModule.INVALID_DEVICE_INDEX)
            {
                MappingRoleIfUnbound(ControllerRole.RightHand, rightIndex);
            }

            if (leftIndex != VRModule.INVALID_DEVICE_INDEX)
            {
                MappingRoleIfUnbound(ControllerRole.LeftHand, leftIndex);
            }

            if (m_sortedDeviceList.Count > 0)
            {
                var otherCtrlIndex = -1;
                var otherRole = ControllerRole.Controller3 - 1;
                while (NextUnmappedSortedDevice(ref otherCtrlIndex) && NextUnmappedRole(ref otherRole))
                {
                    MappingRole(otherRole, m_sortedDeviceList[otherCtrlIndex]);
                }

                m_sortedDeviceList.Clear();
            }
        }

        private bool NextUnmappedSortedDevice(ref int i)
        {
            while (++i < m_sortedDeviceList.Count)
            {
                if (!RoleMap.IsDeviceMapped(m_sortedDeviceList[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private bool NextUnmappedRole(ref ControllerRole r)
        {
            const ControllerRole rMax = ControllerRole.Controller15 + 1;
            while (++r < rMax)
            {
                if (!RoleMap.IsRoleValueMapped((int)r))
                {
                    return true;
                }
            }
            return false;
        }
    }
}