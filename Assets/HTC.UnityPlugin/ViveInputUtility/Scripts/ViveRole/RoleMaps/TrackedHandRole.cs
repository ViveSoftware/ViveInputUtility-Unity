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
    [ViveRoleEnum((int)TrackedHandRole.Invalid)]
    public enum TrackedHandRole
    {
        Invalid = -1,
        RightHand,
        LeftHand,
    }

    internal class TrackedHandRoleIntReslver : EnumToIntResolver<TrackedHandRole> { public override int Resolve(TrackedHandRole e) { return (int)e; } }

    public class TrackedHandRoleHandler : ViveRole.MapHandler<TrackedHandRole>
    {
        public override void OnAssignedAsCurrentMapHandler() { Refresh(); }

        public override void OnTrackedDeviceRoleChanged() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (!RoleMap.IsDeviceBound(deviceSN) && deviceClass != VRModuleDeviceClass.TrackedHand) { return; }
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
            // find tracked right/left hand index
            var deviceCount = VRModule.GetDeviceStateCount();
            var rightIndex = VRModule.INVALID_DEVICE_INDEX;
            var leftIndex = VRModule.INVALID_DEVICE_INDEX;

            for (uint deviceIndex = 0u; deviceIndex < deviceCount; ++deviceIndex)
            {
                var deviceState = VRModule.GetDeviceState(deviceIndex);
                if (deviceState.isConnected && deviceState.deviceClass == VRModuleDeviceClass.TrackedHand)
                {
                    if (deviceState.deviceModel.IsRight())
                    {
                        rightIndex = deviceIndex;
                    }
                    else if (deviceState.deviceModel.IsLeft())
                    {
                        leftIndex = deviceIndex;
                    }
                }
            }

            if (!RoleMap.IsRoleMapped(TrackedHandRole.RightHand))
            {
                if (rightIndex < deviceCount)
                {
                    MappingRoleIfUnbound(TrackedHandRole.RightHand, rightIndex);
                }
            }
            else if (!RoleMap.IsRoleBound(TrackedHandRole.RightHand))
            {
                if (rightIndex < deviceCount)
                {
                    MappingRoleIfUnbound(TrackedHandRole.RightHand, rightIndex);
                }
                else
                {
                    UnmappingRole(TrackedHandRole.RightHand);
                }
            }

            if (!RoleMap.IsRoleMapped(TrackedHandRole.LeftHand))
            {
                if (leftIndex < deviceCount)
                {
                    MappingRoleIfUnbound(TrackedHandRole.LeftHand, leftIndex);
                }
            }
            else if (!RoleMap.IsRoleBound(TrackedHandRole.LeftHand))
            {
                if (leftIndex < deviceCount)
                {
                    MappingRoleIfUnbound(TrackedHandRole.LeftHand, leftIndex);
                }
                else
                {
                    UnmappingRole(TrackedHandRole.LeftHand);
                }
            }
        }
    }
}