//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// Defines roles for those devices that have tracking data
    /// </summary>
    [ViveRoleEnum((int)DeviceRole.Invalid)]
    public enum DeviceRole
    {
        Invalid = -2,
        Hmd,
        Device1,
        Device2,
        Device3,
        Device4,
        Device5,
        Device6,
        Device7,
        Device8,
        Device9,
        Device10,
        Device11,
        Device12,
        Device13,
        Device14,
        Device15,
        [HideInInspector]
        [Obsolete("Use HandRole.RightHand instead")]
        RightHand = Device1,
        [HideInInspector]
        [Obsolete("Use HandRole.LeftHand instead")]
        LeftHand,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller3 instead")]
        Controller3,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller4 instead")]
        Controller4,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller5 instead")]
        Controller5,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller6 instead")]
        Controller6,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller7 instead")]
        Controller7,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller8 instead")]
        Controller8,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller9 instead")]
        Controller9,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller10 instead")]
        Controller10,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller11 instead")]
        Controller11,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller12 instead")]
        Controller12,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller13 instead")]
        Controller13,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller14 instead")]
        Controller14,
        [HideInInspector]
        [Obsolete("Use HandRole.Controller15 instead")]
        Controller15,
    }

    internal class DeviceRoleIntReslver : EnumToIntResolver<DeviceRole> { public override int Resolve(DeviceRole e) { return (int)e; } }

    public class DeviceRoleHandler : ViveRole.MapHandler<DeviceRole>
    {
        public override bool BlockBindings { get { return true; } }

        public override void OnAssignedAsCurrentMapHandler() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (connected)
            {
                if (RoleMap.IsDeviceBound(deviceSN)) { return; }
            }
            else
            {
                return;
            }

            Refresh();
        }

        public override void OnBindingChanged(string deviceSN, bool previousIsBound, DeviceRole previousRole, bool currentIsBound, DeviceRole currentRole)
        {
            uint deviceIndex;
            if (!VRModule.TryGetConnectedDeviceIndex(deviceSN, out deviceIndex)) { return; }

            Refresh();
        }

        public void Refresh()
        {
            var deviceIndex = 0u;
            for (var role = RoleInfo.MinValidRole; role <= RoleInfo.MaxValidRole && deviceIndex < VRModule.MAX_DEVICE_COUNT; ++role, ++deviceIndex)
            {
                if (!RoleInfo.IsValidRole(role)) { continue; }

                if (VRModule.GetCurrentDeviceState(deviceIndex).isConnected)
                {
                    MappingRoleIfUnbound(role, deviceIndex);
                }
                else
                {
                    UnmappingRole(role);
                }
            }
        }
    }
}
