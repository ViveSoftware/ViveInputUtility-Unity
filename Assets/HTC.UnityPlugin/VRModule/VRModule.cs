//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;

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
        Simulator = 1,
        UnityNativeVR = 2,
        SteamVR = 3,
        OculusVR = 4,
        DayDream = 5,
        WaveVR = 6,
        UnityXR = 7,
    }

    public enum VRModuleActiveEnum
    {
        Uninitialized = -1,
        None = VRModuleSelectEnum.None,
        Simulator = VRModuleSelectEnum.Simulator,
        UnityNativeVR = VRModuleSelectEnum.UnityNativeVR,
        SteamVR = VRModuleSelectEnum.SteamVR,
        OculusVR = VRModuleSelectEnum.OculusVR,
        DayDream = VRModuleSelectEnum.DayDream,
        WaveVR = VRModuleSelectEnum.WaveVR,
        UnityXR = VRModuleSelectEnum.UnityXR,
    }

    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public const uint MAX_DEVICE_COUNT = 64u;
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
            if (!Active) { return false; }
            return deviceIndex < Instance.GetDeviceStateLength();
        }

        public static bool HasInputFocus()
        {
            return Instance == null || Instance.m_activatedModuleBase == null ? true : Instance.m_activatedModuleBase.HasInputFocus();
        }

        public static bool IsDeviceConnected(string deviceSerialNumber)
        {
            return (string.IsNullOrEmpty(deviceSerialNumber) || s_deviceSerialNumberTable == null) ? false : s_deviceSerialNumberTable.ContainsKey(deviceSerialNumber);
        }

        public static uint GetConnectedDeviceIndex(string deviceSerialNumber)
        {
            uint deviceIndex;
            if (string.IsNullOrEmpty(deviceSerialNumber) || s_deviceSerialNumberTable == null || !s_deviceSerialNumberTable.TryGetValue(deviceSerialNumber, out deviceIndex))
            {
                return INVALID_DEVICE_INDEX;
            }
            else
            {
                return deviceIndex;
            }
        }

        public static bool TryGetConnectedDeviceIndex(string deviceSerialNumber, out uint deviceIndex)
        {
            if (string.IsNullOrEmpty(deviceSerialNumber) || s_deviceSerialNumberTable == null)
            {
                deviceIndex = INVALID_DEVICE_INDEX;
                return false;
            }
            else
            {
                return s_deviceSerialNumberTable.TryGetValue(deviceSerialNumber, out deviceIndex);
            }
        }

        public static uint GetDeviceStateCount() { return Instance == null ? 0u : Instance.GetDeviceStateLength(); }

        public static IVRModuleDeviceState GetCurrentDeviceState(uint deviceIndex)
        {
            if (Instance == null || Instance.m_currStates == null || !IsValidDeviceIndex(deviceIndex)) { return s_defaultState; }
            return Instance.m_currStates[deviceIndex] ?? s_defaultState;
        }

        public static IVRModuleDeviceState GetPreviousDeviceState(uint deviceIndex)
        {
            if (Instance == null || Instance.m_currStates == null || !IsValidDeviceIndex(deviceIndex)) { return s_defaultState; }
            return Instance.m_prevStates[deviceIndex] ?? s_defaultState;
        }

        public static IVRModuleDeviceState GetDeviceState(uint deviceIndex, bool usePrevious = false)
        {
            return usePrevious ? GetPreviousDeviceState(deviceIndex) : GetCurrentDeviceState(deviceIndex);
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

        public static ISimulatorVRModule Simulator { get { return s_simulator; } }

        public static void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            if (Instance != null && Instance.m_activatedModuleBase != null && IsValidDeviceIndex(deviceIndex))
            {
                Instance.m_activatedModuleBase.TriggerViveControllerHaptic(deviceIndex, durationMicroSec);
            }
        }

        public static void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85f, float amplitude = 0.125f, float startSecondsFromNow = 0f)
        {
            if (Instance != null && Instance.m_activatedModuleBase != null && IsValidDeviceIndex(deviceIndex))
            {
                Instance.m_activatedModuleBase.TriggerHapticVibration(deviceIndex, durationSeconds, frequency, amplitude, startSecondsFromNow);
            }
        }
    }
}