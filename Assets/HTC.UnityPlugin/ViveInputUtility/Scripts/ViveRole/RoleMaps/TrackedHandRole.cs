//========= Copyright 2016-2024, HTC Corporation. All rights reserved. ===========

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
        private static int CompareDevice(IVRModuleDeviceState a, IVRModuleDeviceState b)
        {
            var c = a.isPoseValid.CompareTo(b.isPoseValid);
            if (c != 0) { return -c; }

            foreach (var j in JointEnumArray.StaticEnums)
            {
                c = a.readOnlyHandJoints[j].isValid.CompareTo(b.readOnlyHandJoints[j].isValid);
                if (c != 0) { return -c; }
            }

            return a.deviceIndex.CompareTo(b.deviceIndex);
        }

        private List<IVRModuleDeviceState> rightTrackedHandDevices = new List<IVRModuleDeviceState>();
        private List<IVRModuleDeviceState> leftTrackedHandDevices = new List<IVRModuleDeviceState>();
        public void Refresh()
        {
            // find tracked right/left hand index
            var deviceCount = VRModule.GetDeviceStateCount();
            var rightIndex = VRModule.INVALID_DEVICE_INDEX;
            var leftIndex = VRModule.INVALID_DEVICE_INDEX;

            // find proper left/right tracked hand
            for (uint deviceIndex = 0u; deviceIndex < deviceCount; ++deviceIndex)
            {
                var deviceState = VRModule.GetDeviceState(deviceIndex);
                if (deviceState.isConnected == false) { continue; }
                if (deviceState.deviceClass != VRModuleDeviceClass.TrackedHand) { continue; }
                if (deviceState.deviceModel.IsRight())
                {
                    rightTrackedHandDevices.Add(deviceState);
                }
                else if (deviceState.deviceModel.IsLeft())
                {
                    leftTrackedHandDevices.Add(deviceState);
                }
            }

            if (rightTrackedHandDevices.Count != 0)
            {
                if (rightTrackedHandDevices.Count != 1) { rightTrackedHandDevices.Sort(CompareDevice); }
                rightIndex = rightTrackedHandDevices[0].deviceIndex;
            }
            if (leftTrackedHandDevices.Count != 0)
            {
                if (leftTrackedHandDevices.Count != 1) { leftTrackedHandDevices.Sort(CompareDevice); }
                leftIndex = leftTrackedHandDevices[0].deviceIndex;
            }

            rightTrackedHandDevices.Clear();
            leftTrackedHandDevices.Clear();
            
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