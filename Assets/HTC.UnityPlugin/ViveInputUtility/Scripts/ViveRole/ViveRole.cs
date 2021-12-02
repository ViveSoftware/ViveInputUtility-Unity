//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// Provide static APIs to retrieve device index by semantic role
    /// Same mapping logic as SteamVR_ControllerManager does
    /// </summary>
    public static partial class ViveRole
    {
        [Obsolete("Use VRModule.MAX_DEVICE_COUNT instead")]
        public const uint MAX_DEVICE_COUNT = VRModule.MAX_DEVICE_COUNT;
        [Obsolete("Use VRModule.INVALID_DEVICE_INDEX instead")]
        public const uint INVALID_DEVICE_INDEX = VRModule.INVALID_DEVICE_INDEX;

        public readonly static DeviceRoleHandler DefaultDeviceRoleHandler = new DeviceRoleHandler();
        public readonly static HandRoleHandler DefaultHandRoleHandler = new HandRoleHandler();
        public readonly static TrackerRoleHandler DefaultTrackerRoleHandler = new TrackerRoleHandler();
        public readonly static TrackedHandRoleHandler DefaultTrackedHandRoleHandler = new TrackedHandRoleHandler();
        public readonly static BodyRoleHandler DefaultBodyRoleHandler = new BodyRoleHandler();
        public readonly static ControllerRoleHandler DefaultControllerRoleHandler = new ControllerRoleHandler();
        public readonly static PrimaryHandRoleHandler DefaultPrimaryHandRoleHandler = new PrimaryHandRoleHandler();

        private static bool s_initialized = false;

        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (VRModule.Active && VRModule.activeModule != VRModuleActiveEnum.Uninitialized)
            {
                Initialize();
            }
            else
            {
                VRModule.onActiveModuleChanged += OnModuleActive;
            }
        }

        private static void OnModuleActive(VRModuleActiveEnum activatedModule) { Initialize(); }
        
        public static void Initialize()
        {
            if (s_initialized || !Application.isPlaying) { return; }
            s_initialized = true;

            // update the ViveRole system with initial connecting state
            for (uint index = 0; index < VRModule.MAX_DEVICE_COUNT; ++index)
            {
                OnDeviceConnected(index, VivePose.IsConnected(index));
            }

            VRModule.onDeviceConnected += OnDeviceConnected;
            VRModule.onControllerRoleChanged += OnTrackedDeviceRoleChanged;

            // assign default role map handlers
            AssignMapHandler(DefaultDeviceRoleHandler);
            AssignMapHandler(DefaultHandRoleHandler);
            AssignMapHandler(DefaultTrackerRoleHandler);
            AssignMapHandler(DefaultTrackedHandRoleHandler);
            AssignMapHandler(DefaultBodyRoleHandler);
            AssignMapHandler(DefaultControllerRoleHandler);
            AssignMapHandler(DefaultPrimaryHandRoleHandler);
        }

        private static void OnDeviceConnected(uint deviceIndex, bool connected)
        {
            var prevState = VRModule.GetPreviousDeviceState(deviceIndex);
            var currState = VRModule.GetCurrentDeviceState(deviceIndex);

            // update serial number table and model number table
            if (connected)
            {
                for (int i = s_mapTable.Count - 1; i >= 0; --i)
                {
                    s_mapTable.GetValueByIndex(i).OnConnectedDeviceChanged(deviceIndex, currState.deviceClass, currState.serialNumber, true);
                }
            }
            else
            {
                for (int i = s_mapTable.Count - 1; i >= 0; --i)
                {
                    s_mapTable.GetValueByIndex(i).OnConnectedDeviceChanged(deviceIndex, prevState.deviceClass, prevState.serialNumber, false);
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

        [Obsolete("Use VRModule.TryGetDeviceIndex instead")]
        public static bool TryGetDeviceIndexBySerialNumber(string serialNumber, out uint deviceIndex)
        {
            return VRModule.TryGetConnectedDeviceIndex(serialNumber, out deviceIndex);
        }

        [Obsolete("Use VRModule.GetCurrentDeviceState(deviceIndex).modelNumber instead")]
        public static string GetModelNumber(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? VRModule.GetCurrentDeviceState(deviceIndex).modelNumber : string.Empty;
        }

        [Obsolete("Use VRModule.GetCurrentDeviceState(deviceIndex).serialNumber instead")]
        public static string GetSerialNumber(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? VRModule.GetCurrentDeviceState(deviceIndex).serialNumber : string.Empty;
        }

        [Obsolete("Use VRModule.GetCurrentDeviceState(deviceIndex).deviceClass instead")]
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

        [Obsolete("Use VRModule.IsValidDeviceIndex instead")]
        public static bool IsValidIndex(uint index) { return VRModule.IsValidDeviceIndex(index); }
    }
}