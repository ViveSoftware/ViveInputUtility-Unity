//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

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
    public enum TrackedHandRole
    {
        Invalid = -1,
        TrackedHandRight,
        TrackedHandLeft,
    }

    public class TrackedHandRoleHandler : ViveRole.MapHandler<TrackedHandRole>
    {
        private List<uint> m_sortedDeviceList = new List<uint>();

        // HandRole only tracks tracked hands
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
            if (!RoleMap.IsDeviceBound(deviceSN) && !IsTrackedHand(deviceClass)) { return; }
            Refresh();
        }

        public override void OnBindingChanged(string deviceSN, bool previousIsBound, TrackedHandRole previousRole, bool currentIsBound, TrackedHandRole currentRole)
        {
            uint deviceIndex;
            if (!VRModule.TryGetConnectedDeviceIndex(deviceSN, out deviceIndex)) { return; }

            Refresh();
        }

        public void Refresh()
        {
            MappingTrackedHands();
        }

        private void MappingTrackedHands()
        {
            var deviceIndex = 0u;
            for (var role = RoleInfo.MinValidRole; role <= RoleInfo.MaxValidRole; ++role)
            {
                if (!RoleInfo.IsValidRole(role)) { continue; }
                if (RoleMap.IsRoleBound(role)) { continue; }

                // find next valid device
                if (VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    while (!IsTrackedHand(deviceIndex) || RoleMap.IsDeviceConnectedAndBound(deviceIndex))
                    {
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
        }
    }
}