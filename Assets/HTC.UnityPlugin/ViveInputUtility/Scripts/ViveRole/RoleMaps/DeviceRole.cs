//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using Valve.VR;

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
        [HideMamber]
        [Obsolete("Use HandRole.RightHand instead")]
        RightHand = Device1,
        [HideMamber]
        [Obsolete("Use HandRole.LeftHand instead")]
        LeftHand,
        [HideMamber]
        [Obsolete("Use HandRole.Controller3 instead")]
        Controller3,
        [HideMamber]
        [Obsolete("Use HandRole.Controller4 instead")]
        Controller4,
        [HideMamber]
        [Obsolete("Use HandRole.Controller5 instead")]
        Controller5,
        [HideMamber]
        [Obsolete("Use HandRole.Controller6 instead")]
        Controller6,
        [HideMamber]
        [Obsolete("Use HandRole.Controller7 instead")]
        Controller7,
        [HideMamber]
        [Obsolete("Use HandRole.Controller8 instead")]
        Controller8,
        [HideMamber]
        [Obsolete("Use HandRole.Controller9 instead")]
        Controller9,
        [HideMamber]
        [Obsolete("Use HandRole.Controller10 instead")]
        Controller10,
        [HideMamber]
        [Obsolete("Use HandRole.Controller11 instead")]
        Controller11,
        [HideMamber]
        [Obsolete("Use HandRole.Controller12 instead")]
        Controller12,
        [HideMamber]
        [Obsolete("Use HandRole.Controller13 instead")]
        Controller13,
        [HideMamber]
        [Obsolete("Use HandRole.Controller14 instead")]
        Controller14,
        [HideMamber]
        [Obsolete("Use HandRole.Controller15 instead")]
        Controller15,
    }

    public class DeviceRoleHandler : ViveRole.MapHandler<DeviceRole>
    {
        public override void OnInitialize() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, ETrackedDeviceClass deviceClass, string deviceSN, bool connected) { Refresh(); }

        public override void OnBindingChanged(DeviceRole role, string deviceSN, bool bound) { Refresh(); }

        public override void OnTrackedDeviceRoleChanged() { Refresh(); }

        public void Refresh()
        {
            UnmappingAll();

            var role = DeviceRole.Hmd;
            var deviceIndex = 0u;
            for (; role <= DeviceRole.Device15 && deviceIndex < ViveRole.MAX_DEVICE_COUNT; ++role, ++deviceIndex)
            {
                if (ViveRole.GetDeviceClass(deviceIndex) == ETrackedDeviceClass.Invalid) { continue; }
                MappingRoleIfUnbound(role, deviceIndex);
            }
        }
    }
}
