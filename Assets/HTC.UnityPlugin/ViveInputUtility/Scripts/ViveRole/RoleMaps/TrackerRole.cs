//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;

namespace HTC.UnityPlugin.Vive
{
    [ViveRoleEnum((int)TrackerRole.Invalid)]
    public enum TrackerRole
    {
        Invalid,
        Tracker1,
        Tracker2,
        Tracker3,
        Tracker4,
        Tracker5,
        Tracker6,
        Tracker7,
        Tracker8,
        Tracker9,
        Tracker10,
        Tracker11,
        Tracker12,
        Tracker13,
    }

    public class TrackerRoleHandler : ViveRole.MapHandler<TrackerRole>
    {
        private bool IsTracker(uint deviceIndex)
        {
            return IsTracker(VRModule.GetCurrentDeviceState(deviceIndex).deviceClass);
        }

        private bool IsTracker(VRModuleDeviceClass deviceClass)
        {
            return deviceClass == VRModuleDeviceClass.GenericTracker;
        }

        public override void OnAssignedAsCurrentMapHandler() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (!RoleMap.IsDeviceBound(deviceSN) && !IsTracker(deviceClass)) { return; }

            Refresh();
        }

        public override void OnBindingChanged(string deviceSN, bool previousIsBound, TrackerRole previousRole, bool currentIsBound, TrackerRole currentRole)
        {
            uint deviceIndex;
            if (!VRModule.TryGetConnectedDeviceIndex(deviceSN, out deviceIndex)) { return; }

            Refresh();
        }

        public void Refresh()
        {
            MappingTrackers();
        }

        private void MappingTrackers()
        {
            var deviceIndex = 0u;
            for (var role = RoleInfo.MinValidRole; role <= RoleInfo.MaxValidRole; ++role)
            {
                if (!RoleInfo.IsValidRole(role)) { continue; }
                if (RoleMap.IsRoleBound(role)) { continue; }

                // find next valid device
                if (VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    while (!IsTracker(deviceIndex) || RoleMap.IsDeviceConnectedAndBound(deviceIndex))
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