//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

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

        public static IVRModuleDeviceState GetDeviceState(uint deviceIndex, bool usePrevious = false)
        {
            return Instance == null || !IsValidDeviceIndex(deviceIndex) ? s_defaultState : (usePrevious ? Instance.m_prevStates[deviceIndex] : Instance.m_currStates[deviceIndex]);
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

        public static readonly bool isSteamVRPluginDetected =
#if VIU_STEAMVR
            true;
#else
            false;
#endif

        public static readonly bool isOculusVRPluginDetected =
#if VIU_OCULUSVR
            true;
#else
            false;
#endif

        public static readonly bool isGoogleVRPluginDetected =
#if VIU_GOOGLEVR
            true;
#else
            false;
#endif

        public static readonly bool isWaveVRPluginDetected =
#if VIU_WAVEVR
            true;
#else
            false;
#endif
    }
}