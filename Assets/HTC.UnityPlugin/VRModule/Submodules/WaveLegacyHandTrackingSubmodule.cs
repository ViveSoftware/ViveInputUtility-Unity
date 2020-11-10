//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using System;
using System.Collections;
using HTC.UnityPlugin.LiteCoroutineSystem;

#if VIU_WAVEVR_LEGACY_HAND_TRACKING
using Wave.Native;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class WaveLegacyHandTrackingSubmodule : VRModule.SubmoduleBase
    {
#if VIU_WAVEVR_LEGACY_HAND_TRACKING
        private static readonly string log_prefix = "[" + typeof(WaveLegacyHandTrackingSubmodule).Name + "] ";

        private struct DeviceFeature
        {
            private ulong featuresField;

            public bool supportedTracking
            {
                get { return (featuresField & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_HandTracking) > 0ul; }
            }

            public bool supportedGesture
            {
                get { return (featuresField & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_HandGesture) > 0ul; }
            }

            public void Fetch()
            {
                featuresField = Interop.WVR_GetSupportedFeatures();
            }
        }

        private DeviceFeature deviceFeature;
        private TrackingActivator trackingActivator = TrackingActivator.Default;
        private GestureActivator gestureActivity = GestureActivator.Default;
        private uint leftDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint rightDeviceIndex = VRModule.INVALID_DEVICE_INDEX;

        public override bool ShouldActiveModule() { return VRModuleSettings.activateWaveLegacyHandTrackingSubmodule; }

        protected override void OnActivated()
        {
            deviceFeature.Fetch();
        }

        protected override void OnDeactivated()
        {
            //GestureInterface.StopGestureDetection();
        }

        protected override void OnUpdateDeviceConnectionAndPoses()
        {
            trackingActivator.SetActive(VRModuleSettings.enableWaveLegacyHandTracking);

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
            if (trackingActivator.isLeftValid || gestureActivity.isRightValid)
            {
                if (leftDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(leftDeviceIndex, out prevState, out currState);
                }
                else
                {
                    leftDeviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.serialNumber = "WaveLegacyTrackedHandLeft";
                    currState.modelNumber = "WaveLegacyTrackedHandLeft";
                    currState.renderModelName = "WaveLegacyTrackedHandLeft";

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.deviceModel = VRModuleDeviceModel.WaveLegacyTrackedHandLeft;
                    currState.input2DType = VRModuleInput2DType.None;
                }

                currState.isConnected = true;
                //UpdateDeviceJoints(currState, leftResult.joints, true);
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
        }

        protected override void OnUpdateDeviceInput()
        {
            gestureActivity.SetActive(VRModuleSettings.enableWaveLegacyHandGesture);
        }

        public override uint GetLeftHandedIndex() { return leftDeviceIndex; }

        public override uint GetRightHandedIndex() { return rightDeviceIndex; }

        private enum FeatureActivity
        {
            Stopped,
            StartFailed,
            Starting,
            Started,
        }

        private struct FeatureActivator
        {
            private string featureName;
            private FeatureActivity activity;
            private bool shouldActive;
            private LiteCoroutine coroutineHandle;
            private Func<WVR_Result> starter;
            private Action stopper;

            public FeatureActivator(string featureName, Func<WVR_Result> starter, Action stopper)
            {
                this.featureName = featureName;
                this.activity = FeatureActivity.Stopped;
                this.shouldActive = false;
                this.coroutineHandle = null;
                this.starter = starter;
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
                yield return LiteTask.ToForground;

                const long retryInterval = 1000L;
                var nextRestartTime = default(DateTime);
                var startResult = default(WVR_Result);
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

                    startResult = starter();

                    yield return LiteTask.ToForground;

                    switch (startResult)
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
                            Debug.LogError(log_prefix + "Start " + featureName + " error:" + startResult);
                            activity = FeatureActivity.StartFailed;
                            break;
                    }
                }
            }
        }

        private struct TrackingActivator
        {
            private FeatureActivator activator;
            private WVR_HandSkeletonData_t skeletenData;
            private WVR_HandPoseData_t pintchData;

            public static TrackingActivator Default
            {
                get
                {
                    return new TrackingActivator()
                    {
                        activator = new FeatureActivator("HandTracking", Interop.WVR_StartHandTracking, Interop.WVR_StopHandTracking),
                    };
                }
            }

            public void SetActive(bool value) { activator.SetActive(value); }

            public bool TryFetchData(WVR_PoseOriginModel originModel)
            {
                if (activator.isActive)
                {
                    var result = Interop.WVR_GetHandTrackingData(ref skeletenData, ref pintchData, originModel);
                    if (result == WVR_Result.WVR_Success) { return true; }

                    skeletenData.left.wrist.IsValidPose = false;
                    skeletenData.right.wrist.IsValidPose = false;
                    pintchData.left.state.type = WVR_HandPoseType.WVR_HandPoseType_Invalid;
                    pintchData.right.state.type = WVR_HandPoseType.WVR_HandPoseType_Invalid;
                    Debug.LogError(log_prefix + "WVR_GetHandTrackingData fail. error:" + result);
                }

                return false;
            }

            public bool isLeftValid { get { return skeletenData.left.wrist.IsValidPose; } }

            public bool isRightValid { get { return skeletenData.right.wrist.IsValidPose; } }

            private void UpdateJoints(IVRModuleDeviceStateRW state, ref WVR_HandSkeletonState_t data, bool isLeft)
            {
                //state.isPoseValid = data.wrist.IsValidPose;
                //var wvrPose = new RigidTransform(data.wrist.PoseMatrix);
                //state.position = wvrPose.pos;
                //state.rotation = wvrPose.rot;
                //state.velocity = Coordinate.GetVectorFromGL(data.wrist.Velocity); // FIXME: need GL tranform?
                //state.angularVelocity = Coordinate.GetVectorFromGL(data.wrist.AngularVelocity); // FIXME: need GL tranform?

                //state.joints[HandJointIndex.ThumbMetacarpal] = Coordinate.GetVectorFromGL(data.thumb.joint1);

                //state.joints[HandJointIndex.Wrist] = new JointPose(new RigidPose(wvrPose.pos, wvrPose.rot));
                //state.joints[HandJointIndex.Palm] = new JointPose(new RigidPose(    ));
            }

            public void UpdateLeftJoints(IVRModuleDeviceStateRW state)
            {
                //UpdateJoints(state, ref skeletenData.left);

                //var hmdPose = VRModule.GetDeviceState(VRModule.HMD_DEVICE_INDEX).pose;
                //var roomSpaceCamPose = hmdPose;
                //var camSpaceWrist2index = rawJoints[5] - rawJoints[0];
                //var camSpaceWrist2middle = rawJoints[9] - rawJoints[0];
                //var camSpaceWrist2pinky = rawJoints[17] - rawJoints[0];
                //var camSpaceWristUp = isLeft ? Vector3.Cross(camSpaceWrist2pinky, camSpaceWrist2index) : Vector3.Cross(camSpaceWrist2index, camSpaceWrist2pinky);
                //var camSpaceWristPose = new RigidPose(rawJoints[0], Quaternion.LookRotation(camSpaceWrist2middle, camSpaceWristUp));
                //var roomSpaceWristPose = roomSpaceCamPose * camSpaceWristPose;

                //state.pose = roomSpaceWristPose;
                //state.joints[HandJointIndex.Wrist] = new JointPose(roomSpaceWristPose);
                //state.joints[HandJointIndex.Palm] = new JointPose(roomSpaceCamPose * new RigidPose((rawJoints[0] + rawJoints[9]) * 0.5f, camSpaceWristPose.rot));

                //var camSpaceThumbRight = camSpaceWristPose.rot * (isLeft ? new Vector3(-3f, -8f, -5f) : new Vector3(-3f, 8f, 5f));
                //var camSpaceFingerRight = camSpaceWristPose.rot * Vector3.right;
                //Quaternion camSpaceRot;
                //camSpaceRot = CalculateJointRot(rawJoints, 1, 2, camSpaceThumbRight);
                //state.joints[HandJointIndex.ThumbMetacarpal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[1], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 2, 3, camSpaceThumbRight);
                //state.joints[HandJointIndex.ThumbProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[2], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 3, 4, camSpaceThumbRight);
                //state.joints[HandJointIndex.ThumbDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[3], camSpaceRot));
                //state.joints[HandJointIndex.ThumbTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[4], camSpaceRot));

                //camSpaceRot = CalculateJointRot(rawJoints, 5, 6, camSpaceFingerRight);
                //state.joints[HandJointIndex.IndexProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[5], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 6, 7, camSpaceFingerRight);
                //state.joints[HandJointIndex.IndexIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[6], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 7, 8, camSpaceFingerRight);
                //state.joints[HandJointIndex.IndexDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[7], camSpaceRot));
                //state.joints[HandJointIndex.IndexTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[8], camSpaceRot));

                //camSpaceRot = CalculateJointRot(rawJoints, 9, 10, camSpaceFingerRight);
                //state.joints[HandJointIndex.MiddleProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[9], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 10, 11, camSpaceFingerRight);
                //state.joints[HandJointIndex.MiddleIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[10], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 11, 12, camSpaceFingerRight);
                //state.joints[HandJointIndex.MiddleDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[11], camSpaceRot));
                //state.joints[HandJointIndex.MiddleTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[12], camSpaceRot));

                //camSpaceRot = CalculateJointRot(rawJoints, 13, 14, camSpaceFingerRight);
                //state.joints[HandJointIndex.RingProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[13], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 14, 15, camSpaceFingerRight);
                //state.joints[HandJointIndex.RingIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[14], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 15, 16, camSpaceFingerRight);
                //state.joints[HandJointIndex.RingDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[15], camSpaceRot));
                //state.joints[HandJointIndex.RingTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[16], camSpaceRot));

                //camSpaceRot = CalculateJointRot(rawJoints, 17, 18, camSpaceFingerRight);
                //state.joints[HandJointIndex.PinkyProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[17], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 18, 19, camSpaceFingerRight);
                //state.joints[HandJointIndex.PinkyIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[18], camSpaceRot));
                //camSpaceRot = CalculateJointRot(rawJoints, 19, 20, camSpaceFingerRight);
                //state.joints[HandJointIndex.PinkyDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[19], camSpaceRot));
                //state.joints[HandJointIndex.PinkyTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[20], camSpaceRot));
            }
        }

        private struct GestureActivator
        {
            private FeatureActivator activator;
            private WVR_HandGestureData_t gestureData;

            public static GestureActivator Default
            {
                get
                {
                    return new GestureActivator()
                    {
                        activator = new FeatureActivator("HandGesture", Interop.WVR_StartHandGesture, Interop.WVR_StopHandGesture),
                    };
                }
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

            public bool isLeftValid { get { return gestureData.left != WVR_HandGestureType.WVR_HandGestureType_Invalid; } }

            public bool isRightValid { get { return gestureData.right != WVR_HandGestureType.WVR_HandGestureType_Invalid; } }
        }
#endif
    }
}
