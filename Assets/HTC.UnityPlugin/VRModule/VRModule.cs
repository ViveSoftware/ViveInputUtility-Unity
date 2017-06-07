//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public enum VRModuleTrackingSpaceType
    {
        Stationary,
        RoomScale,
    }

    public enum SelectVRModuleEnum
    {
        Auto = -1,
        None = 0,
        //Simulator = 1,
        UnityNativeVR = 2,
        SteamVR = 3,
        OculusVR = 4,
    }

    public enum SupportedVRModule
    {
        Uninitialized = -1,
        None = SelectVRModuleEnum.None,
        //Simulator = SelectVRModuleEnum.Simulator,
        UnityNativeVR = SelectVRModuleEnum.UnityNativeVR,
        SteamVR = SelectVRModuleEnum.SteamVR,
        OculusVR = SelectVRModuleEnum.OculusVR,
    }

    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public const uint MAX_DEVICE_COUNT = 16;
        public const uint INVALID_DEVICE_INDEX = 4294967295;

        public static SelectVRModuleEnum selectModule
        {
            get
            {
                return Instance == null ? SelectVRModuleEnum.Auto : Instance.m_selectModule;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_selectModule = value;
                }
            }
        }

        public static SupportedVRModule activeModule
        {
            get
            {
                return Instance == null ? SupportedVRModule.Uninitialized : Instance.m_activatedModule;
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