//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Text;
using UnityEngine;
using Valve.VR;
using System;
using System.Runtime.InteropServices;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#elif UNITY_5_4_OR_NEWER
using XRSettings = UnityEngine.VR.VRSettings;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class SteamVRModule : VRModule.ModuleBase
    {
#if VIU_STEAMVR_2_0_0_OR_NEWER
        public const int INPUT_SOURCE_COUNT = 11;
        public const string ACTIONSET_PATH = "/actions/htc_viu";

        private static bool s_pathInitialized;
        private static bool s_actionInitialized;
        private static int s_buttonActionLength;
        private static int s_axisActionLength;
        private static EnumUtils.EnumDisplayInfo s_buttonInfo;
        private static EnumUtils.EnumDisplayInfo s_axisInfo;
        private static string[] s_pressActionPaths;
        private static string[] s_touchActionPaths;
        private static string[] s_axisActionPaths;
        private static ulong[] s_pressActions;
        private static ulong[] s_touchActions;
        private static ulong[] s_axisActions;
        private static string s_poseActionPath;
        private static ulong s_poseAction;
        private static string s_hapticActionPath;
        private static ulong s_hapticsAction;
        private static string[] s_inputSourcePaths;
        private static ulong[] s_inputSources;

        private static uint s_digitalDataSize;
        private static uint s_analogDataSize;
        private static uint s_poseDataSize;
        private static uint s_originInfoSize;

        private ETrackingUniverseOrigin m_trackingSpace;
        private bool m_hasInputFocus = true;
        private ulong[] m_activeOrigins;
        private uint[] m_activeOriginIndices;

        public static void InitializePaths()
        {
            if (s_pathInitialized) { return; }
            s_pathInitialized = true;

            s_buttonInfo = EnumUtils.GetDisplayInfo(typeof(VRModuleRawButton));
            s_buttonActionLength = s_buttonInfo.maxValue - s_buttonInfo.minValue + 1;
            s_pressActionPaths = new string[s_buttonActionLength];
            s_touchActionPaths = new string[s_buttonActionLength];
            for (int i = 0, v = s_buttonInfo.minValue, vmax = s_buttonInfo.maxValue; v <= vmax; ++i, ++v)
            {
                if (!Enum.IsDefined(typeof(VRModuleRawButton), v)) { continue; }
                var buttonName = Enum.GetName(typeof(VRModuleRawButton), v).ToLower();
                s_pressActionPaths[i] = ACTIONSET_PATH + "/in/viu_press_" + buttonName;
                s_touchActionPaths[i] = ACTIONSET_PATH + "/in/viu_touch_" + buttonName;
            }

            s_axisInfo = EnumUtils.GetDisplayInfo(typeof(VRModuleRawAxis));
            s_axisActionLength = s_axisInfo.maxValue - s_axisInfo.minValue + 1;
            s_axisActionPaths = new string[s_axisActionLength];
            for (int i = 0, v = s_axisInfo.minValue, vmax = s_axisInfo.maxValue; v <= vmax; ++i, ++v)
            {
                if (!Enum.IsDefined(typeof(VRModuleRawAxis), v)) { continue; }
                var axisName = Enum.GetName(typeof(VRModuleRawAxis), v).ToLower();
                s_axisActionPaths[i] = ACTIONSET_PATH + "/in/viu_axis_" + axisName;
            }

            s_poseActionPath = ACTIONSET_PATH + "/in/viu_pose";

            s_hapticActionPath = ACTIONSET_PATH + "/out/viu_haptic";

            s_inputSourcePaths = new string[INPUT_SOURCE_COUNT]
            {
                "/user/head",
                "/user/hand/left",
                "/user/hand/right",
                "/user/foot/left",
                "/user/foot/right",
                "/user/shoulder/left",
                "/user/shoulder/right",
                "/user/waist",
                "/user/chest",
                "/user/camera",
                "/user/keyboard",
            };
        }

        public static void InitializeHandles()
        {
            if (!Application.isPlaying || s_actionInitialized) { return; }
            s_actionInitialized = true;

            InitializePaths();

            SteamVR.Initialize();
            SteamVR_Input.PreInitialize();
            SteamVR_Input.Initialize();

            var vrInput = OpenVR.Input;
            if (vrInput == null)
            {
                Debug.LogError("Fail loading OpenVR.Input");
                return;
            }

            s_pressActions = new ulong[s_buttonActionLength];
            s_touchActions = new ulong[s_buttonActionLength];
            for (int i = 0, imax = s_buttonActionLength; i < imax; ++i)
            {
                s_pressActions[i] = SafeGetActionHandle(vrInput, s_pressActionPaths[i]);
                s_touchActions[i] = SafeGetActionHandle(vrInput, s_touchActionPaths[i]);
            }

            s_axisActions = new ulong[s_axisActionLength];
            for (int i = 0, imax = s_axisActionLength; i < imax; ++i)
            {
                s_axisActions[i] = SafeGetActionHandle(vrInput, s_axisActionPaths[i]);
            }

            s_poseAction = SafeGetActionHandle(vrInput, s_poseActionPath);
            s_hapticsAction = SafeGetActionHandle(vrInput, s_hapticActionPath);

            s_inputSources = new ulong[INPUT_SOURCE_COUNT];
            for (int i = 0; i < INPUT_SOURCE_COUNT; ++i)
            {
                s_inputSources[i] = SafeGetInputSource(vrInput, s_inputSourcePaths[i]);
            }
        }

        private static ulong SafeGetActionHandle(CVRInput vrInput, string path)
        {
            if (string.IsNullOrEmpty(path)) { return 0ul; }

            var handle = 0ul;
            var error = vrInput.GetActionHandle(s_poseActionPath, ref handle);
            if (error != EVRInputError.None)
            {
                Debug.LogError("Load " + s_poseActionPath + " action failed! error=" + error);
            }

            return handle;
        }

        private static ulong SafeGetInputSource(CVRInput vrInput, string path)
        {
            if (string.IsNullOrEmpty(path)) { return 0ul; }

            var handle = 0ul;
            var error = vrInput.GetInputSourceHandle(s_poseActionPath, ref handle);
            if (error != EVRInputError.None)
            {
                Debug.LogError("Load " + s_poseActionPath + " action failed! error=" + error);
            }

            return handle;
        }

        public override bool ShouldActiveModule()
        {
#if UNITY_5_4_OR_NEWER
            return VIUSettings.activateSteamVRModule && XRSettings.enabled && XRSettings.loadedDeviceName == "OpenVR";
#else
            return VIUSettings.activateSteamVRModule && SteamVR.enabled;
#endif
        }

        public override void OnActivated()
        {
            s_digitalDataSize = (uint)Marshal.SizeOf(new InputDigitalActionData_t());
            s_analogDataSize = (uint)Marshal.SizeOf(new InputAnalogActionData_t());
            s_poseDataSize = (uint)Marshal.SizeOf(new InputPoseActionData_t());
            s_originInfoSize = (uint)Marshal.SizeOf(new InputOriginInfo_t());

            m_activeOrigins = new ulong[INPUT_SOURCE_COUNT];
            m_activeOriginIndices = new uint[INPUT_SOURCE_COUNT];

            InitializeHandles();

            SteamVR_Input.OnNonVisualActionsUpdated += UpdateDeviceInput;
            SteamVR_Input.OnPosesUpdated += UpdateDevicePose;

            var actionSet = SteamVR_Input.GetActionSetFromPath(ACTIONSET_PATH);
            if (actionSet == null)
            {
                Debug.LogError("Unable to activate ActionSet " + ACTIONSET_PATH);
            }
            else
            {
                actionSet.ActivatePrimary();
            }

            EnsureDeviceStateLength(INPUT_SOURCE_COUNT);

            // setup tracking space
            UpdateTrackingSpaceType();
        }

        public override void OnDeactivated()
        {
            SteamVR_Input.OnNonVisualActionsUpdated -= UpdateDeviceInput;
            SteamVR_Input.OnPosesUpdated -= UpdateDevicePose;
        }

        private void UpdateDeviceInput()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            var vrInput = OpenVR.Input;
            if (vrInput == null)
            {
                for (uint i = 0; i < INPUT_SOURCE_COUNT; ++i)
                {
                    if (TryGetValidDeviceState(i, out prevState, out currState) && currState.isConnected)
                    {
                        currState.buttonPressed = 0ul;
                        currState.buttonTouched = 0ul;
                        currState.ResetAxisValues();
                    }
                }
            }
            else
            {
                for (uint i = 0; i < INPUT_SOURCE_COUNT; ++i)
                {
                    if (TryGetValidDeviceState(i, out prevState, out currState) && currState.isConnected)
                    {
                        for (int index = 0, value = s_buttonInfo.minValue, vmax = s_buttonInfo.maxValue; value <= vmax; ++index, ++value)
                        {
                            if (s_pressActions[index] != 0ul)
                            {
                                var digitalData = default(InputDigitalActionData_t);
                                var error = vrInput.GetDigitalActionData(s_pressActions[index], ref digitalData, s_digitalDataSize, s_inputSources[i]);
                                if (error != EVRInputError.None)
                                {
                                    Debug.LogError("GetDigitalActionData failed! action=" + s_pressActionPaths[index] + " source=" + s_inputSourcePaths[index] + " error=" + error);
                                }
                                currState.SetButtonPress((VRModuleRawButton)value, digitalData.bActive && digitalData.bState);
                            }
                            if (s_touchActions[index] != 0ul)
                            {
                                var digitalData = default(InputDigitalActionData_t);
                                var error = vrInput.GetDigitalActionData(s_touchActions[index], ref digitalData, s_digitalDataSize, s_inputSources[i]);
                                if (error != EVRInputError.None)
                                {
                                    Debug.LogError("GetDigitalActionData failed! action=" + s_touchActionPaths[index] + " source=" + s_inputSourcePaths[index] + " error=" + error);
                                }
                                currState.SetButtonTouch((VRModuleRawButton)value, digitalData.bActive && digitalData.bState);
                            }
                        }

                        for (int index = 0, value = s_axisInfo.minValue, vmax = s_axisInfo.maxValue; value <= vmax; ++index, ++value)
                        {
                            if (s_axisActions[index] != 0ul)
                            {
                                var analogData = default(InputAnalogActionData_t);
                                var error = vrInput.GetAnalogActionData(s_axisActions[index], ref analogData, s_analogDataSize, s_inputSources[i]);
                                if (error != EVRInputError.None)
                                {
                                    Debug.LogError("GetAnalogActionData failed! action=" + s_axisActionPaths[index] + " source=" + s_inputSourcePaths[index] + " error=" + error);
                                }
                                currState.SetAxisValue((VRModuleRawAxis)value, analogData.x);
                            }
                        }
                    }
                }
            }

            ProcessDeviceInputChanged();
        }

        private void UpdateDevicePose(bool obj)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            FlushDeviceState();

            var vrSystem = OpenVR.System;
            var vrInput = OpenVR.Input;
            if (vrSystem == null || vrInput == null)
            {
                for (uint i = 0; i < INPUT_SOURCE_COUNT; ++i)
                {
                    if (TryGetValidDeviceState(i, out prevState, out currState) && currState.isConnected)
                    {
                        currState.Reset();
                    }
                }
            }
            else
            {
                // update connected device
                for (uint i = 0; i < INPUT_SOURCE_COUNT; ++i)
                {
                    var poseData = default(InputPoseActionData_t);
                    vrInput.GetPoseActionData(s_poseAction, m_trackingSpace, 0f, ref poseData, s_poseDataSize, s_inputSources[i]);

                    if (!poseData.pose.bDeviceIsConnected && poseData.bActive && poseData.activeOrigin != 0ul)
                    {
                        if (TryGetValidDeviceState(i, out prevState, out currState) && prevState.isConnected)
                        {
                            Debug.Log("device " + s_inputSourcePaths[i] + " disconnected, bDeviceIsConnected:" + poseData.pose.bDeviceIsConnected + " bActive:" + poseData.bActive + " activeOrigin:" + poseData.activeOrigin);
                            currState.Reset();
                        }
                    }
                    else
                    {
                        EnsureValidDeviceState(i, out prevState, out currState);

                        if (!prevState.isConnected || m_activeOrigins[i] != poseData.activeOrigin)
                        {
                            // FIXME: what will happen when changing rendermodel?
                            var originInfo = default(InputOriginInfo_t);
                            var sb = new StringBuilder(64);
                            EVRInputError error;

                            error = vrInput.GetOriginLocalizedName(poseData.activeOrigin, sb, 64);
                            if (error != EVRInputError.None)
                            {
                                Debug.LogError("GetOriginLocalizedName failed! error=" + error);
                            }

                            error = vrInput.GetOriginTrackedDeviceInfo(poseData.activeOrigin, ref originInfo, s_originInfoSize);
                            if (error != EVRInputError.None)
                            {
                                Debug.LogError("GetOriginTrackedDeviceInfo failed! error=" + error);
                            }

                            Debug.Log("Origin: " + sb.ToString() + " index=" + originInfo.trackedDeviceIndex + " deviceIsSourceInput?" + (originInfo.devicePath == s_inputSources[i]));
                            m_activeOrigins[i] = poseData.activeOrigin;
                            m_activeOriginIndices[i] = originInfo.trackedDeviceIndex;

                            currState.isConnected = true;
                            currState.deviceClass = (VRModuleDeviceClass)vrSystem.GetTrackedDeviceClass(i);
                            currState.serialNumber = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_SerialNumber_String);
                            currState.modelNumber = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                            currState.renderModelName = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_RenderModelName_String);

                            SetupKnownDeviceModel(currState);
                        }

                        // update poses
                        currState.isPoseValid = poseData.pose.bPoseIsValid;
                        currState.isOutOfRange = poseData.pose.eTrackingResult == ETrackingResult.Running_OutOfRange || poseData.pose.eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                        currState.isCalibrating = poseData.pose.eTrackingResult == ETrackingResult.Calibrating_InProgress || poseData.pose.eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                        currState.isUninitialized = poseData.pose.eTrackingResult == ETrackingResult.Uninitialized;
                        currState.velocity = new Vector3(poseData.pose.vVelocity.v0, poseData.pose.vVelocity.v1, -poseData.pose.vVelocity.v2);
                        currState.angularVelocity = new Vector3(-poseData.pose.vAngularVelocity.v0, -poseData.pose.vAngularVelocity.v1, poseData.pose.vAngularVelocity.v2);

                        var rigidTransform = new SteamVR_Utils.RigidTransform(poseData.pose.mDeviceToAbsoluteTracking);
                        currState.position = rigidTransform.pos;
                        currState.rotation = rigidTransform.rot;
                    }
                }
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
        }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.RoomScale:
                    m_trackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;
                    break;
                case VRModuleTrackingSpaceType.Stationary:
                    m_trackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
                    break;
            }
        }

        private StringBuilder m_sb;
        private string QueryDeviceStringProperty(CVRSystem system, uint deviceIndex, ETrackedDeviceProperty prop)
        {
            var error = default(ETrackedPropertyError);
            var capacity = (int)system.GetStringTrackedDeviceProperty(deviceIndex, prop, null, 0, ref error);
            if (capacity <= 1 || capacity > 128) { return string.Empty; }

            if (m_sb == null) { m_sb = new StringBuilder(capacity); }
            else { m_sb.EnsureCapacity(capacity); }

            system.GetStringTrackedDeviceProperty(deviceIndex, prop, m_sb, (uint)m_sb.Capacity, ref error);
            if (error != ETrackedPropertyError.TrackedProp_Success) { return string.Empty; }

            var result = m_sb.ToString();
            m_sb.Length = 0;

            return result;
        }

        public override void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            var vrInput = OpenVR.Input;
            if (vrInput == null) { return; }

            var err = vrInput.TriggerHapticVibrationAction(s_hapticsAction, 0f, 0.000001f * durationMicroSec, 200, 1f, s_inputSources[deviceIndex]);
        }
#endif
    }
}
