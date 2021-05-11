//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR && UNITY_STANDALONE
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Text;
using UnityEngine;
using Valve.VR;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#elif UNITY_5_4_OR_NEWER
using XRSettings = UnityEngine.VR.VRSettings;
#endif
#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class SteamVRModule : VRModule.ModuleBase
    {
        public const string OPENVR_XR_LOADER_NAME = "Open VR Loader";
        public const string OPENVR_XR_LOADER_CLASS_NAME = "OpenVRLoader";
        public const string ACTION_SET_NAME = "htc_viu";
        public const string ACTION_SET_PATH = "/actions/" + ACTION_SET_NAME;

#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE

        public enum HapticStruct { Haptic }
        private class HapticStructReslver : EnumToIntResolver<HapticStruct> { public override int Resolve(HapticStruct e) { return (int)e; } }

        private static readonly Quaternion s_leftSkeletonWristFixRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        private static readonly Quaternion s_rightSkeletonWristFixRotation = Quaternion.Euler(0.0f, 0.0f, -90.0f);
        private static readonly Quaternion s_leftSkeletonFixRotation = Quaternion.Euler(180.0f, 90.0f, 0.0f);
        private static readonly Quaternion s_rightSkeletonFixRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

        private static bool s_pathInitialized;
        private static bool s_actionInitialized;

        public static ActionCollection<VRModuleRawButton> pressActions { get; private set; }
        public static ActionCollection<VRModuleRawButton> touchActions { get; private set; }
        public static ActionCollection<VRModuleRawAxis> v1Actions { get; private set; }
        public static ActionCollection<VRModuleRawAxis> v2Actions { get; private set; }
        public static ActionCollection<HapticStruct> vibrateActions { get; private set; }
        public static ulong skeletonActionHandleLeft { get; private set; }
        public static ulong skeletonActionHandleRight { get; private set; }

        private static ulong s_actionSetHandle;
        private static VRBoneTransform_t[] s_tempBoneTransforms = new VRBoneTransform_t[SteamVR_Action_Skeleton.numBones];

        private uint m_digitalDataSize;
        private uint m_analogDataSize;

        private ETrackingUniverseOrigin m_prevTrackingSpace;
        private bool m_hasInputFocus = true;
        private TrackedDevicePose_t[] m_poses;
        private TrackedDevicePose_t[] m_gamePoses;

        private OriginDataCache m_originDataCache = new OriginDataCache();
        private IndexMap m_indexMap = new IndexMap();
        private VRModule.SubmoduleBase.Collection m_submodules = new VRModule.SubmoduleBase.Collection(new ViveHandTrackingSubmodule());
        private uint m_openvrRightIndex;
        private uint m_openvrLeftIndex;
        private uint m_moduleRightIndex;
        private uint m_moduleLeftIndex;

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

            pressActions = new ActionCollection<VRModuleRawButton>("/in/viu_press_", "boolean");
            pressActions.Set(VRModuleRawButton.System, "00", "Press00 (System)");
            pressActions.Set(VRModuleRawButton.ApplicationMenu, "01", "Press01 (ApplicationMenu)");
            pressActions.Set(VRModuleRawButton.Grip, "02", "Press02 (Grip)");
            pressActions.Set(VRModuleRawButton.DPadLeft, "03", "Press03 (DPadLeft)");
            pressActions.Set(VRModuleRawButton.DPadUp, "04", "Press04 (DPadUp)");
            pressActions.Set(VRModuleRawButton.DPadRight, "05", "Press05 (DPadRight)");
            pressActions.Set(VRModuleRawButton.DPadDown, "06", "Press06 (DPadDown)");
            pressActions.Set(VRModuleRawButton.A, "07", "Press07 (A)");
            pressActions.Set(VRModuleRawButton.Joystick, "08", "Press08 (Thumbstick)");
            pressActions.Set(VRModuleRawButton.ProximitySensor, "31", "Press31 (ProximitySensor)");
            pressActions.Set(VRModuleRawButton.Touchpad, "32", "Press32 (Touchpad)");
            pressActions.Set(VRModuleRawButton.Trigger, "33", "Press33 (Trigger)");
            pressActions.Set(VRModuleRawButton.CapSenseGrip, "34", "Press34 (CapSenseGrip)");
            pressActions.Set(VRModuleRawButton.Bumper, "35", "Press35 (Bumper)");

            touchActions = new ActionCollection<VRModuleRawButton>("/in/viu_touch_", "boolean");
            touchActions.Set(VRModuleRawButton.System, "00", "Touch00 (System)");
            touchActions.Set(VRModuleRawButton.ApplicationMenu, "01", "Touch01 (ApplicationMenu)");
            touchActions.Set(VRModuleRawButton.Grip, "02", "Touch02 (Grip)");
            touchActions.Set(VRModuleRawButton.DPadLeft, "03", "Touch03 (DPadLeft)");
            touchActions.Set(VRModuleRawButton.DPadUp, "04", "Touch04 (DPadUp)");
            touchActions.Set(VRModuleRawButton.DPadRight, "05", "Touch05 (DPadRight)");
            touchActions.Set(VRModuleRawButton.DPadDown, "06", "Touch06 (DPadDown)");
            touchActions.Set(VRModuleRawButton.A, "07", "Touch07 (A)");
            touchActions.Set(VRModuleRawButton.Joystick, "08", "Touch08 (Thumbstick)");
            touchActions.Set(VRModuleRawButton.ProximitySensor, "31", "Touch31 (ProximitySensor)");
            touchActions.Set(VRModuleRawButton.Touchpad, "32", "Touch32 (Touchpad)");
            touchActions.Set(VRModuleRawButton.Trigger, "33", "Touch33 (Trigger)");
            touchActions.Set(VRModuleRawButton.CapSenseGrip, "34", "Touch34 (CapSenseGrip)");
            touchActions.Set(VRModuleRawButton.Bumper, "35", "Touch35 (Bumper)");

            v1Actions = new ActionCollection<VRModuleRawAxis>("/in/viu_axis_", "vector1");
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

            v2Actions = new ActionCollection<VRModuleRawAxis>("/in/viu_axis_", "vector2");
            v2Actions.Set(VRModuleRawAxis.Axis0X, "0xy", "Axis0 X&Y (Touchpad)");
            v2Actions.Set(VRModuleRawAxis.Axis1X, "1xy", "Axis1 X&Y");
            v2Actions.Set(VRModuleRawAxis.Axis2X, "2xy", "Axis2 X&Y (Thumbstick)");
            v2Actions.Set(VRModuleRawAxis.Axis3X, "3xy", "Axis3 X&Y");
            v2Actions.Set(VRModuleRawAxis.Axis4X, "4xy", "Axis4 X&Y");

            vibrateActions = new ActionCollection<HapticStruct>("/out/viu_vib_", "vibration");
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

            pressActions.ResolveHandles(vrInput);
            touchActions.ResolveHandles(vrInput);
            v1Actions.ResolveHandles(vrInput);
            v2Actions.ResolveHandles(vrInput);
            vibrateActions.ResolveHandles(vrInput);

            s_actionSetHandle = SafeGetActionSetHandle(vrInput, ACTION_SET_PATH);

            skeletonActionHandleLeft = SafeGetActionHandle(vrInput, ACTION_SET_PATH + "/in/viu_skeleton_left");
            skeletonActionHandleRight = SafeGetActionHandle(vrInput, ACTION_SET_PATH + "/in/viu_skeleton_right");

            if (skeletonActionHandleLeft == OpenVR.k_ulInvalidActionHandle)
            {
                Debug.LogWarning("Skeleton action for left hand is not found: " + ACTION_SET_PATH + "/in/viu_skeleton_left");
            }

            if (skeletonActionHandleRight == OpenVR.k_ulInvalidActionHandle)
            {
                Debug.LogWarning("Skeleton action for right hand is not found: " + ACTION_SET_PATH + "/in/viu_skeleton_right");
            }
        }

        public static ulong GetInputSourceHandleForDevice(uint deviceIndex)
        {
            return s_moduleInstance.m_originDataCache.GetPathHandleByTrackedIndex(s_moduleInstance.m_indexMap.Module2TrackedIndex(deviceIndex));
        }

        public override bool ShouldActiveModule()
        {
#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
            return VIUSettings.activateSteamVRModule && (UnityXRModule.HasActiveLoader(OPENVR_XR_LOADER_NAME) ||
                (XRSettings.enabled && XRSettings.loadedDeviceName == "OpenVR"));
#elif UNITY_5_4_OR_NEWER
            return VIUSettings.activateSteamVRModule && XRSettings.enabled && XRSettings.loadedDeviceName == "OpenVR";
#else
            return VIUSettings.activateSteamVRModule && SteamVR.enabled;
#endif
        }

        public override void OnActivated()
        {
            m_digitalDataSize = (uint)Marshal.SizeOf(new InputDigitalActionData_t());
            m_analogDataSize = (uint)Marshal.SizeOf(new InputAnalogActionData_t());

            m_poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            m_gamePoses = new TrackedDevicePose_t[0];

            InitializeHandles();

#if VIU_STEAMVR_2_1_0_OR_NEWER
            SteamVR_Input.GetActionSet(ACTION_SET_NAME).Activate(SteamVR_Input_Sources.Any, 0, false);
#else
            var actionSet = SteamVR_Input.GetActionSetFromPath(ACTION_SET_PATH);
            if (actionSet != null)
            {
                actionSet.ActivatePrimary();
            }
#endif

#if VIU_STEAMVR_2_2_0_OR_NEWER
            SteamVR_Input.onNonVisualActionsUpdated += UpdateDeviceInput;
            SteamVR_Input.onPosesUpdated += UpdateDeviceConnectionAndPose;
#else
            SteamVR_Input.OnNonVisualActionsUpdated += UpdateDeviceInput;
            SteamVR_Input.OnPosesUpdated += UpdateDeviceConnectionAndPose;
#endif

            //s_devicePathHandles = new ulong[OpenVR.k_unMaxTrackedDeviceCount];
            EnsureDeviceStateLength(8);

            // preserve previous tracking space
            m_prevTrackingSpace = trackingSpace;

            m_hasInputFocus = inputFocus;

            SteamVR_Events.InputFocus.AddListener(OnInputFocus);

            s_moduleInstance = this;

            m_submodules.ActivateAllModules();
        }

        public override void OnDeactivated()
        {
            m_submodules.DeactivateAllModules();

            SteamVR_Events.InputFocus.RemoveListener(OnInputFocus);

#if VIU_STEAMVR_2_2_0_OR_NEWER
            SteamVR_Input.onNonVisualActionsUpdated -= UpdateDeviceInput;
            SteamVR_Input.onPosesUpdated -= UpdateDeviceConnectionAndPose;
#else
            SteamVR_Input.OnNonVisualActionsUpdated -= UpdateDeviceInput;
            SteamVR_Input.OnPosesUpdated -= UpdateDeviceConnectionAndPose;
#endif
            m_originDataCache.ClearCache();
            m_indexMap.Clear();
            trackingSpace = m_prevTrackingSpace;

            s_moduleInstance = null;
        }

        private void UpdateDeviceInput()
        {
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
                m_originDataCache.ClearCache();

                foreach (var actionDeviceData in AllActionDevicePath(vrInput, pressActions))
                {
                    bool digitValue;
                    var error = TryGetDigitalValue(vrInput, actionDeviceData.actionHandle, actionDeviceData.devicePathHandle, out digitValue);
                    if (error == EVRInputError.None)
                    {
                        actionDeviceData.currState.SetButtonPress(actionDeviceData.inputKey, digitValue);
                    }
                    else
                    {
                        Debug.LogError("[SteamVRv2Module] CVRInput.GetDigitalActionData fail. action=" + pressActions.ActionPaths[actionDeviceData.inputKey] + " error=" + error);
                    }
                }

                foreach (var actionDeviceData in AllActionDevicePath(vrInput, touchActions))
                {
                    bool value;
                    var error = TryGetDigitalValue(vrInput, actionDeviceData.actionHandle, actionDeviceData.devicePathHandle, out value);
                    if (error == EVRInputError.None)
                    {
                        actionDeviceData.currState.SetButtonTouch(actionDeviceData.inputKey, value);
                    }
                    else
                    {
                        Debug.LogError("[SteamVRv2Module] CVRInput.GetDigitalActionData fail. action=" + touchActions.ActionPaths[actionDeviceData.inputKey] + " error=" + error);
                    }
                }

                foreach (var actionDeviceData in AllActionDevicePath(vrInput, v1Actions))
                {
                    Vector3 value;
                    var error = TryGetAnalogValue(vrInput, actionDeviceData.actionHandle, actionDeviceData.devicePathHandle, out value);
                    if (error == EVRInputError.None)
                    {
                        actionDeviceData.currState.SetAxisValue(actionDeviceData.inputKey, value.x);
                    }
                    else
                    {
                        Debug.LogError("[SteamVRv2Module] CVRInput.GetAnalogActionData fail. action=" + v1Actions.ActionPaths[actionDeviceData.inputKey] + " error=" + error);
                    }
                }

                foreach (var actionDeviceData in AllActionDevicePath(vrInput, v2Actions))
                {
                    Vector3 value;
                    var error = TryGetAnalogValue(vrInput, actionDeviceData.actionHandle, actionDeviceData.devicePathHandle, out value);
                    if (error == EVRInputError.None)
                    {
                        actionDeviceData.currState.SetAxisValue(actionDeviceData.inputKey, value.x);
                        actionDeviceData.currState.SetAxisValue(actionDeviceData.inputKey + 1, value.y);
                    }
                    else
                    {
                        Debug.LogError("[SteamVRv2Module] CVRInput.GetAnalogActionData fail. action=" + v2Actions.ActionPaths[actionDeviceData.inputKey] + " error=" + error);
                    }
                }
            }

            m_submodules.UpdateAllModulesActivity();
            m_submodules.UpdateModulesDeviceInput();

            ProcessDeviceInputChanged();
        }

        private void UpdateDeviceConnectionAndPose(bool obj)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            FlushDeviceState();

            var vrSystem = OpenVR.System;
            var vrCompositor = OpenVR.Compositor;
            if (vrSystem == null || vrCompositor == null)
            {
                // clear all device states if OpenVR disabled
                for (uint i = 0, imax = GetDeviceStateLength(); i < imax; ++i)
                {
                    if (TryGetValidDeviceState(i, out prevState, out currState) && currState.isConnected)
                    {
                        currState.Reset();
                    }
                }
            }
            else
            {
                vrCompositor.GetLastPoses(m_poses, m_gamePoses);

                for (uint i = 0u, imax = (uint)m_poses.Length; i < imax; ++i)
                {
                    if (!m_poses[i].bDeviceIsConnected)
                    {
                        uint moduleIndex;
                        if (m_indexMap.UnmapTracked(i, out moduleIndex))
                        {
                            if (TryGetValidDeviceState(moduleIndex, out prevState, out currState))
                            {
                                currState.Reset();
                            }
                        }
                    }
                    else
                    {
                        uint moduleIndex;
                        if (!m_indexMap.TryGetModuleIndex(i, out moduleIndex))
                        {
                            moduleIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);
                            m_indexMap.Map(i, moduleIndex);
                        }
                        else
                        {
                            EnsureValidDeviceState(moduleIndex, out prevState, out currState);
                        }

                        if (!prevState.isConnected)
                        {
                            currState.isConnected = true;
                            currState.deviceClass = ToVRModuleDeviceClass(vrSystem.GetTrackedDeviceClass(i));
                            currState.serialNumber = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_SerialNumber_String);
                            currState.modelNumber = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                            currState.renderModelName = QueryDeviceStringProperty(vrSystem, i, ETrackedDeviceProperty.Prop_RenderModelName_String);

                            SetupKnownDeviceModel(currState);
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
            }

            m_submodules.UpdateAllModulesActivity();
            m_submodules.UpdateModulesDeviceConnectionAndPoses();

            // process hand role
            bool roleChanged = false;
            if (vrSystem == null)
            {
                m_openvrRightIndex = INVALID_DEVICE_INDEX;
                m_openvrLeftIndex = INVALID_DEVICE_INDEX;
            }
            else
            {
                var right = vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
                var left = vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);

                uint openvrRight;
                m_indexMap.TryGetModuleIndex(right, out openvrRight);
                roleChanged |= ChangeProp.Set(ref m_openvrRightIndex, openvrRight);

                uint openvrLeft;
                m_indexMap.TryGetModuleIndex(left, out openvrLeft);
                roleChanged |= ChangeProp.Set(ref m_openvrLeftIndex, openvrLeft);
            }

            var moduleRight = m_openvrRightIndex != INVALID_DEVICE_INDEX ? m_openvrRightIndex : m_submodules.GetFirstRightHandedIndex();
            var moduleLeft = m_openvrLeftIndex != INVALID_DEVICE_INDEX ? m_openvrLeftIndex : m_submodules.GetFirstLeftHandedIndex();
            roleChanged |= ChangeProp.Set(ref m_moduleRightIndex, moduleRight);
            roleChanged |= ChangeProp.Set(ref m_moduleLeftIndex, moduleLeft);

            UpdateHandJoints(m_openvrLeftIndex, skeletonActionHandleLeft, true);
            UpdateHandJoints(m_openvrRightIndex, skeletonActionHandleRight, false);

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();

            if (roleChanged)
            {
                InvokeControllerRoleChangedEvent();
            }
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

        private void UpdateHandJoints(uint deviceIndex, ulong actionHandle, bool isLeft)
        {
            if (deviceIndex == INVALID_DEVICE_INDEX || actionHandle == OpenVR.k_ulInvalidActionHandle)
            {
                return;
            }

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            if (TryGetValidDeviceState(deviceIndex, out prevState, out currState))
            {
                RigidPose fixedHandPose = currState.pose;
                fixedHandPose.rot *= SteamVR_Action_Skeleton.steamVRFixUpRotation;

                EVRSkeletalMotionRange skeletonType = (EVRSkeletalMotionRange)(isLeft ? VIUSettings.steamVRLeftSkeletonMode : VIUSettings.steamVRRightSkeletonMode);
                EVRInputError boneError = OpenVR.Input.GetSkeletalBoneData(actionHandle, EVRSkeletalTransformSpace.Model, skeletonType, s_tempBoneTransforms);
                if (boneError == EVRInputError.None)
                {
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.Wrist, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.wrist);

                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.ThumbMetacarpal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.thumbMetacarpal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.ThumbProximal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.thumbProximal);
                    //AssignHandJoint(handPose, isLeft, currState.handJoints, HandJointName.ThumbIntermediate, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.thumbMiddle);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.ThumbDistal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.thumbDistal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.ThumbTip, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.thumbTip);

                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.IndexMetacarpal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.indexMetacarpal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.IndexProximal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.indexProximal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.IndexIntermediate, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.indexMiddle);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.IndexDistal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.indexDistal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.IndexTip, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.indexTip);

                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.MiddleMetacarpal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.middleMetacarpal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.MiddleProximal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.middleProximal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.MiddleIntermediate, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.middleMiddle);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.MiddleDistal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.middleDistal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.MiddleTip, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.middleTip);

                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.RingMetacarpal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.ringMetacarpal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.RingProximal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.ringProximal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.RingIntermediate, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.ringMiddle);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.RingDistal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.ringDistal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.RingTip, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.ringTip);

                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.PinkyMetacarpal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.pinkyMetacarpal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.PinkyProximal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.pinkyProximal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.PinkyIntermediate, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.pinkyMiddle);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.PinkyDistal, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.pinkyDistal);
                    AssignHandJoint(fixedHandPose, isLeft, currState.handJoints, HandJointName.PinkyTip, s_tempBoneTransforms, SteamVR_Skeleton_JointIndexes.pinkyTip);
                }
            }
        }

        private void AssignHandJoint(RigidPose handPose, bool isLeft, JointEnumArray joints, HandJointName jointName, VRBoneTransform_t[] boneTransforms, int steamBoneIndex)
        {
            VRBoneTransform_t bone = boneTransforms[steamBoneIndex];
            Vector3 position = new Vector3(-bone.position.v0, bone.position.v1, bone.position.v2);
            Quaternion rotation = new Quaternion(bone.orientation.x, -bone.orientation.y, -bone.orientation.z, bone.orientation.w);

            if (steamBoneIndex == SteamVR_Skeleton_JointIndexes.wrist)
            {
                if (isLeft)
                {
                    rotation *= s_leftSkeletonWristFixRotation;
                }
                else
                {
                    rotation *= s_rightSkeletonWristFixRotation;
                }
            }
            else
            {
                if (isLeft)
                {
                    rotation *= s_leftSkeletonFixRotation;
                }
                else
                {
                    rotation *= s_rightSkeletonFixRotation;
                }
            }

            joints[jointName] = new JointPose(handPose * new RigidPose(position, rotation));
        }

        private void OnInputFocus(bool value)
        {
            m_hasInputFocus = value;
            InvokeInputFocusEvent(value);
        }

        public override bool HasInputFocus() { return m_hasInputFocus; }

        public override uint GetLeftControllerDeviceIndex() { return m_moduleLeftIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_moduleRightIndex; }

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
            var trackedIndex = m_indexMap.Module2TrackedIndex(deviceIndex);
            var handle = m_originDataCache.GetPathHandleByTrackedIndex(trackedIndex);
            if (handle == OpenVR.k_ulInvalidDriverHandle) { return; }

            var vrInput = OpenVR.Input;
            if (vrInput != null)
            {
                var error = vrInput.TriggerHapticVibrationAction(vibrateActions.ActionHandles[HapticStruct.Haptic], startSecondsFromNow, durationSeconds, frequency, amplitude, handle);
                if (error != EVRInputError.None)
                {
                    Debug.LogError("TriggerViveControllerHaptic failed! error=" + error);
                }
            }
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

        private EVRInputError TryGetDigitalValue(CVRInput vrInput, ulong actionHandle, ulong devicePathHandle, out bool value)
        {
            var data = default(InputDigitalActionData_t);
            var error = vrInput.GetDigitalActionData(actionHandle, ref data, m_digitalDataSize, devicePathHandle);

            value = data.bState;
            return error;
        }

        private EVRInputError TryGetAnalogValue(CVRInput vrInput, ulong actionHandle, ulong devicePathHandle, out Vector3 value)
        {
            var data = default(InputAnalogActionData_t);
            var error = vrInput.GetAnalogActionData(actionHandle, ref data, m_analogDataSize, devicePathHandle);

            value.x = data.x;
            value.y = data.y;
            value.z = data.z;
            return error;
        }

        private struct OriginData
        {
            public ulong devicePathHandle;
            public uint deviceIndex;
        }

        private class OriginDataCache
        {
            private static readonly uint originInfoSize = (uint)Marshal.SizeOf(new InputOriginInfo_t());
            private readonly Dictionary<ulong, OriginData> cache = new Dictionary<ulong, OriginData>();
            private readonly ulong[] index2PathHandle = new ulong[OpenVR.k_unMaxTrackedDeviceCount];

            public void ClearCache()
            {
                Array.Clear(index2PathHandle, 0, (int)OpenVR.k_unMaxTrackedDeviceCount);
                cache.Clear();
            }

            public ulong GetPathHandleByTrackedIndex(uint trackedIndex)
            {
                return trackedIndex < index2PathHandle.Length ? index2PathHandle[trackedIndex] : OpenVR.k_ulInvalidPathHandle;
            }

            public EVRInputError TryGetOriginData(CVRInput vrInput, ulong originHandle, out OriginData originData)
            {
                if (cache.TryGetValue(originHandle, out originData)) { return EVRInputError.None; }

                var info = default(InputOriginInfo_t);
                var error = vrInput.GetOriginTrackedDeviceInfo(originHandle, ref info, originInfoSize);
                if (error == EVRInputError.None)
                {
                    cache.Add(originHandle, originData = new OriginData()
                    {
                        devicePathHandle = info.devicePath,
                        deviceIndex = info.trackedDeviceIndex,
                    });
                    index2PathHandle[originData.deviceIndex] = originData.devicePathHandle;
                }
                else
                {
                    originData = default(OriginData);
                }

                return error;
            }
        }

        private ActionCollection<T>.Enumerable AllActionDevicePath<T>(CVRInput vrInput, ActionCollection<T> actionCollection)
#if CSHARP_7_OR_LATER
            where T : Enum
#endif
        {
            return new ActionCollection<T>.Enumerable(AllActionDevicePathEnumerator(vrInput, actionCollection));
        }

        private static ulong[] outActionOrigins = new ulong[OpenVR.k_unMaxActionOriginCount];
        private IEnumerator<ActionCollection<T>.EnumData> AllActionDevicePathEnumerator<T>(CVRInput vrInput, ActionCollection<T> actionCollection)
#if CSHARP_7_OR_LATER
            where T : Enum
#endif
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            EVRInputError error;
            var current = default(ActionCollection<T>.EnumData);
            foreach (var p in actionCollection.ActionHandles.EnumValues)
            {
                current.inputKey = p.Key;
                current.actionHandle = p.Value;
                if (current.actionHandle == OpenVR.k_ulInvalidActionHandle)
                {
                    continue;
                }

                Array.Clear(outActionOrigins, 0, outActionOrigins.Length);
                error = vrInput.GetActionOrigins(s_actionSetHandle, current.actionHandle, outActionOrigins);

                while (error == EVRInputError.BufferTooSmall && outActionOrigins.Length <= 4096)
                {
                    Array.Resize(ref outActionOrigins, outActionOrigins.Length * 2);
                    Debug.LogWarning("Expanding outActionOrigins size to " + outActionOrigins.Length);
                    error = vrInput.GetActionOrigins(s_actionSetHandle, current.actionHandle, outActionOrigins);
                }

                if (error != EVRInputError.None)
                {
                    Debug.LogError("[SteamVRv2Module] CVRInput.GetActionOrigins fail. input=" + p.Key + " action=" + actionCollection.ActionPaths[p.Key] + " error=" + error);
                    continue;
                }

                foreach (var originHandle in outActionOrigins)
                {
                    if (originHandle == 0ul) { break; }

                    OriginData data;
                    error = m_originDataCache.TryGetOriginData(vrInput, originHandle, out data);
                    if (error != EVRInputError.None)
                    {
                        Debug.LogError("[SteamVRv2Module] CVRInput.GetOriginTrackedDeviceInfo fail. input=" + p.Key + " action=" + actionCollection.ActionPaths[p.Key] + " originHandle=" + originHandle + " error=" + error);
                        continue;
                    }

                    current.devicePathHandle = data.devicePathHandle;
                    uint moduleIndex;
                    if (!m_indexMap.TryGetModuleIndex(data.deviceIndex, out moduleIndex)) { continue; }
                    if (!TryGetValidDeviceState(moduleIndex, out prevState, out currState)) { continue; }

                    current.currState = currState;
                    yield return current;
                }
            }
        }

        public class ActionCollection<T>
#if CSHARP_7_OR_LATER
            where T : Enum
#endif
        {
            public struct EnumData
            {
                public T inputKey;
                public ulong actionHandle;
                public ulong devicePathHandle;
                public IVRModuleDeviceStateRW currState;
            }

            public struct Enumerable
            {
                private IEnumerator<EnumData> enumerator;
                public Enumerable(IEnumerator<EnumData> enumerator) { this.enumerator = enumerator; }
                public IEnumerator<EnumData> GetEnumerator() { return enumerator; }
            }

            private readonly string pathPrefix;
            private readonly string dataTypeName;
            private readonly EnumArray<T, string> actionPaths = new EnumArray<T, string>();
            private readonly EnumArray<T, string> actionAlias = new EnumArray<T, string>();
            private readonly EnumArray<T, ulong> actionHandles = new EnumArray<T, ulong>();

            public string PathPrefix { get { return pathPrefix; } }
            public string DataTypeName { get { return dataTypeName; } }
            public EnumArray<T, string>.IReadOnly ActionPaths { get { return actionPaths.ReadOnly; } }
            public EnumArray<T, string>.IReadOnly ActionAlias { get { return actionAlias.ReadOnly; } }
            public EnumArray<T, ulong>.IReadOnly ActionHandles { get { return actionHandles.ReadOnly; } }

            public ActionCollection(string pathPrefix, string dataTypeName)
            {
                this.pathPrefix = pathPrefix;
                this.dataTypeName = dataTypeName;
            }

            public void Set(T key, string pathName, string alias)
            {
                actionPaths[key] = ACTION_SET_PATH + pathPrefix + pathName;
                actionAlias[key] = alias;
            }

            public void ResolveHandles(CVRInput vrInput)
            {
                foreach (var key in EnumArrayBase<T>.StaticEnums)
                {
                    actionHandles[key] = SafeGetActionHandle(vrInput, actionPaths[key]);
                }
            }
        }
#endif
    }
}
