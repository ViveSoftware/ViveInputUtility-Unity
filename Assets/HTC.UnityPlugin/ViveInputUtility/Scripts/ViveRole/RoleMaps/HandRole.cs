//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using Valve.VR;

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
        private uint[] m_sortedDevices = new uint[ViveRole.MAX_DEVICE_COUNT];

        private bool handsAreMappedOrBound
        {
            get
            {
                return (RoleMap.IsRoleMapped(HandRole.RightHand) || RoleMap.IsRoleBound(HandRole.RightHand)) &&
                    (RoleMap.IsRoleMapped(HandRole.LeftHand) || RoleMap.IsRoleBound(HandRole.LeftHand));
            }
        }

        public override void OnInitialize()
        {
            MappingHandsAndOthers();
        }

        public override void OnConnectedDeviceChanged(uint deviceIndex, ETrackedDeviceClass deviceClass, string deviceSN, bool connected)
        {
            if (RoleMap.IsDeviceBound(deviceSN) || deviceClass != ETrackedDeviceClass.Controller) { return; }

            if (!connected)
            {
                UnmappingDevice(deviceIndex);
            }

            if (handsAreMappedOrBound)
            {
                MappingOthers();
            }
            else
            {
                MappingHandsAndOthers();
            }
        }

        public override void OnBindingChanged(HandRole role, string deviceSN, bool bound)
        {
            if (!bound)
            {
                if (RoleMap.IsRoleMapped(role) && !IsController(RoleMap.GetMappedDeviceByRole(role)))
                {
                    UnmappingRole(role);
                }
            }

            if (handsAreMappedOrBound)
            {
                MappingOthers();
            }
            else
            {
                MappingHandsAndOthers();
            }
        }

        public override void OnTrackedDeviceRoleChanged()
        {
            MappingHandsAndOthers();
        }

        private bool IsController(uint deviceIndex)
        {
            return ViveRole.GetDeviceClass(deviceIndex) == ETrackedDeviceClass.Controller;
        }

        // unmapping all and mapping only right/left hands
        private void MappingHandsAndOthers()
        {
            UnmappingAll();

            var system = OpenVR.System;
            var rightIndex = ViveRole.INVALID_DEVICE_INDEX;
            var leftIndex = ViveRole.INVALID_DEVICE_INDEX;

            var trackedControllerCount = 0;

            if (system != null)
            {
                leftIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
                rightIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);

                if (RoleMap.IsDeviceMapped(leftIndex)) { leftIndex = ViveRole.INVALID_DEVICE_INDEX; }
                if (RoleMap.IsDeviceMapped(rightIndex)) { rightIndex = ViveRole.INVALID_DEVICE_INDEX; }

                trackedControllerCount = (int)system.GetSortedTrackedDeviceIndicesOfClass(ETrackedDeviceClass.Controller, m_sortedDevices, 0);
            }

            if (ViveRole.IsValidIndex(rightIndex))
            {
                MappingRoleIfUnbound(HandRole.RightHand, rightIndex);
            }

            if (ViveRole.IsValidIndex(leftIndex) && leftIndex != rightIndex)
            {
                MappingRoleIfUnbound(HandRole.LeftHand, leftIndex);
            }

            if (!RoleMap.IsRoleMapped(HandRole.RightHand) && !RoleMap.IsRoleBound(HandRole.RightHand))
            {
                // find most right side controller
                for (var i = 0; i < trackedControllerCount; ++i)
                {
                    if (RoleMap.IsDeviceMapped(m_sortedDevices[i])) { continue; }
                    MappingRole(HandRole.RightHand, m_sortedDevices[i]);
                    break;
                }
            }

            if (!RoleMap.IsRoleMapped(HandRole.LeftHand) && !RoleMap.IsRoleBound(HandRole.LeftHand))
            {
                // find most left side controller
                for (var i = trackedControllerCount - 1; i >= 0; --i)
                {
                    if (RoleMap.IsDeviceMapped(m_sortedDevices[i])) { continue; }
                    MappingRole(HandRole.LeftHand, m_sortedDevices[i]);
                    break;
                }
            }

            MappingOthers();
        }

        private void MappingOthers()
        {
            // try mapping the rest of the devices
            var role = HandRole.Controller3;
            var deviceIndex = 0u;

            while (role <= HandRole.Controller12 && deviceIndex < ViveRole.MAX_DEVICE_COUNT)
            {
                while (RoleMap.IsRoleMapped(role) || RoleMap.IsRoleBound(role))
                {
                    if (++role > HandRole.Controller12) { return; }
                }

                while (!IsController(deviceIndex) || RoleMap.IsDeviceMapped(deviceIndex))
                {
                    if (++deviceIndex >= ViveRole.MAX_DEVICE_COUNT) { return; }
                }

                MappingRole(role++, deviceIndex++);
            }
        }
    }
}