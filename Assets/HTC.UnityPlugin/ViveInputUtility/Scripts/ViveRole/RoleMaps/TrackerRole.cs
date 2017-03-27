//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using Valve.VR;

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
            return ViveRole.GetDeviceClass(deviceIndex) == ETrackedDeviceClass.GenericTracker;
        }

        public override void OnInitialize()
        {
            UnmappingAll();

            MappingTrackers();
        }

        public override void OnConnectedDeviceChanged(uint deviceIndex, ETrackedDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (RoleMap.IsDeviceBound(deviceSN)) { return; }

            if (connected)
            {
                if (deviceClass != ETrackedDeviceClass.GenericTracker) { return; }

                // find unmapped role
                var role = RoleMap.RoleInfo.MinValidRole;
                while (!RoleMap.RoleInfo.IsValidRole(role) || RoleMap.IsRoleMapped(role))
                {
                    if (++role > RoleMap.RoleInfo.MaxValidRole) { return; }
                }

                MappingRole(role, deviceIndex);
            }
            else
            {
                UnmappingDevice(deviceIndex);
            }
        }

        public override void OnBindingChanged(TrackerRole role, string deviceSN, bool bound)
        {
            if (bound)
            {
                // it's possible that some device will be pushed out when binding
                MappingTrackers();
            }
            else
            {
                if (RoleMap.IsRoleMapped(role))
                {
                    UnmappingRole(role);

                    MappingTrackers();
                }
            }
        }

        public override void OnTrackedDeviceRoleChanged() { }

        private void MappingTrackers()
        {
            var role = RoleMap.RoleInfo.MinValidRole;
            var index = (uint)1;

            while (true)
            {
                while (!RoleMap.RoleInfo.IsValidRole(role) || RoleMap.IsRoleMapped(role) || RoleMap.IsRoleBound(role))
                {
                    if (++role > RoleMap.RoleInfo.MaxValidRole) { return; }
                }

                while (ViveRole.GetDeviceClass(index) != ETrackedDeviceClass.GenericTracker || RoleMap.IsDeviceMapped(index) || RoleMap.IsDeviceConnectedAndBound(index))
                {
                    if (++index >= ViveRole.MAX_DEVICE_COUNT) { return; }
                }

                MappingRole(role++, index++);

                if (role > RoleMap.RoleInfo.MaxValidRole || index >= ViveRole.MAX_DEVICE_COUNT) { return; }
            }
        }
    }
}