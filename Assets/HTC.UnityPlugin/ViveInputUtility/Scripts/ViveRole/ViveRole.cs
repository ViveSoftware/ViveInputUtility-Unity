//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.Text;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// Provide static APIs to retrieve device index by semantic role
    /// Same mapping logic as SteamVR_ControllerManager does
    /// </summary>
    public static partial class ViveRole
    {
        public const uint MAX_DEVICE_COUNT = OpenVR.k_unMaxTrackedDeviceCount;
        public const uint INVALID_DEVICE_INDEX = OpenVR.k_unTrackedDeviceIndexInvalid;

        private readonly static StringBuilder s_propStrBuilder = new StringBuilder();

        private readonly static ETrackedDeviceClass[] s_class = new ETrackedDeviceClass[MAX_DEVICE_COUNT];
        private readonly static string[] s_modelNum = new string[MAX_DEVICE_COUNT]; // device model number
        private readonly static string[] s_serialNum = new string[MAX_DEVICE_COUNT]; // device serial number

        private readonly static Dictionary<string, uint> s_serialNum2device = new Dictionary<string, uint>((int)MAX_DEVICE_COUNT);

        public readonly static DeviceRoleHandler DefaultDeviceRoleHandler = new DeviceRoleHandler();
        public readonly static HandRoleHandler DefaultHandRoleHandler = new HandRoleHandler();
        public readonly static TrackerRoleHandler DefaultTrackerRoleHandler = new TrackerRoleHandler();
        public readonly static BodyRoleHandler DefaultBodyRoleHandler = new BodyRoleHandler();

        static ViveRole()
        {
            for (uint i = 0; i < MAX_DEVICE_COUNT; ++i)
            {
                s_class[i] = ETrackedDeviceClass.Invalid;
                s_modelNum[i] = string.Empty;
                s_serialNum[i] = string.Empty;
            }

            // update the ViveRole system with initial connecting state
            var system = OpenVR.System;
            if (system == null)
            {
                for (int index = 0; index < MAX_DEVICE_COUNT; ++index)
                {
                    OnDeviceConnected(index, false);
                }
            }
            else
            {
                for (int index = 0; index < MAX_DEVICE_COUNT; ++index)
                {
                    OnDeviceConnected(index, system.IsTrackedDeviceConnected((uint)index));
                }
            }

            SteamVR_Events.DeviceConnectedAction(OnDeviceConnected).Enable(true);
            SteamVR_Events.SystemAction(EVREventType.VREvent_TrackedDeviceRoleChanged, OnTrackedDeviceRoleChanged).Enable(true);

            // assign default role map handlers
            AssignMapHandler(DefaultDeviceRoleHandler);
            AssignMapHandler(DefaultHandRoleHandler);
            AssignMapHandler(DefaultTrackerRoleHandler);
            AssignMapHandler(DefaultBodyRoleHandler);
        }

        private static void OnDeviceConnected(int index, bool connected)
        {
            var system = OpenVR.System;
            var deviceIndex = (uint)index;
            var serialNum = s_serialNum[deviceIndex];
            var deviceClass = s_class[deviceIndex];

            if (connected && deviceClass != ETrackedDeviceClass.Invalid) { return; } // already connected in structure
            if (!connected && deviceClass == ETrackedDeviceClass.Invalid) { return; } // already disconnected in structure

            // update serial number table and model number table
            if (system != null && connected)
            {
                s_class[deviceIndex] = deviceClass = system.GetTrackedDeviceClass(deviceIndex);

                if (QueryDeviceStringProperty(deviceIndex, ETrackedDeviceProperty.Prop_SerialNumber_String, out serialNum) && !string.IsNullOrEmpty(serialNum))
                {
                    s_serialNum[deviceIndex] = serialNum;
                    s_serialNum2device[serialNum] = deviceIndex;
                }

                string modelNum;
                if (QueryDeviceStringProperty(deviceIndex, ETrackedDeviceProperty.Prop_ModelNumber_String, out modelNum) && !string.IsNullOrEmpty(modelNum))
                {
                    s_modelNum[deviceIndex] = modelNum;
                }
            }
            else
            {
                s_class[deviceIndex] = ETrackedDeviceClass.Invalid;

                s_serialNum2device.Remove(s_serialNum[deviceIndex]);
                s_serialNum[deviceIndex] = string.Empty;

                s_modelNum[deviceIndex] = string.Empty;
            }

            // inform all role map handlers that a device connected or disconnected
            for (int i = s_mapTable.Count - 1; i >= 0; --i)
            {
                s_mapTable.GetValueByIndex(i).OnConnectedDeviceChanged(deviceIndex, deviceClass, serialNum, connected);
            }
        }

        private static void OnTrackedDeviceRoleChanged(VREvent_t arg = default(VREvent_t))
        {
            for (int i = s_mapTable.Count - 1; i >= 0; --i)
            {
                s_mapTable.GetValueByIndex(i).OnTrackedDeviceRoleChanged();
            }
        }

        private static bool QueryDeviceStringProperty(uint deviceIndex, ETrackedDeviceProperty prop, out string propValue)
        {
            propValue = string.Empty;

            if (!IsValidIndex(deviceIndex)) { return false; }

            var system = OpenVR.System;
            if (system == null) { return false; }

            var error = default(ETrackedPropertyError);
            var capacity = (int)system.GetStringTrackedDeviceProperty(deviceIndex, prop, null, 0, ref error);
            if (capacity <= 1 || capacity > 128) { return false; }

            system.GetStringTrackedDeviceProperty(deviceIndex, prop, s_propStrBuilder, (uint)s_propStrBuilder.EnsureCapacity(capacity), ref error);
            if (error != ETrackedPropertyError.TrackedProp_Success) { return false; }

            propValue = s_propStrBuilder.ToString();
            s_propStrBuilder.Length = 0;

            return true;
        }

        public static bool TryGetDeviceIndexBySerialNumber(string serialNumber, out uint deviceIndex)
        {
            return s_serialNum2device.TryGetValue(serialNumber, out deviceIndex);
        }

        public static string GetModelNumber(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? s_modelNum[deviceIndex] : string.Empty;
        }

        public static string GetSerialNumber(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? s_serialNum[deviceIndex] : string.Empty;
        }

        public static ETrackedDeviceClass GetDeviceClass(uint deviceIndex)
        {
            return IsValidIndex(deviceIndex) ? s_class[deviceIndex] : ETrackedDeviceClass.Invalid;
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