//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
#if VIU_VIVE_HANDTRACKING
using ViveHandTracking;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class ViveHandTrackingSubmodule : VRModule.SubModuleBase
    {
#if VIU_VIVE_HANDTRACKING
        private struct HandResultData
        {
            public bool isConnected;
            public Vector3[] joints;
            public GestureType gesture;
            public float confidence;
            public float pinchLevel;
        }

        private const int RETRY_COUNT = 5;
        private const float RETRY_INTERVAL = 1f;
        private static readonly int sizeofGestureResult = Marshal.SizeOf(typeof(GestureResult));

        private GestureOption option = new GestureOption();
        private GestureProvider provider;
        private bool isStarted;
        private int retryCount;
        private float nextRetryTime;

        private int lastResultFrame;
        private HandResultData leftResult;
        private HandResultData rightResult;
        private uint leftDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint rightDeviceIndex = VRModule.INVALID_DEVICE_INDEX;

        public override bool ShouldActiveModule() { return true; }

        protected override void OnActivated()
        {
            retryCount = RETRY_COUNT;
            nextRetryTime = 0f;
            GestureInterface.UseExternalTransform(false);
        }

        protected override void OnDeactivated()
        {
            GestureInterface.StopGestureDetection();
        }

        protected override void OnUpdateDeviceConnectionAndPoses()
        {
            // try start engine detection
            if (!isStarted)
            {
                var now = Time.unscaledTime;
                if (now >= nextRetryTime && retryCount >= 0)
                {
                    --retryCount;
                    nextRetryTime = now + RETRY_INTERVAL;

                    var error = GestureInterface.StartGestureDetection(option);
                    switch (error)
                    {
                        case GestureFailure.None:
                            retryCount = RETRY_COUNT;
                            lastResultFrame = -1;
                            isStarted = true;
                            Debug.LogError("[ViveHandTrackingSubmodule] StartGestureDetection");
                            break;
                        case GestureFailure.Camera:
                            --retryCount;
                            nextRetryTime = now + RETRY_INTERVAL;
                            if (retryCount >= 0)
                            {
                                Debug.LogError("[ViveHandTrackingSubmodule] StartGestureDetection fail. Front camera function not found. retrying...");
                            }
                            else
                            {
                                Debug.LogError("[ViveHandTrackingSubmodule] StartGestureDetection fail. Front camera function not found.");
                            }
                            break;
                        default:
                            retryCount = 0;
                            Debug.LogError("[ViveHandTrackingSubmodule] StartGestureDetection fail. Front camera function not found. error:" + error);
                            break;

                    }
                }
            }

            if (!isStarted) { return; }

            // fetch raw data from engine
            IntPtr resultPtr;
            int resultFrame;
            var resultSize = GestureInterface.GetGestureResult(out resultPtr, out resultFrame);

            if (resultFrame < 0)
            {
                Debug.LogError("[ViveHandTrackingSubmodule] Detection stopped!");
                isStarted = false;
                return;
            }
            else if (resultFrame < lastResultFrame)
            {
                // skip frame
                return;
            }

            lastResultFrame = resultFrame;

            leftResult.isConnected = false;
            rightResult.isConnected = false;

            for (int i = 0, imax = resultSize; i < imax; ++i)
            {
                var result = (GestureResult)Marshal.PtrToStructure(resultPtr, typeof(GestureResult));
                IntPtr.Add(resultPtr, sizeofGestureResult);
                if (result.isLeft)
                {
                    leftResult = new HandResultData()
                    {
                        isConnected = true,
                        joints = result.points,
                        gesture = result.gesture,
                        confidence = result.confidence,
                        pinchLevel = result.pinchLevel,
                    };
                }
                else
                {
                    rightResult = new HandResultData()
                    {
                        isConnected = true,
                        joints = result.points,
                        gesture = result.gesture,
                        confidence = result.confidence,
                        pinchLevel = result.pinchLevel,
                    };
                }
            }

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            // update connection/pose for left hand devices
            if (leftResult.isConnected)
            {
                if (leftDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(leftDeviceIndex, out prevState, out currState);
                }
                else
                {
                    leftDeviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.serialNumber = "ViveHandTrackingLeft";
                    currState.modelNumber = "ViveHandTrackingLeft";
                    currState.renderModelName = "ViveHandTrackingLeft";

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.deviceModel = VRModuleDeviceModel.ViveHandTrackingTrackedHandLeft;
                    currState.input2DType = VRModuleInput2DType.None;
                }

                UpdateDeviceJoints(currState, leftResult.joints, true);

                currState.isConnected = true;
                currState.isPoseValid = true;
                currState.pose = currState.joints[HandJointIndex.Wrist].pose;
            }
            else
            {
                // FIXME: device connected state already reset by main module?
                leftDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
            }

            if (rightResult.isConnected)
            {
                if (rightDeviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(rightDeviceIndex, out prevState, out currState);
                }
                else
                {
                    rightDeviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.serialNumber = "ViveHandTrackingRight";
                    currState.modelNumber = "ViveHandTrackingRight";
                    currState.renderModelName = "ViveHandTrackingRight";

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.deviceModel = VRModuleDeviceModel.ViveHandTrackingTrackedHandRight;
                    currState.input2DType = VRModuleInput2DType.None;
                }

                UpdateDeviceJoints(currState, rightResult.joints, false);

                currState.isConnected = true;
                currState.isPoseValid = true;
                currState.pose = currState.joints[HandJointIndex.Wrist].pose;
            }
            else
            {
                // FIXME: device connected state already reset by main module?
                rightDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
            }
        }

        protected override void OnUpdateDeviceInput()
        {
            if (!isStarted) { return; }

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            if (leftResult.isConnected)
            {
                EnsureValidDeviceState(leftDeviceIndex, out prevState, out currState);

                currState.SetButtonPress(VRModuleRawButton.GestureIndexPinch, leftResult.pinchLevel >= 0.95f);
                currState.SetButtonTouch(VRModuleRawButton.GestureIndexPinch, leftResult.pinchLevel >= 0.90f);
                currState.SetButtonPress(VRModuleRawButton.GestureFist, leftResult.gesture == GestureType.Fist);
                currState.SetButtonTouch(VRModuleRawButton.GestureFist, leftResult.gesture == GestureType.Fist);
                currState.SetButtonPress(VRModuleRawButton.GestureFive, leftResult.gesture == GestureType.Five);
                currState.SetButtonTouch(VRModuleRawButton.GestureFive, leftResult.gesture == GestureType.Five);
                currState.SetButtonPress(VRModuleRawButton.GestureOk, leftResult.gesture == GestureType.OK);
                currState.SetButtonTouch(VRModuleRawButton.GestureOk, leftResult.gesture == GestureType.OK);
                currState.SetButtonPress(VRModuleRawButton.GestureThumbUp, leftResult.gesture == GestureType.Like);
                currState.SetButtonTouch(VRModuleRawButton.GestureThumbUp, leftResult.gesture == GestureType.Like);
                currState.SetButtonPress(VRModuleRawButton.GestureIndexUp, leftResult.gesture == GestureType.Point);
                currState.SetButtonTouch(VRModuleRawButton.GestureIndexUp, leftResult.gesture == GestureType.Point);
                currState.SetAxisValue(VRModuleRawAxis.Trigger, leftResult.pinchLevel);
            }

            if (rightResult.isConnected)
            {
                EnsureValidDeviceState(rightDeviceIndex, out prevState, out currState);

                currState.SetButtonPress(VRModuleRawButton.GestureIndexPinch, rightResult.pinchLevel >= 0.95f);
                currState.SetButtonTouch(VRModuleRawButton.GestureIndexPinch, rightResult.pinchLevel >= 0.90f);
                currState.SetButtonPress(VRModuleRawButton.GestureFist, rightResult.gesture == GestureType.Fist);
                currState.SetButtonTouch(VRModuleRawButton.GestureFist, rightResult.gesture == GestureType.Fist);
                currState.SetButtonPress(VRModuleRawButton.GestureFive, rightResult.gesture == GestureType.Five);
                currState.SetButtonTouch(VRModuleRawButton.GestureFive, rightResult.gesture == GestureType.Five);
                currState.SetButtonPress(VRModuleRawButton.GestureOk, rightResult.gesture == GestureType.OK);
                currState.SetButtonTouch(VRModuleRawButton.GestureOk, rightResult.gesture == GestureType.OK);
                currState.SetButtonPress(VRModuleRawButton.GestureThumbUp, rightResult.gesture == GestureType.Like);
                currState.SetButtonTouch(VRModuleRawButton.GestureThumbUp, rightResult.gesture == GestureType.Like);
                currState.SetButtonPress(VRModuleRawButton.GestureIndexUp, rightResult.gesture == GestureType.Point);
                currState.SetButtonTouch(VRModuleRawButton.GestureIndexUp, rightResult.gesture == GestureType.Point);
                currState.SetAxisValue(VRModuleRawAxis.Trigger, rightResult.pinchLevel);
            }
        }


        private static void UpdateDeviceJoints(IVRModuleDeviceStateRW state, Vector3[] rawJoints, bool isLeft)
        {
            var wrist2index = rawJoints[5] - rawJoints[0];
            var wrist2middle = rawJoints[9] - rawJoints[0];
            var wristUp = isLeft ? Vector3.Cross(wrist2index, wrist2middle) : Vector3.Cross(wrist2middle, wrist2index);
            var wristRot = Quaternion.LookRotation(wrist2middle, wristUp);
            state.joints[HandJointIndex.Wrist] = new JointPose(rawJoints[0], wristRot);
            var palmPos = (rawJoints[0] + rawJoints[9]) * 0.5f;
            state.joints[HandJointIndex.Palm] = new JointPose(palmPos, wristRot);

            Quaternion linkedJointRot;
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 0, 1, 2);
            state.joints[HandJointIndex.ThumbMetacarpal] = new JointPose(rawJoints[1], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 1, 2, 3);
            state.joints[HandJointIndex.ThumbProximal] = new JointPose(rawJoints[2], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 2, 3, 4);
            state.joints[HandJointIndex.ThumbDistal] = new JointPose(rawJoints[3], linkedJointRot);
            state.joints[HandJointIndex.ThumbTip] = new JointPose(rawJoints[4], linkedJointRot);

            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 0, 5, 6);
            state.joints[HandJointIndex.IndexProximal] = new JointPose(rawJoints[5], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 5, 6, 7);
            state.joints[HandJointIndex.IndexIntermediate] = new JointPose(rawJoints[6], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 6, 7, 8);
            state.joints[HandJointIndex.IndexDistal] = new JointPose(rawJoints[7], linkedJointRot);
            state.joints[HandJointIndex.IndexTip] = new JointPose(rawJoints[8], linkedJointRot);

            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 0, 9, 10);
            state.joints[HandJointIndex.MiddleProximal] = new JointPose(rawJoints[9], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 9, 10, 11);
            state.joints[HandJointIndex.MiddleIntermediate] = new JointPose(rawJoints[10], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 10, 11, 12);
            state.joints[HandJointIndex.MiddleDistal] = new JointPose(rawJoints[11], linkedJointRot);
            state.joints[HandJointIndex.MiddleTip] = new JointPose(rawJoints[12], linkedJointRot);

            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 0, 13, 14);
            state.joints[HandJointIndex.RingProximal] = new JointPose(rawJoints[13], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 13, 14, 15);
            state.joints[HandJointIndex.RingIntermediate] = new JointPose(rawJoints[14], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 14, 15, 16);
            state.joints[HandJointIndex.RingDistal] = new JointPose(rawJoints[15], linkedJointRot);
            state.joints[HandJointIndex.RingTip] = new JointPose(rawJoints[16], linkedJointRot);

            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 0, 17, 18);
            state.joints[HandJointIndex.RingProximal] = new JointPose(rawJoints[17], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 17, 18, 19);
            state.joints[HandJointIndex.RingIntermediate] = new JointPose(rawJoints[18], linkedJointRot);
            linkedJointRot = CalculateLinkedJointRotation(rawJoints, 18, 19, 20);
            state.joints[HandJointIndex.RingDistal] = new JointPose(rawJoints[19], linkedJointRot);
            state.joints[HandJointIndex.RingTip] = new JointPose(rawJoints[20], linkedJointRot);
        }

        private static Quaternion CalculateLinkedJointRotation(Vector3[] joints, int back, int mid, int front)
        {
            var f2m = joints[front] - joints[mid];
            var m2b = joints[front] - joints[mid];
            var right = Vector3.Cross(f2m, m2b);
            return Quaternion.LookRotation(f2m, Vector3.Cross(right, f2m));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GestureResult
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool isLeft;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
            public Vector3[] points;
            public GestureType gesture;
            public float confidence;
            public float pinchLevel;
        }

        private static class GestureInterface
        {
#if (VIVEHANDTRACKING_WITH_WAVEVR || VIVEHANDTRACKING_WAVEXR_HAND) && UNITY_ANDROID && !UNITY_EDITOR
            private const string DLLPath = "aristo_interface_wavevr";
#else
            private const string DLLPath = "aristo_interface";
#endif

            [DllImport(DLLPath)]
            internal static extern GestureFailure StartGestureDetection([In, Out] GestureOption option);

            [DllImport(DLLPath)]
            internal static extern void StopGestureDetection();

            [DllImport(DLLPath)]
            internal static extern int GetGestureResult(out IntPtr points, out int frameIndex);

            [DllImport(DLLPath)]
            internal static extern void UseExternalTransform([MarshalAs(UnmanagedType.I1)] bool value);

            [DllImport(DLLPath)]
            internal static extern void SetCameraTransform(Vector3 position, Quaternion rotation);
        }
#endif
    }
}