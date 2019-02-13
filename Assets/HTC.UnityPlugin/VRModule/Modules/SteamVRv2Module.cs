//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Text;
using UnityEngine;
using Valve.VR;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
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
        public class ActionArray<T> where T : struct
        {
            private static readonly EnumUtils.EnumDisplayInfo s_enumInfo;
            private static readonly T[] s_enums;
            private static readonly ulong[] s_actionOrigins;
            public static readonly int Len;

            private string m_pathPrefix;
            private string m_dataType;
            private string[] m_aliases;
            private string[] m_paths;
            private ulong[] m_handles;

            private int m_iterator = -1;
            private int m_originIterator = -1;

            static ActionArray()
            {
                s_enumInfo = EnumUtils.GetDisplayInfo(typeof(T));
                Len = s_enumInfo.maxValue - s_enumInfo.minValue + 1;

                var ints = new int[Len];
                for (int i = 0; i < Len; ++i)
                {
                    ints[i] = s_enumInfo.minValue + i;
                }

                s_enums = ints as T[];

                s_actionOrigins = new ulong[OpenVR.k_unMaxActionOriginCount];
            }

            public ActionArray(string pathPrefix, string dataType)
            {
                m_pathPrefix = pathPrefix;
                m_dataType = dataType;

                m_aliases = new string[Len];
                m_paths = new string[Len];
                m_handles = new ulong[Len];

            }

            public string DataType { get { return m_dataType; } }
            public T Current { get { return s_enums[m_iterator]; } }
            public string CurrentAlias { get { return m_aliases[m_iterator]; } }
            public string CurrentPath { get { return m_paths[m_iterator]; } }
            public ulong CurrentHandle { get { return m_handles[m_iterator]; } }
            public void MoveNext() { ++m_iterator; }
            public bool IsCurrentValid() { return m_iterator >= 0 && m_iterator < Len; }
            public void Reset() { m_iterator = 0; }

            public ulong CurrentOrigin { get { return s_actionOrigins[m_originIterator]; } }
            public void MoveNextOrigin() { ++m_originIterator; }
            public bool IsCurrentOriginValid() { return m_originIterator >= 0 && m_originIterator < s_actionOrigins.Length && s_actionOrigins[m_originIterator] != OpenVR.k_ulInvalidInputValueHandle; }
            public void ResetOrigins(CVRInput vrInput)
            {
                if (CurrentHandle == OpenVR.k_ulInvalidActionHandle)
                {
                    m_originIterator = -1;
                    return;
                }

                m_originIterator = 0;
                var error = vrInput.GetActionOrigins(s_actionSetHandle, CurrentHandle, s_actionOrigins);
                if (error != EVRInputError.None)
                {
                    Debug.LogError("GetActionOrigins failed! action=" + CurrentPath + " error=" + error);
                }
            }

            public bool TryGetCurrentDigitalData(CVRInput vrInput, out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState, ref InputDigitalActionData_t data)
            {
                ulong originDevicePath;
                if (!TryGetCurrentOriginDataAndDeviceState(vrInput, out prevState, out currState, out originDevicePath)) { return false; }

                var error = vrInput.GetDigitalActionData(CurrentHandle, ref data, s_moduleInstance.m_digitalDataSize, originDevicePath);
                if (error != EVRInputError.None)
                {
                    Debug.LogError("GetDigitalActionData failed! action=" + CurrentPath + " error=" + error);
                    return false;
                }

                return true;
            }

            public bool TryGetCurrentAnalogData(CVRInput vrInput, out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState, ref InputAnalogActionData_t data)
            {
                ulong originDevicePath;
                if (!TryGetCurrentOriginDataAndDeviceState(vrInput, out prevState, out currState, out originDevicePath)) { return false; }

                var error = vrInput.GetAnalogActionData(CurrentHandle, ref data, s_moduleInstance.m_analogDataSize, originDevicePath);
                if (error != EVRInputError.None)
                {
                    Debug.LogError("GetAnalogActionData failed! action=" + CurrentPath + " error=" + error);
                    return false;
                }

                return true;
            }

            private bool TryGetCurrentOriginDataAndDeviceState(CVRInput vrInput, out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState, out ulong originDevicePath)
            {
                OriginData originData;
                EVRInputError error;
                if (!s_moduleInstance.TryGetDeviceIndexFromOrigin(vrInput, CurrentOrigin, out originData, out error))
                {
                    Debug.LogError("GetOriginTrackedDeviceInfo failed! error=" + error + " action=" + pressActions.CurrentPath);
                    prevState = null;
                    currState = null;
                    originDevicePath = 0ul;
                    return false;
                }

                originDevicePath = originData.devicePath;
                return s_moduleInstance.TryGetValidDeviceState(originData.deviceIndex, out prevState, out currState) && currState.isConnected;
            }

            public void Set(T e, string pathName, string alias)
            {
                var index = EqualityComparer<T>.Default.GetHashCode(e) - s_enumInfo.minValue;
                m_aliases[index] = alias;
                m_paths[index] = ACTION_SET_NAME + m_pathPrefix + pathName;
            }

            public void InitiateHandles(CVRInput vrInput)
            {
                for (int i = 0; i < Len; ++i)
                {
                    m_handles[i] = SafeGetActionHandle(vrInput, m_paths[i]);
                }
            }
        }

        public enum HapticStruct { Haptic }

        public const string ACTION_SET_NAME = "/actions/htc_viu";

        private static bool s_pathInitialized;
        private static bool s_actionInitialized;

        public static ActionArray<VRModuleRawButton> pressActions { get; private set; }
        public static ActionArray<VRModuleRawButton> touchActions { get; private set; }
        public static ActionArray<VRModuleRawAxis> v1Actions { get; private set; }
        public static ActionArray<VRModuleRawAxis> v2Actions { get; private set; }
        public static ActionArray<HapticStruct> vibrateActions { get; private set; }

        private static ulong[] s_devicePathHandles;
        private static ulong s_actionSetHandle;

        private uint m_digitalDataSize;
        private uint m_analogDataSize;
        private uint m_originInfoSize;
        private uint m_activeActionSetSize;

        private ETrackingUniverseOrigin m_prevTrackingSpace;
        private bool m_hasInputFocus = true;
        private TrackedDevicePose_t[] m_poses;
        private TrackedDevicePose_t[] m_gamePoses;
        private VRActiveActionSet_t[] m_activeActionSets;

        private struct OriginData
        {
            public ulong devicePath;
            public uint deviceIndex;
        }

        private Dictionary<ulong, OriginData> m_originDataCache;

        private static ETrackingUniverseOrigin trackingSpace
        {
            get
            {
                var compositor = OpenVR.Compositor;
                if (compositor == null) { return default(ETrackingUniverseOrigin); }

                return compositor.GetTrackingSpace();
            }
            set
            {
                var compositor = OpenVR.Compositor;
                if (compositor == null) { return; }

                compositor.SetTrackingSpace(value);
            }
        }

        private static bool inputFocus
        {
            get
            {
                var system = OpenVR.System;
                if (system == null) { return false; }
                return system.IsInputAvailable();
            }
        }

        public static void InitializePaths()
        {
            if (s_pathInitialized) { return; }
            s_pathInitialized = true;

            pressActions = new ActionArray<VRModuleRawButton>("/in/viu_press_", "boolean");
            pressActions.Set(VRModuleRawButton.System, "00", "Press00 (System)");
            pressActions.Set(VRModuleRawButton.ApplicationMenu, "01", "Press01 (ApplicationMenu)");
            pressActions.Set(VRModuleRawButton.Grip, "02", "Press02 (Grip)");
            pressActions.Set(VRModuleRawButton.DPadLeft, "03", "Press03 (DPadLeft)");
            pressActions.Set(VRModuleRawButton.DPadUp, "04", "Press04 (DPadUp)");
            pressActions.Set(VRModuleRawButton.DPadRight, "05", "Press05 (DPadRight)");
            pressActions.Set(VRModuleRawButton.DPadDown, "06", "Press06 (DPadDown)");
            pressActions.Set(VRModuleRawButton.A, "07", "Press07 (A)");
            pressActions.Set(VRModuleRawButton.ProximitySensor, "31", "Press31 (ProximitySensor)");
            pressActions.Set(VRModuleRawButton.Touchpad, "32", "Press32 (Touchpad)");
            pressActions.Set(VRModuleRawButton.Trigger, "33", "Press33 (Trigger)");
            pressActions.Set(VRModuleRawButton.CapSenseGrip, "34", "Press34 (CapSenseGrip)");

            touchActions = new ActionArray<VRModuleRawButton>("/in/viu_touch_", "boolean");
            touchActions.Set(VRModuleRawButton.System, "00", "Touch00 (System)");
            touchActions.Set(VRModuleRawButton.ApplicationMenu, "01", "Touch01 (ApplicationMenu)");
            touchActions.Set(VRModuleRawButton.Grip, "02", "Touch02 (Grip)");
            touchActions.Set(VRModuleRawButton.DPadLeft, "03", "Touch03 (DPadLeft)");
            touchActions.Set(VRModuleRawButton.DPadUp, "04", "Touch04 (DPadUp)");
            touchActions.Set(VRModuleRawButton.DPadRight, "05", "Touch05 (DPadRight)");
            touchActions.Set(VRModuleRawButton.DPadDown, "06", "Touch06 (DPadDown)");
            touchActions.Set(VRModuleRawButton.A, "07", "Touch07 (A)");
            touchActions.Set(VRModuleRawButton.ProximitySensor, "31", "Touch31 (ProximitySensor)");
            touchActions.Set(VRModuleRawButton.Touchpad, "32", "Touch32 (Touchpad)");
            touchActions.Set(VRModuleRawButton.Trigger, "33", "Touch33 (Trigger)");
            touchActions.Set(VRModuleRawButton.CapSenseGrip, "34", "Touch34 (CapSenseGrip)");

            v1Actions = new ActionArray<VRModuleRawAxis>("/in/viu_axis_", "vector1");
            v1Actions.Set(VRModuleRawAxis.Axis0X, "0x", "Axis0 X (TouchpadX)");
            v1Actions.Set(VRModuleRawAxis.Axis0Y, "0y", "Axis0 Y (TouchpadY)");
            v1Actions.Set(VRModuleRawAxis.Axis1X, "1x", "Axis1 X (Trigger)");
            v1Actions.Set(VRModuleRawAxis.Axis1Y, "1y", "Axis1 Y");
            v1Actions.Set(VRModuleRawAxis.Axis2X, "2x", "Axis2 X (CapSenseGrip)");
            v1Actions.Set(VRModuleRawAxis.Axis2Y, "2y", "Axis2 Y");
            v1Actions.Set(VRModuleRawAxis.Axis3X, "3x", "Axis3 X (IndexCurl)");
            v1Actions.Set(VRModuleRawAxis.Axis3Y, "3y", "Axis3 Y (MiddleCurl)");
            v1Actions.Set(VRModuleRawAxis.Axis4X, "4x", "Axis4 X (RingCurl)");
            v1Actions.Set(VRModuleRawAxis.Axis4Y, "4y", "Axis4 Y (PinkyCurl)");

            v2Actions = new ActionArray<VRModuleRawAxis>("/in/viu_axis_", "vector2");
            v2Actions.Set(VRModuleRawAxis.Axis0X, "0xy", "Axis0 X&Y (Touchpad)");
            v2Actions.Set(VRModuleRawAxis.Axis1X, "1xy", "Axis1 X&Y");
            v2Actions.Set(VRModuleRawAxis.Axis2X, "2xy", "Axis2 X&Y (Thumbstick)");
            v2Actions.Set(VRModuleRawAxis.Axis3X, "3xy", "Axis3 X&Y");
            v2Actions.Set(VRModuleRawAxis.Axis4X, "4xy", "Axis4 X&Y");

            vibrateActions = new ActionArray<HapticStruct>("/out/viu_vib_", "vibration");
            vibrateActions.Set(HapticStruct.Haptic, "01", "Vibration");
        }

        public static void InitializeHandles()
        {
            if (!Application.isPlaying || s_actionInitialized) { return; }
            s_actionInitialized = true;

            InitializePaths();

            SteamVR.Initialize();
#if VIU_STEAMVR_2_2_0_OR_NEWER
            SteamVR_ActionSet_Manager.UpdateActionStates();
#elif VIU_STEAMVR_2_1_0_OR_NEWER
            SteamVR_ActionSet_Manager.UpdateActionSetsState();
#else
            SteamVR_ActionSet.UpdateActionSetsState();
#endif

            var vrInput = OpenVR.Input;
            if (vrInput == null)
            {
                Debug.LogError("Fail loading OpenVR.Input");
                return;
            }

            pressActions.InitiateHandles(vrInput);
            touchActions.InitiateHandles(vrInput);
            v1Actions.InitiateHandles(vrInput);
            v2Actions.InitiateHandles(vrInput);
            vibrateActions.InitiateHandles(vrInput);

            s_actionSetHandle = SafeGetActionSetHandle(vrInput, ACTION_SET_NAME);
        }

        private static ulong SafeGetActionSetHandle(CVRInput vrInput, string path)
        {
            if (string.IsNullOrEmpty(path)) { return 0ul; }

            var handle = OpenVR.k_ulInvalidActionHandle;
            var error = vrInput.GetActionSetHandle(path, ref handle);
            if (error != EVRInputError.None)
            {
                Debug.LogError("Load " + path + " action failed! error=" + error);
                return OpenVR.k_ulInvalidActionHandle;
            }
            else
            {
                return handle;
            }
        }

        private static ulong SafeGetActionHandle(CVRInput vrInput, string path)
        {
            if (string.IsNullOrEmpty(path)) { return 0ul; }

            var handle = OpenVR.k_ulInvalidActionHandle;
            var error = vrInput.GetActionHandle(path, ref handle);
            if (error != EVRInputError.None)
            {
                Debug.LogError("Load " + path + " action failed! error=" + error);
                return OpenVR.k_ulInvalidActionHandle;
            }
            else
            {
                return handle;
            }
        }

        public static ulong GetInputSrouceHandleForDevice(uint deviceIndex)
        {
            if (s_devicePathHandles == null || deviceIndex >= s_devicePathHandles.Length)
            {
                return OpenVR.k_ulInvalidInputValueHandle;
            }
            else
            {
                return s_devicePathHandles[deviceIndex];
            }
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
            m_digitalDataSize = (uint)Marshal.SizeOf(new InputDigitalActionData_t());
            m_analogDataSize = (uint)Marshal.SizeOf(new InputAnalogActionData_t());
            m_originInfoSize = (uint)Marshal.SizeOf(new InputOriginInfo_t());
            m_activeActionSetSize = (uint)Marshal.SizeOf(new VRActiveActionSet_t());


            m_poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            m_gamePoses = new TrackedDevicePose_t[0];
            m_originDataCache = new Dictionary<ulong, OriginData>((int)OpenVR.k_unMaxActionOriginCount);

            InitializeHandles();

            m_activeActionSets = new VRActiveActionSet_t[1] { new VRActiveActionSet_t() { ulActionSet = s_actionSetHandle, } };

#if VIU_STEAMVR_2_2_0_OR_NEWER
            SteamVR_Input.onNonVisualActionsUpdated += UpdateDeviceInput;
            SteamVR_Input.onPosesUpdated += UpdateDevicePose;
#else
            SteamVR_Input.OnNonVisualActionsUpdated += UpdateDeviceInput;
            SteamVR_Input.OnPosesUpdated += UpdateDevicePose;
#endif

            s_devicePathHandles = new ulong[OpenVR.k_unMaxTrackedDeviceCount];
            EnsureDeviceStateLength(OpenVR.k_unMaxTrackedDeviceCount);

            // preserve previous tracking space
            m_prevTrackingSpace = trackingSpace;

            m_hasInputFocus = inputFocus;

            SteamVR_Events.InputFocus.AddListener(OnInputFocus);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).AddListener(OnTrackedDeviceRoleChanged);

            s_moduleInstance = this;
        }

        public override void OnDeactivated()
        {
            SteamVR_Events.InputFocus.RemoveListener(OnInputFocus);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).RemoveListener(OnTrackedDeviceRoleChanged);

#if VIU_STEAMVR_2_2_0_OR_NEWER
            SteamVR_Input.onNonVisualActionsUpdated -= UpdateDeviceInput;
            SteamVR_Input.onPosesUpdated -= UpdateDevicePose;
#else
            SteamVR_Input.OnNonVisualActionsUpdated -= UpdateDeviceInput;
            SteamVR_Input.OnPosesUpdated -= UpdateDevicePose;
#endif

            trackingSpace = m_prevTrackingSpace;

            s_moduleInstance = null;
        }

        private void UpdateDeviceInput()
        {
            EVRInputError error;
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            var vrInput = OpenVR.Input;
            if (vrInput == null)
            {
                for (uint i = 0, iMax = GetDeviceStateLength(); i < iMax; ++i)
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
                // FIXME: Should update by SteamVR_Input? SteamVR_Input.GetActionSetFromPath(ACTIONSET_PATH).ActivatePrimary();
                error = vrInput.UpdateActionState(m_activeActionSets, m_activeActionSetSize);
                if (error != EVRInputError.None)
                {
                    Debug.LogError("UpdateActionState failed! " + ACTION_SET_NAME + " error=" + error);
                }

                for (pressActions.Reset(); pressActions.IsCurrentValid(); pressActions.MoveNext())
                {
                    for (pressActions.ResetOrigins(vrInput); pressActions.IsCurrentOriginValid(); pressActions.MoveNextOrigin())
                    {
                        var data = default(InputDigitalActionData_t);
                        if (pressActions.TryGetCurrentDigitalData(vrInput, out prevState, out currState, ref data))
                        {
                            currState.SetButtonPress(pressActions.Current, data.bState);
                        }
                    }
                }

                for (touchActions.Reset(); touchActions.IsCurrentValid(); touchActions.MoveNext())
                {
                    for (touchActions.ResetOrigins(vrInput); touchActions.IsCurrentOriginValid(); touchActions.MoveNextOrigin())
                    {
                        var data = default(InputDigitalActionData_t);
                        if (touchActions.TryGetCurrentDigitalData(vrInput, out prevState, out currState, ref data))
                        {
                            currState.SetButtonTouch(touchActions.Current, data.bState);
                        }
                    }
                }

                for (v1Actions.Reset(); v1Actions.IsCurrentValid(); v1Actions.MoveNext())
                {
                    for (v1Actions.ResetOrigins(vrInput); v1Actions.IsCurrentOriginValid(); v1Actions.MoveNextOrigin())
                    {
                        var data = default(InputAnalogActionData_t);
                        if (v1Actions.TryGetCurrentAnalogData(vrInput, out prevState, out currState, ref data))
                        {
                            currState.SetAxisValue(v1Actions.Current, data.x);
                        }
                    }
                }

                for (v2Actions.Reset(); v2Actions.IsCurrentValid(); v2Actions.MoveNext())
                {
                    for (v2Actions.ResetOrigins(vrInput); v2Actions.IsCurrentOriginValid(); v2Actions.MoveNextOrigin())
                    {
                        var data = default(InputAnalogActionData_t);
                        if (v2Actions.TryGetCurrentAnalogData(vrInput, out prevState, out currState, ref data))
                        {
                            currState.SetAxisValue(v2Actions.Current, data.x);
                            currState.SetAxisValue(v2Actions.Current + 1, data.y);
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
            var vrCompositor = OpenVR.Compositor;
            if (vrSystem == null || vrCompositor == null)
            {
                for (uint i = 0, imax = GetDeviceStateLength(); i < imax; ++i)
                {
                    if (TryGetValidDeviceState(i, out prevState, out currState) && currState.isConnected)
                    {
                        currState.Reset();
                    }
                }

                return;
            }

            vrCompositor.GetLastPoses(m_poses, m_gamePoses);

            for (uint i = 0u, imax = (uint)m_poses.Length; i < imax; ++i)
            {
                if (!m_poses[i].bDeviceIsConnected)
                {
                    if (TryGetValidDeviceState(i, out prevState, out currState) && prevState.isConnected)
                    {
                        s_devicePathHandles[i] = OpenVR.k_ulInvalidInputValueHandle;
                        currState.Reset();
                    }
                }
                else
                {
                    EnsureValidDeviceState(i, out prevState, out currState);

                    if (!prevState.isConnected)
                    {
                        currState.isConnected = true;
                        currState.deviceClass = (VRModuleDeviceClass)vrSystem.GetTrackedDeviceClass(i);
                        currState.serialNumber = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_SerialNumber_String);
                        currState.modelNumber = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                        currState.renderModelName = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_RenderModelName_String);

                        SetupKnownDeviceModel(currState);

                        m_originDataCache.Clear();
                    }

                    // update device status
                    currState.isPoseValid = m_poses[i].bPoseIsValid;
                    currState.isOutOfRange = m_poses[i].eTrackingResult == ETrackingResult.Running_OutOfRange || m_poses[i].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                    currState.isCalibrating = m_poses[i].eTrackingResult == ETrackingResult.Calibrating_InProgress || m_poses[i].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                    currState.isUninitialized = m_poses[i].eTrackingResult == ETrackingResult.Uninitialized;
                    currState.velocity = new Vector3(m_poses[i].vVelocity.v0, m_poses[i].vVelocity.v1, -m_poses[i].vVelocity.v2);
                    currState.angularVelocity = new Vector3(-m_poses[i].vAngularVelocity.v0, -m_poses[i].vAngularVelocity.v1, m_poses[i].vAngularVelocity.v2);

                    var rigidTransform = new SteamVR_Utils.RigidTransform(m_poses[i].mDeviceToAbsoluteTracking);
                    currState.position = rigidTransform.pos;
                    currState.rotation = rigidTransform.rot;
                }
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
        }

        public override void Update()
        {
            if (SteamVR.active)
            {
                SteamVR_Settings.instance.lockPhysicsUpdateRateToRenderFrequency = VRModule.lockPhysicsUpdateRateToRenderFrequency;
            }
        }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.RoomScale:
                    trackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;
                    break;
                case VRModuleTrackingSpaceType.Stationary:
                    trackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
                    break;
            }
        }

        private bool TryGetDeviceIndexFromOrigin(CVRInput vrInput, ulong origin, out OriginData originData, out EVRInputError error)
        {
            if (!m_originDataCache.TryGetValue(origin, out originData))
            {
                var originInfo = default(InputOriginInfo_t);
                error = vrInput.GetOriginTrackedDeviceInfo(origin, ref originInfo, m_originInfoSize);
                if (error != EVRInputError.None)
                {
                    originData = new OriginData()
                    {
                        devicePath = OpenVR.k_ulInvalidInputValueHandle,
                        deviceIndex = OpenVR.k_unTrackedDeviceIndexInvalid,
                    };
                    return false;
                }
                else
                {
                    originData = new OriginData()
                    {
                        devicePath = originInfo.devicePath,
                        deviceIndex = originInfo.trackedDeviceIndex,
                    };

                    s_devicePathHandles[originInfo.trackedDeviceIndex] = originInfo.devicePath;
                    //Debug.Log("Set device path " + originInfo.trackedDeviceIndex + " to " + originInfo.devicePath);
                    m_originDataCache.Add(origin, originData);
                    return true;
                }
            }
            else
            {
                error = EVRInputError.None;
                return true;
            }
        }

        private void OnInputFocus(bool value)
        {
            m_hasInputFocus = value;
            InvokeInputFocusEvent(value);
        }

        public override bool HasInputFocus() { return m_hasInputFocus; }

        private void OnTrackedDeviceRoleChanged(VREvent_t arg)
        {
            InvokeControllerRoleChangedEvent();
            m_originDataCache.Clear();
        }

        public override uint GetLeftControllerDeviceIndex()
        {
            var system = OpenVR.System;
            return system == null ? INVALID_DEVICE_INDEX : system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        }

        public override uint GetRightControllerDeviceIndex()
        {
            var system = OpenVR.System;
            return system == null ? INVALID_DEVICE_INDEX : system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
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
            TriggerHapticVibration(deviceIndex, 0.01f, 85f, Mathf.InverseLerp(0, 4000, durationMicroSec), 0f);
        }

        public override void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85f, float amplitude = 0.125f, float startSecondsFromNow = 0f)
        {
            var handle = GetInputSrouceHandleForDevice(deviceIndex);
            if (handle == OpenVR.k_ulInvalidDriverHandle) { return; }

            var vrInput = OpenVR.Input;
            if (vrInput != null)
            {
                vibrateActions.Reset();

                var error = vrInput.TriggerHapticVibrationAction(vibrateActions.CurrentHandle, startSecondsFromNow, durationSeconds, frequency, amplitude, handle);
                if (error != EVRInputError.None)
                {
                    Debug.LogError("TriggerViveControllerHaptic failed! error=" + error);
                }
            }
        }
#endif
    }
}
