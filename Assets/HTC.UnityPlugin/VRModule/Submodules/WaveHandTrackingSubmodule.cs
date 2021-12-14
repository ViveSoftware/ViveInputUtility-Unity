//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using System;
using System.Collections;
using HTC.UnityPlugin.LiteCoroutineSystem;
using HTC.UnityPlugin.Utility;
using System.Runtime.InteropServices;

#if VIU_WAVEVR_HAND_TRACKING
using Wave.Native;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class WaveHandTrackingSubmodule : VRModule.SubmoduleBase
    {
#if VIU_WAVEVR_HAND_TRACKING
        private static readonly string log_prefix = "[" + typeof(WaveHandTrackingSubmodule).Name + "] ";

        private struct DeviceFeature
        {
            private ulong featuresField;

            public bool supportTracking
            {
                get { return (featuresField & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_HandTracking) > 0ul; }
            }

            public bool supportGesture
            {
                get { return (featuresField & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_HandGesture) > 0ul; }
            }

            public void Fetch()
            {
                featuresField = Interop.WVR_GetSupportedFeatures();
            }
        }

        private DeviceFeature deviceFeature;
        private static TrackingActivator trackingActivator = TrackingActivator.Default;
        private GestureActivator gestureActivator = GestureActivator.Default;
        private uint leftDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint rightDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private static WVR_HandTrackerType preferredTrackerType = WVR_HandTrackerType.WVR_HandTrackerType_Natural;
        private static WVR_HandModelType showElectronicHandWithController =
            VRModuleSettings.showWaveElectronicHandWithController ?
            WVR_HandModelType.WVR_HandModelType_WithController : WVR_HandModelType.WVR_HandModelType_WithoutController;

        public override bool ShouldActiveModule() { return VRModuleSettings.activateWaveHandTrackingSubmodule; }

        protected override void OnActivated()
        {
            deviceFeature.Fetch();
        }

        protected override void OnDeactivated()
        {
            trackingActivator.SetActive(false);
            gestureActivator.SetActive(false);
        }

        protected override void OnUpdateDeviceConnectionAndPoses()
        {
            trackingActivator.SetActive(deviceFeature.supportTracking);

            if (VRModule.trackingSpaceType == VRModuleTrackingSpaceType.RoomScale)
            {
                trackingActivator.TryFetchData(WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnGround);
            }
            else
            {
                trackingActivator.TryFetchData(WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead);
            }

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            // update connection/pose for left hand devices
            if (trackingActivator.isLeftValid)
            {
                if (leftDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(leftDeviceIndex, out prevState, out currState);
                }
                else
                {
                    leftDeviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

                    currState.serialNumber = "WaveTrackedHandLeft";
                    currState.modelNumber = "WaveTrackedHandLeft";
                    currState.renderModelName = "WaveTrackedHandLeft";

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.deviceModel = VRModuleDeviceModel.WaveTrackedHandLeft;
                    currState.input2DType = VRModuleInput2DType.None;
                }

                currState.isConnected = true;
                trackingActivator.UpdateJoints(currState, true);
            }
            else
            {
                if (leftDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(leftDeviceIndex, out prevState, out currState);
                    currState.Reset();
                    leftDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
                }
            }

            if (trackingActivator.isRightValid)
            {
                if (rightDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(rightDeviceIndex, out prevState, out currState);
                }
                else
                {
                    rightDeviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

                    currState.serialNumber = "WaveTrackedHandRight";
                    currState.modelNumber = "WaveTrackedHandRight";
                    currState.renderModelName = "WaveTrackedHandRight";

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.deviceModel = VRModuleDeviceModel.WaveTrackedHandRight;
                    currState.input2DType = VRModuleInput2DType.None;
                }

                currState.isConnected = true;
                trackingActivator.UpdateJoints(currState, false);
            }
            else
            {
                if (rightDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(rightDeviceIndex, out prevState, out currState);
                    currState.Reset();
                    rightDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
                }
            }
        }

        protected override void OnUpdateDeviceInput()
        {
            gestureActivator.SetActive(VRModuleSettings.enableWaveHandGesture && deviceFeature.supportGesture);

            gestureActivator.TryFetchData();

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            if (leftDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
            {
                if (gestureActivator.isLeftValid)
                {
                    EnsureValidDeviceState(leftDeviceIndex, out prevState, out currState);
                    gestureActivator.UpdateGestureInput(currState, true);
                }

                if (trackingActivator.isLeftValid)
                {
                    EnsureValidDeviceState(leftDeviceIndex, out prevState, out currState);
                    trackingActivator.UpdateDeviceInput(currState, true);
                }
            }

            if (rightDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
            {
                if (gestureActivator.isRightValid)
                {
                    EnsureValidDeviceState(rightDeviceIndex, out prevState, out currState);
                    gestureActivator.UpdateGestureInput(currState, false);
                }

                if (trackingActivator.isRightValid)
                {
                    EnsureValidDeviceState(rightDeviceIndex, out prevState, out currState);
                    trackingActivator.UpdateDeviceInput(currState, false);
                }
            }
        }

        public override uint GetLeftHandedIndex() { return leftDeviceIndex; }

        public override uint GetRightHandedIndex() { return rightDeviceIndex; }

        public static bool TryGetLeftPinchRay(out Vector3 origin, out Vector3 direction)
        {
            if (!trackingActivator.isLeftValid)
            {
                origin = Vector3.zero;
                direction = Vector3.zero;

                return false;
            }

            var pinch = trackingActivator.getLeftPinchData;
            Coordinate.GetVectorFromGL(pinch.pinch.origin, out origin);
            Coordinate.GetVectorFromGL(pinch.pinch.direction, out direction);

            return true;
        }

        public static bool TryGetRightPinchRay(out Vector3 origin, out Vector3 direction)
        {
            if (!trackingActivator.isRightValid)
            {
                origin = Vector3.zero;
                direction = Vector3.zero;

                return false;
            }

            var pinch = trackingActivator.getRightPinchData;
            Coordinate.GetVectorFromGL(pinch.pinch.origin, out origin);
            Coordinate.GetVectorFromGL(pinch.pinch.direction, out direction);

            return true;
        }

        private enum FeatureActivity
        {
            Stopped,
            StartFailed,
            Starting,
            Started,
        }

        private class FeatureActivator
        {
            private string featureName;
            private FeatureActivity activity;
            private bool shouldActive;
            private LiteCoroutine coroutineHandle;
            private Func<WVR_Result> starter;
            private Func<WVR_Result> initializer;
            private Action stopper;

            public FeatureActivator(string featureName, Func<WVR_Result> starter, Func<WVR_Result> initializer, Action stopper)
            {
                this.featureName = featureName;
                this.activity = FeatureActivity.Stopped;
                this.shouldActive = false;
                this.coroutineHandle = null;
                this.starter = starter;
                this.initializer = initializer;
                this.stopper = stopper;
            }

            public bool isActive { get { return activity == FeatureActivity.Started; } }

            public void SetActive(bool value)
            {
                if (value) { Activate(); }
                else { Deactivate(); }
            }

            public void Activate()
            {
                shouldActive = true;

                if (activity == FeatureActivity.Stopped)
                {
                    LiteCoroutine.StartCoroutine(ref coroutineHandle, new LiteTask(ActivateCoroutine(), false));
                }
            }

            public void Deactivate()
            {
                shouldActive = false;

                if (activity == FeatureActivity.Started || activity == FeatureActivity.StartFailed)
                {
                    activity = FeatureActivity.Stopped;
                    stopper();
                    Debug.Log(log_prefix + "Stop " + featureName + " done.");
                }
            }

            private IEnumerator ActivateCoroutine()
            {
                yield return LiteTask.ToForeground;

                const long retryInterval = 1000L;
                var nextRestartTime = default(DateTime);
                var result = default(WVR_Result);
                while (true)
                {
                    if (shouldActive)
                    {
                        switch (activity)
                        {
                            case FeatureActivity.Stopped:
                                activity = FeatureActivity.Starting;
                                break;

                            case FeatureActivity.Starting:
                                break;

                            case FeatureActivity.Started:
                            case FeatureActivity.StartFailed:
                            default:
                                yield break;
                        }
                    }
                    else
                    {
                        switch (activity)
                        {
                            case FeatureActivity.Started:
                            case FeatureActivity.StartFailed:
                                stopper();
                                Debug.Log(log_prefix + "Stop " + featureName + " done.");
                                activity = FeatureActivity.Stopped;
                                yield break;

                            case FeatureActivity.Starting:
                            case FeatureActivity.Stopped:
                            default:
                                yield break;
                        }
                    }

                    if (DateTime.UtcNow < nextRestartTime)
                    {
                        yield return null;
                        continue;
                    }

                    yield return LiteTask.ToBackground;

                    result = starter();
                    if (result == WVR_Result.WVR_Success)
                    {
                        result = initializer();
                    }

                    yield return LiteTask.ToForeground;

                    switch (result)
                    {
                        case WVR_Result.WVR_Error_SystemInvalid:
                            nextRestartTime = DateTime.UtcNow + new TimeSpan(retryInterval * TimeSpan.TicksPerMillisecond);
                            Debug.LogWarning(log_prefix + "Start " + featureName + " fail (system not ready). Retrying in " + retryInterval + " milliseconds...");
                            yield return null;
                            break;

                        case WVR_Result.WVR_Success:
                            Debug.Log(log_prefix + "Start " + featureName + " success.");
                            activity = FeatureActivity.Started;
                            break;

                        default:
                            Debug.LogError(log_prefix + "Start " + featureName + " error:" + result);
                            activity = FeatureActivity.StartFailed;
                            break;
                    }
                }
            }
        }

        private class TrackingActivator
        {
            private FeatureActivator activator;
            private static WVR_HandTrackerInfo_t trackerInfo = new WVR_HandTrackerInfo_t();
            private static WVR_HandTrackingData_t trackingData;
            private static WVR_HandPoseData_t pinchData;
            private static WVR_HandJoint[] s_NaturalHandJoints;
            private static ulong[] s_NaturalHandJointsFlag;
            private static WVR_HandJointData_t m_NaturalHandJointDataLeft = new WVR_HandJointData_t();
            private static WVR_Pose_t[] s_NaturalHandJointsPoseLeft;
            private static WVR_HandJointData_t m_NaturalHandJointDataRight = new WVR_HandJointData_t();
            private static WVR_Pose_t[] s_NaturalHandJointsPoseRight;
            private static int[] intJointMappingArray;
            private static byte[] jointValidFlagArrayBytes;
            private static uint jointCount;
            private static EnumArray<WVR_HandJoint, HandJointName> handJointMapping;
            static TrackingActivator()
            {
                handJointMapping = new EnumArray<WVR_HandJoint, HandJointName>();
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Palm] = HandJointName.Palm;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Wrist] = HandJointName.Wrist;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Thumb_Joint0] = HandJointName.ThumbMetacarpal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Thumb_Joint1] = HandJointName.ThumbProximal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Thumb_Joint2] = HandJointName.ThumbDistal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Thumb_Tip] = HandJointName.ThumbTip;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Index_Joint0] = HandJointName.IndexMetacarpal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Index_Joint1] = HandJointName.IndexProximal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Index_Joint2] = HandJointName.IndexIntermediate;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Index_Joint3] = HandJointName.IndexDistal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Index_Tip] = HandJointName.IndexTip;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Middle_Joint0] = HandJointName.MiddleMetacarpal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Middle_Joint1] = HandJointName.MiddleProximal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Middle_Joint2] = HandJointName.MiddleIntermediate;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Middle_Joint3] = HandJointName.MiddleDistal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Middle_Tip] = HandJointName.MiddleTip;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Ring_Joint0] = HandJointName.RingMetacarpal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Ring_Joint1] = HandJointName.RingProximal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Ring_Joint2] = HandJointName.RingIntermediate;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Ring_Joint3] = HandJointName.RingDistal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Ring_Tip] = HandJointName.RingTip;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Pinky_Joint0] = HandJointName.PinkyMetacarpal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Pinky_Joint1] = HandJointName.PinkyProximal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Pinky_Joint2] = HandJointName.PinkyIntermediate;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Pinky_Joint3] = HandJointName.PinkyDistal;
                handJointMapping[WVR_HandJoint.WVR_HandJoint_Pinky_Tip] = HandJointName.PinkyTip;
            }

            public static TrackingActivator Default
            {
                get
                {
                    return new TrackingActivator()
                    {
                        activator = new FeatureActivator("HandTracking", GenerateStarter(), GenerateInitializer(), GenerateStopper()),
                    };
                }
            }

            private static Func<WVR_Result> GenerateStarter()
            {
                return () =>
                {
                    return Interop.WVR_StartHandTracking(preferredTrackerType);
                };
            }

            private static Action GenerateStopper()
            {
                return () =>
                {
                    Interop.WVR_StopHandTracking(preferredTrackerType);
                };
            }

            private static Func<WVR_Result> GenerateInitializer()
            {
                return () =>
                {
                    var result = Interop.WVR_GetHandJointCount(preferredTrackerType, ref jointCount);
                    if (result != WVR_Result.WVR_Success) return result;

                    InitializeHandTrackerInfo(ref trackerInfo, ref s_NaturalHandJoints, ref s_NaturalHandJointsFlag, jointCount);
                    InitializeHandTrackerData(
                        ref trackingData,
                        ref m_NaturalHandJointDataLeft,
                        ref m_NaturalHandJointDataRight,
                        ref s_NaturalHandJointsPoseLeft,
                        ref s_NaturalHandJointsPoseRight,
                        jointCount);
                    var trackerInfoResult = Interop.WVR_GetHandTrackerInfo(preferredTrackerType, ref trackerInfo);
                    var hasTrackerInfo = ExtractHandTrackerInfo(trackerInfo, ref s_NaturalHandJoints, ref s_NaturalHandJointsFlag);
                    //if (hasTrackerInfo)
                    //{
                    //    for (int i = 0; i < trackerInfo.jointCount; i++)
                    //    {
                    //        Debug.Log("GetHandTrackerInfo() "
                    //            + "joint count: " + trackerInfo.jointCount
                    //            + ", s_NaturalHandJoints[" + i + "] = " + s_NaturalHandJoints[i]
                    //            + ", s_NaturalHandJointsFlag[" + i + "] = " + s_NaturalHandJointsFlag[i]);
                    //    }
                    //}
                    return default(WVR_Result);
                };
            }

            public void SetActive(bool value) { activator.SetActive(value); }

            public bool TryFetchData(WVR_PoseOriginModel originModel)
            {
                if (activator.isActive && jointCount > 0)
                {
                    var result = Interop.WVR_GetHandTrackingData(preferredTrackerType,
                        (preferredTrackerType == WVR_HandTrackerType.WVR_HandTrackerType_Natural) ? WVR_HandModelType.WVR_HandModelType_WithoutController : showElectronicHandWithController,
                        originModel, ref trackingData, ref pinchData);
                    if (result == WVR_Result.WVR_Success)
                    {
                        ExtractHandTrackerData(trackingData, ref s_NaturalHandJointsPoseLeft, ref s_NaturalHandJointsPoseRight);

                        return true;
                    }

                    trackingData.left.isValidPose = false;
                    trackingData.right.isValidPose = false;
                    pinchData.left.state.type = WVR_HandPoseType.WVR_HandPoseType_Invalid;
                    pinchData.right.state.type = WVR_HandPoseType.WVR_HandPoseType_Invalid;
                    Debug.LogError(log_prefix + "WVR_GetHandTrackingData fail. error:" + result);
                }

                return false;
            }

            public bool isLeftValid { get { return trackingData.left.isValidPose && !Interop.WVR_IsInputFocusCapturedBySystem(); } }

            public bool isRightValid { get { return trackingData.right.isValidPose && !Interop.WVR_IsInputFocusCapturedBySystem(); } }

            public WVR_HandPoseState_t getLeftPinchData { get { return pinchData.left; } }

            public WVR_HandPoseState_t getRightPinchData { get { return pinchData.right; } }

            public void UpdateJoints(IVRModuleDeviceStateRW state, bool isLeft)
            {
                var data = isLeft ? trackingData.left : trackingData.right;
                var pose = isLeft ? s_NaturalHandJointsPoseLeft : s_NaturalHandJointsPoseRight;

                for (int i = 0; i < trackerInfo.jointCount; i++)
                {
                    var p = default(RigidPose);
                    Coordinate.GetVectorFromGL(pose[i].position, out p.pos);
                    Coordinate.GetQuaternionFromGL(pose[i].rotation, out p.rot);
                    state.handJoints[handJointMapping[s_NaturalHandJoints[i]]] = new JointPose(p);
                }

                state.isPoseValid = data.isValidPose && !Interop.WVR_IsInputFocusCapturedBySystem();
                state.pose = state.handJoints[HandJointName.Wrist].pose;
            }

            public void UpdateDeviceInput(IVRModuleDeviceStateRW state, bool isLeft)
            {
                var pinch = isLeft ? pinchData.left : pinchData.right;
                var pinched = pinch.pinch.strength >= 0.5f;

                state.SetButtonPress(VRModuleRawButton.GestureIndexPinch, pinched);
                state.SetButtonTouch(VRModuleRawButton.GestureIndexPinch, pinched);
                state.SetAxisValue(VRModuleRawAxis.Trigger, pinch.pinch.strength);

                var indexCurl = GetFingerCurl(state, HandJointName.IndexTip);
                var middleCurl = GetFingerCurl(state, HandJointName.MiddleTip);
                var ringCurl = GetFingerCurl(state, HandJointName.RingTip);
                var pinkyCurl = GetFingerCurl(state, HandJointName.PinkyTip);
                var curlAvg = (indexCurl + middleCurl + ringCurl + pinkyCurl) * 0.25f;

                state.SetAxisValue(VRModuleRawAxis.IndexCurl, indexCurl);
                state.SetAxisValue(VRModuleRawAxis.MiddleCurl, middleCurl);
                state.SetAxisValue(VRModuleRawAxis.RingCurl, ringCurl);
                state.SetAxisValue(VRModuleRawAxis.PinkyCurl, pinkyCurl);
                state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, curlAvg);
                state.SetButtonPress(VRModuleRawButton.Grip, curlAvg > 0.75f);
                state.SetButtonTouch(VRModuleRawButton.Grip, curlAvg > 0.50f);
            }

            private float GetFingerCurl(IVRModuleDeviceStateRW state, HandJointName finger)
            {
                var palmDir = state.pose.forward;
                var fingerDir = state.handJoints[finger].pose.forward;
                var angle = Vector3.SignedAngle(palmDir, fingerDir, state.pose.right);
                if (angle < -90f) { angle += 360f; }
                return Mathf.InverseLerp(0f, 180f, angle);
            }

            private static void InitializeHandTrackerInfo(ref WVR_HandTrackerInfo_t handTrackerInfo, ref WVR_HandJoint[] jointMappingArray, ref ulong[] jointValidFlagArray, uint count)
            {
                handTrackerInfo.jointCount = jointCount;
                handTrackerInfo.handModelTypeBitMask = 0;

                /// WVR_HandTrackerInfo_t.jointMappingArray
                jointMappingArray = new WVR_HandJoint[jointCount];
                intJointMappingArray = new int[jointMappingArray.Length];
                intJointMappingArray = Array.ConvertAll(jointMappingArray, delegate (WVR_HandJoint value) { return (int)value; });
                handTrackerInfo.jointMappingArray = Marshal.AllocHGlobal(sizeof(int) * intJointMappingArray.Length);
                Marshal.Copy(intJointMappingArray, 0, handTrackerInfo.jointMappingArray, intJointMappingArray.Length);
                /*unsafe
                {
                    fixed (WVR_HandJoint* pJointMappingArray = jointMappingArray)
                    {
                        handTrackerInfo.jointMappingArray = pJointMappingArray;
                    }
                }*/

                /// WVR_HandTrackerInfo_t.jointValidFlagArray
                jointValidFlagArray = new ulong[jointCount];
                int jointValidFlagArrayByteLength = Buffer.ByteLength(jointValidFlagArray);
                jointValidFlagArrayBytes = new byte[jointValidFlagArrayByteLength];
                Buffer.BlockCopy(jointValidFlagArray, 0, jointValidFlagArrayBytes, 0, jointValidFlagArrayBytes.Length);

                handTrackerInfo.jointValidFlagArray = Marshal.AllocHGlobal(sizeof(byte) * jointValidFlagArrayBytes.Length);
                Marshal.Copy(jointValidFlagArrayBytes, 0, handTrackerInfo.jointValidFlagArray, jointValidFlagArrayBytes.Length);
                /*unsafe
                {
                    fixed (ulong* pHandJointsFlag = jointValidFlagArray)
                    {
                        handTrackerInfo.jointValidFlagArray = pHandJointsFlag;
                    }
                }*/
            }

            private static void InitializeHandTrackerData(
                ref WVR_HandTrackingData_t handTrackerData,
                ref WVR_HandJointData_t handJointDataLeft,
                ref WVR_HandJointData_t handJointDataRight,
                ref WVR_Pose_t[] handJointsPoseLeft,
                ref WVR_Pose_t[] handJointsPoseRight,
                uint count)
            {
                handTrackerData.timestamp = 0;

                InitializeHandJointData(ref handJointDataLeft, ref handJointsPoseLeft, count);
                handTrackerData.left = handJointDataLeft;

                InitializeHandJointData(ref handJointDataRight, ref handJointsPoseRight, count);
                handTrackerData.right = handJointDataRight;
            }

            private static void InitializeHandJointData(ref WVR_HandJointData_t handJointData, ref WVR_Pose_t[] jointsPose, uint count)
            {
                handJointData.isValidPose = false;
                handJointData.confidence = 0;
                handJointData.jointCount = count;

                WVR_Pose_t wvr_pose_type = default(WVR_Pose_t);
                handJointData.joints = Marshal.AllocHGlobal(Marshal.SizeOf(wvr_pose_type) * (int)count);

                jointsPose = new WVR_Pose_t[count];

                long offset = 0;
                if (IntPtr.Size == 4)
                    offset = handJointData.joints.ToInt32();
                else
                    offset = handJointData.joints.ToInt64();

                for (int i = 0; i < jointsPose.Length; i++)
                {
                    IntPtr wvr_pose_ptr = new IntPtr(offset);
                    Marshal.StructureToPtr(jointsPose[i], wvr_pose_ptr, false);
                    offset += Marshal.SizeOf(wvr_pose_type);
                }
            }

            private static bool ExtractHandTrackerInfo(WVR_HandTrackerInfo_t handTrackerInfo, ref WVR_HandJoint[] jointMappingArray, ref ulong[] jointValidFlagArray)
            {
                if (handTrackerInfo.jointCount == 0)
                {
                    Debug.Log("ExtractHandTrackerInfo() WVR_GetHandTrackerInfo WVR_HandTrackerInfo_t jointCount SHOULD NOT be 0!!");
                    return false;
                }

                // WVR_HandTrackerInfo_t.jointMappingArray
                if (jointMappingArray.Length != handTrackerInfo.jointCount)
                {
                    Debug.Log("ExtractHandTrackerInfo() The WVR_GetHandJointCount count (jointMappingArray) " + jointMappingArray.Length
                        + " differs from WVR_GetHandTrackerInfo WVR_HandTrackerInfo_t jointCount " + handTrackerInfo.jointCount);
                    jointMappingArray = new WVR_HandJoint[handTrackerInfo.jointCount];
                    intJointMappingArray = new int[jointMappingArray.Length];
                }

                Marshal.Copy(handTrackerInfo.jointMappingArray, intJointMappingArray, 0, intJointMappingArray.Length);
                jointMappingArray = Array.ConvertAll(intJointMappingArray, delegate (int value) { return (WVR_HandJoint)value; });
                /*unsafe
                {
                    for (int i = 0; i < jointMappingArray.Length; i++)
                    {
                        jointMappingArray[i] = *(handTrackerInfo.jointMappingArray + i);
                    }
                }*/

                // WVR_HandTrackerInfo_t.jointValidFlagArray
                if (jointValidFlagArray.Length != handTrackerInfo.jointCount)
                {
                    Debug.Log("ExtractHandTrackerInfo() The WVR_GetHandJointCount count (jointValidFlagArray) " + jointValidFlagArray.Length
                        + " differs from WVR_GetHandTrackerInfo WVR_HandTrackerInfo_t jointCount " + handTrackerInfo.jointCount);
                    jointValidFlagArray = new ulong[handTrackerInfo.jointCount];
                    int jointValidFlagArrayByteLength = Buffer.ByteLength(jointValidFlagArray);
                    jointValidFlagArrayBytes = new byte[jointValidFlagArrayByteLength];
                }

                Marshal.Copy(handTrackerInfo.jointValidFlagArray, jointValidFlagArrayBytes, 0, jointValidFlagArrayBytes.Length);
                for (int byteIndex = 0; byteIndex < jointValidFlagArrayBytes.Length; byteIndex = byteIndex + 8)
                {
                    int i = (byteIndex / 8);
                    jointValidFlagArray[i] = BitConverter.ToUInt64(jointValidFlagArrayBytes, byteIndex);
                }
                /*unsafe
                {
                    for (int i = 0; i < jointValidFlagArray.Length; i++)
                    {
                        jointValidFlagArray[i] = *(handTrackerInfo.jointValidFlagArray + i);
                    }
                }*/

                return true;
            }

            private static bool ExtractHandTrackerData(WVR_HandTrackingData_t handTrackerData, ref WVR_Pose_t[] handJointsPoseLeft, ref WVR_Pose_t[] handJointsPoseRight)
            {
                if (!ExtractHandJointData(handTrackerData.left, ref handJointsPoseLeft))
                    return false;
                if (!ExtractHandJointData(handTrackerData.right, ref handJointsPoseRight))
                    return false;

                return true;
            }

            private static bool ExtractHandJointData(WVR_HandJointData_t handJointData, ref WVR_Pose_t[] jointsPose)
            {
                if (handJointData.jointCount == 0)
                {
                    Debug.Log("ExtractHandJointData() WVR_GetHandTrackingData WVR_HandJointData_t jointCount SHOULD NOT be 0!!");
                    return false;
                }

                if (jointsPose.Length != handJointData.jointCount)
                {
                    Debug.Log("ExtractHandJointData() The WVR_GetHandJointCount count " + jointsPose.Length
                        + " differs from WVR_GetHandTrackingData WVR_HandJointData_t jointCount " + handJointData.jointCount);
                    jointsPose = new WVR_Pose_t[handJointData.jointCount];
                }

                WVR_Pose_t wvr_pose_type = default(WVR_Pose_t);

                int offset = 0;
                for (int i = 0; i < jointsPose.Length; i++)
                {
                    if (IntPtr.Size == 4)
                        jointsPose[i] = (WVR_Pose_t)Marshal.PtrToStructure(new IntPtr(handJointData.joints.ToInt32() + offset), typeof(WVR_Pose_t));
                    else
                        jointsPose[i] = (WVR_Pose_t)Marshal.PtrToStructure(new IntPtr(handJointData.joints.ToInt64() + offset), typeof(WVR_Pose_t));

                    offset += Marshal.SizeOf(wvr_pose_type);
                }

                return true;
            }
        }

        private class GestureActivator
        {
            private FeatureActivator activator;
            public WVR_HandGestureData_t gestureData;
            private static ulong m_GestureValue = 0;

            public static GestureActivator Default
            {
                get
                {
                    return new GestureActivator()
                    {
                        activator = new FeatureActivator("HandGesture", GenerateStarter(), GenerateInitializer(), Interop.WVR_StopHandGesture),
                    };
                }
            }

            private static Func<WVR_Result> GenerateStarter()
            {
                return () =>
                {
                    m_GestureValue |= 1 << (int)WVR_HandGestureType.WVR_HandGestureType_Fist;
                    m_GestureValue |= 1 << (int)WVR_HandGestureType.WVR_HandGestureType_Five;
                    m_GestureValue |= 1 << (int)WVR_HandGestureType.WVR_HandGestureType_OK;
                    m_GestureValue |= 1 << (int)WVR_HandGestureType.WVR_HandGestureType_ThumbUp;
                    m_GestureValue |= 1 << (int)WVR_HandGestureType.WVR_HandGestureType_IndexUp;
                    m_GestureValue |= 1 << (int)WVR_HandGestureType.WVR_HandGestureType_Inverse;
                    return Interop.WVR_StartHandGesture(m_GestureValue);
                };
            }

            private static Func<WVR_Result> GenerateInitializer()
            {
                return () =>
                {
                    return default(WVR_Result); // TODO
                };
            }

            public void SetActive(bool value) { activator.SetActive(value); }

            public bool TryFetchData()
            {
                if (activator.isActive)
                {
                    var result = Interop.WVR_GetHandGestureData(ref gestureData);
                    if (result == WVR_Result.WVR_Success) { return true; }

                    gestureData.left = WVR_HandGestureType.WVR_HandGestureType_Invalid;
                    gestureData.right = WVR_HandGestureType.WVR_HandGestureType_Invalid;
                    Debug.LogError(log_prefix + "WVR_GetHandGestureData fail. error:" + result);
                }

                return false;
            }

            public void UpdateGestureInput(IVRModuleDeviceStateRW state, bool isLeft)
            {
                var gesture = isLeft ? gestureData.left : gestureData.right;
                state.SetButtonPress(VRModuleRawButton.GestureFist, gesture == WVR_HandGestureType.WVR_HandGestureType_Fist);
                state.SetButtonPress(VRModuleRawButton.GestureFive, gesture == WVR_HandGestureType.WVR_HandGestureType_Five);
                state.SetButtonPress(VRModuleRawButton.GestureIndexUp, gesture == WVR_HandGestureType.WVR_HandGestureType_IndexUp);
                state.SetButtonPress(VRModuleRawButton.GestureOk, gesture == WVR_HandGestureType.WVR_HandGestureType_OK);
                state.SetButtonPress(VRModuleRawButton.GestureThumbUp, gesture == WVR_HandGestureType.WVR_HandGestureType_ThumbUp);
                state.SetButtonPress(VRModuleRawButton.System, gesture == WVR_HandGestureType.WVR_HandGestureType_Inverse);
                state.SetButtonTouch(VRModuleRawButton.GestureFist, gesture == WVR_HandGestureType.WVR_HandGestureType_Fist);
                state.SetButtonTouch(VRModuleRawButton.GestureFive, gesture == WVR_HandGestureType.WVR_HandGestureType_Five);
                state.SetButtonTouch(VRModuleRawButton.GestureIndexUp, gesture == WVR_HandGestureType.WVR_HandGestureType_IndexUp);
                state.SetButtonTouch(VRModuleRawButton.GestureOk, gesture == WVR_HandGestureType.WVR_HandGestureType_OK);
                state.SetButtonTouch(VRModuleRawButton.GestureThumbUp, gesture == WVR_HandGestureType.WVR_HandGestureType_ThumbUp);
                state.SetButtonTouch(VRModuleRawButton.System, gesture == WVR_HandGestureType.WVR_HandGestureType_Inverse);
            }

            public bool isLeftValid { get { return gestureData.left != WVR_HandGestureType.WVR_HandGestureType_Invalid; } }

            public bool isRightValid { get { return gestureData.right != WVR_HandGestureType.WVR_HandGestureType_Invalid; } }
        }
#endif
    }
}