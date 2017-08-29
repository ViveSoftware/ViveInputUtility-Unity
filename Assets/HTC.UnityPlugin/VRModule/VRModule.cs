//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public enum VRModuleTrackingSpaceType
    {
        Stationary,
        RoomScale,
    }

    public enum VRModuleSelectEnum
    {
        Auto = -1,
        None = 0,
        //Simulator = 1,
        UnityNativeVR = 2,
        SteamVR = 3,
        OculusVR = 4,
    }

    public enum VRModuleActiveEnum
    {
        Uninitialized = -1,
        None = VRModuleSelectEnum.None,
        //Simulator = SelectVRModuleEnum.Simulator,
        UnityNativeVR = VRModuleSelectEnum.UnityNativeVR,
        SteamVR = VRModuleSelectEnum.SteamVR,
        OculusVR = VRModuleSelectEnum.OculusVR,
    }

    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public const uint MAX_DEVICE_COUNT = 16u;
        public const uint INVALID_DEVICE_INDEX = 4294967295u;
        public const uint HMD_DEVICE_INDEX = 0u;

        public static bool lockPhysicsUpdateRateToRenderFrequency
        {
            get
            {
                return Instance == null ? true : Instance.m_lockPhysicsUpdateRateToRenderFrequency;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_lockPhysicsUpdateRateToRenderFrequency = value;
                }
            }
        }

        public static VRModuleSelectEnum selectModule
        {
            get
            {
                return Instance == null ? VRModuleSelectEnum.Auto : Instance.m_selectModule;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_selectModule = value;
                }
            }
        }

        public static VRModuleActiveEnum activeModule
        {
            get
            {
                return Instance == null ? VRModuleActiveEnum.Uninitialized : Instance.m_activatedModule;
            }
        }

        public static IVRModuleDeviceState defaultDeviceState
        {
            get
            {
                return s_defaultState;
            }
        }

        public static bool IsValidDeviceIndex(uint deviceIndex)
        {
            return deviceIndex < MAX_DEVICE_COUNT;
        }

        public static bool HasInputFocus()
        {
            return Instance == null || Instance.m_activatedModuleBase == null ? true : Instance.m_activatedModuleBase.HasInputFocus();
        }

        public static bool IsDeviceConnected(string deviceSerialNumber)
        {
            return s_deviceSerialNumberTable.ContainsKey(deviceSerialNumber);
        }

        public static uint GetConnectedDeviceIndex(string deviceSerialNumber)
        {
            uint deviceIndex;
            if (s_deviceSerialNumberTable.TryGetValue(deviceSerialNumber, out deviceIndex)) { return deviceIndex; }
            return INVALID_DEVICE_INDEX;
        }

        public static bool TryGetConnectedDeviceIndex(string deviceSerialNumber, out uint deviceIndex)
        {
            return s_deviceSerialNumberTable.TryGetValue(deviceSerialNumber, out deviceIndex);
        }

        public static IVRModuleDeviceState GetCurrentDeviceState(uint deviceIndex)
        {
            return Instance == null || !IsValidDeviceIndex(deviceIndex) ? s_defaultState : Instance.m_currStates[deviceIndex];
        }

        public static IVRModuleDeviceState GetPreviousDeviceState(uint deviceIndex)
        {
            return Instance == null || !IsValidDeviceIndex(deviceIndex) ? s_defaultState : Instance.m_prevStates[deviceIndex];
        }

        public static uint GetLeftControllerDeviceIndex()
        {
            return Instance == null || Instance.m_activatedModuleBase == null ? INVALID_DEVICE_INDEX : Instance.m_activatedModuleBase.GetLeftControllerDeviceIndex();
        }

        public static uint GetRightControllerDeviceIndex()
        {
            return Instance == null || Instance.m_activatedModuleBase == null ? INVALID_DEVICE_INDEX : Instance.m_activatedModuleBase.GetRightControllerDeviceIndex();
        }

        public static VRModuleTrackingSpaceType trackingSpaceType
        {
            get
            {
                return Instance == null ? VRModuleTrackingSpaceType.RoomScale : Instance.m_trackingSpaceType;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_trackingSpaceType = value;

                    if (Instance.m_activatedModuleBase != null)
                    {
                        Instance.m_activatedModuleBase.UpdateTrackingSpaceType();
                    }
                }
            }
        }

        public static void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            if (Instance != null && Instance.m_activatedModuleBase != null && IsValidDeviceIndex(deviceIndex))
            {
                Instance.m_activatedModuleBase.TriggerViveControllerHaptic(deviceIndex, durationMicroSec);
            }
        }
    }
}