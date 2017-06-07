//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// Provide static APIs to retrieve device index by semantic role
    /// Same mapping logic as SteamVR_ControllerManager does
    /// </summary>
    public static partial class ViveRole
    {
        public const uint MAX_DEVICE_COUNT = VRModule.MAX_DEVICE_COUNT;
        public const uint INVALID_DEVICE_INDEX = VRModule.INVALID_DEVICE_INDEX;

        private readonly static Dictionary<string, uint> s_serialNum2device = new Dictionary<string, uint>((int)MAX_DEVICE_COUNT);

        public readonly static DeviceRoleHandler DefaultDeviceRoleHandler = new DeviceRoleHandler();
        public readonly static HandRoleHandler DefaultHandRoleHandler = new HandRoleHandler();
        public readonly static TrackerRoleHandler DefaultTrackerRoleHandler = new TrackerRoleHandler();
        public readonly static BodyRoleHandler DefaultBodyRoleHandler = new BodyRoleHandler();

        static ViveRole()
        {
            // update the ViveRole system with initial connecting state
            for (uint index = 0; index < MAX_DEVICE_COUNT; ++index)
            {
                OnDeviceConnected(index, VivePose.IsConnected(index));
            }

            VRModule.onDeviceConnected.AddListener(OnDeviceConnected);
            VRModule.onControllerRoleChanged.AddListener(OnTrackedDeviceRoleChanged);

            // assign default role map handlers
            AssignMapHandler(DefaultDeviceRoleHandler);
            AssignMapHandler(DefaultHandRoleHandler);
            AssignMapHandler(DefaultTrackerRoleHandler);
            AssignMapHandler(DefaultBodyRoleHandler);
        }

        private static void OnDeviceConnected(uint deviceIndex, bool connected)
        {
            var prevState = VRModule.GetPreviousDeviceState(deviceIndex);
            var currState = VRModule.GetCurrentDeviceState(deviceIndex);
            
            // update serial number table and model number table
            if (connected)
            {
                s_serialNum2device[currState.deviceSerialID] = deviceIndex;

                // inform all role map handlers that a device connected or disconnected
                for (int i = s_mapTable.Count - 1; i >= 0; --i)
                {
                    s_mapTable.GetValueByIndex(i).OnConnectedDeviceChanged(deviceIndex, currState.deviceClass, currState.deviceSerialID, true);
                }
            }
            else
            {
                s_serialNum2device.Remove(prevState.deviceSerialID);

                // inform all role map handlers that a device connected or disconnected
                for (int i = s_mapTable.Count - 1; i >= 0; --i)
                {
                    s_mapTable.GetValueByIndex(i).OnConnectedDeviceChanged(deviceIndex, prevState.deviceClass, prevState.deviceSerialID, false);
                }
            }
        }

        private static void OnTrackedDeviceRoleChanged()
        {
            for (int i = s_mapTable.Count - 1; i >= 0; --i)
            {
                s_mapTable.GetValueByIndex(i).OnTrackedDeviceRoleChanged();
            }
        }

        public static bool TryGetDeviceIndexBySerialNumber(string serialNumber, out uint deviceIndex)
        {
            return s_serialNum2device.TryGetValue(serialNumber, out deviceIndex);
        }

        public static string GetModelNumber(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? VRModule.GetCurrentDeviceState(deviceIndex).deviceModelNumber : string.Empty;
        }

        public static string GetSerialNumber(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? VRModule.GetCurrentDeviceState(deviceIndex).deviceSerialID : string.Empty;
        }

        public static VRModuleDeviceClass GetDeviceClass(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? VRModule.GetCurrentDeviceState(deviceIndex).deviceClass : VRModuleDeviceClass.Invalid;
        }

        /// <summary>
        /// Returns device index of the device identified by the role
        /// Returns INVALID_DEVICE_INDEX if the role doesn't assign to any device
        /// </summary>
        /// <returns>Current device index assigned to the role, should be tested by ViveRole.IsValidIndex before using it</returns>
        public static uint GetDeviceIndex(HandRole role)
        {
            return GetDeviceIndexEx(role);
        }

        /// <summary>
        /// Returns device index of the device identified by the role
        /// Returns INVALID_DEVICE_INDEX if the role doesn't assign to any device
        /// </summary>
        /// <returns>Current device index assigned to the role, should be tested by ViveRole.IsValidIndex before using it</returns>
        public static uint GetDeviceIndex(DeviceRole role)
        {
            return GetDeviceIndexEx(role);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static uint GetDeviceIndexEx<TRole>(TRole role)
        {
            return GetMap<TRole>().GetMappedDeviceByRole(role);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static uint GetDeviceIndexEx(Type type, int roleValue)
        {
            return GetMap(type).GetMappedDeviceByRoleValue(roleValue);
        }

        /// <summary>
        /// Check if the device index is valid to be used
        /// </summary>
        public static bool IsValidIndex(uint index) { return index < MAX_DEVICE_COUNT; }
    }
}