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
    public class ViveHandTrackingSubmodule : VRModule.SubmoduleBase
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
            GestureInterface.UseExternalTransform(true);
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
                            Debug.Log("[ViveHandTrackingSubmodule] StartGestureDetection");
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
                Debug.Log("[ViveHandTrackingSubmodule] Detection stopped!");
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

                currState.isConnected = true;
                currState.isPoseValid = true;
                UpdateDeviceJoints(currState, leftResult.joints, true);
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

                currState.isConnected = true;
                currState.isPoseValid = true;
                UpdateDeviceJoints(currState, rightResult.joints, false);
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
            var hmdPose = VRModule.GetDeviceState(VRModule.HMD_DEVICE_INDEX).pose;
            var wrist2index = rawJoints[5] - rawJoints[0];
            var wrist2middle = rawJoints[9] - rawJoints[0];
            var wristUp = isLeft ? Vector3.Cross(wrist2middle, wrist2index) : Vector3.Cross(wrist2index, wrist2middle);
            var wristRot = Quaternion.LookRotation(wrist2middle, wristUp);
            var wristPose = new RigidPose(rawJoints[0], wristRot);
            var wristInverse = wristPose.GetInverse();
            var wristRight = wristRot * Vector3.right;

            state.pose = hmdPose * wristPose;

            state.joints[HandJointIndex.Wrist] = new JointPose(Vector3.zero, Quaternion.identity);
            var palmPose = new RigidPose((rawJoints[0] + rawJoints[9]) * 0.5f, Quaternion.identity);
            state.joints[HandJointIndex.Palm] = new JointPose(wristInverse * palmPose);

            Quaternion linkedJointRot;
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 0, 1, wristRight) * Quaternion.Euler(0f, 0f, isLeft ? 50f : -50f);
            state.joints[HandJointIndex.ThumbMetacarpal] = new JointPose(wristInverse * new RigidPose(rawJoints[1], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 1, 2, wristRight);
            state.joints[HandJointIndex.ThumbProximal] = new JointPose(wristInverse * new RigidPose(rawJoints[2], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 2, 3, wristRight);
            state.joints[HandJointIndex.ThumbDistal] = new JointPose(wristInverse * new RigidPose(rawJoints[3], linkedJointRot));
            state.joints[HandJointIndex.ThumbTip] = new JointPose(wristInverse * new RigidPose(rawJoints[4], linkedJointRot));

            linkedJointRot = CalculateFingerJointRotation(rawJoints, 5, 6, wristRight);
            state.joints[HandJointIndex.IndexProximal] = new JointPose(wristInverse * new RigidPose(rawJoints[5], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 6, 7, wristRight);
            state.joints[HandJointIndex.IndexIntermediate] = new JointPose(wristInverse * new RigidPose(rawJoints[6], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 7, 8, wristRight);
            state.joints[HandJointIndex.IndexDistal] = new JointPose(wristInverse * new RigidPose(rawJoints[7], linkedJointRot));
            state.joints[HandJointIndex.IndexTip] = new JointPose(wristInverse * new RigidPose(rawJoints[8], linkedJointRot));

            linkedJointRot = CalculateFingerJointRotation(rawJoints, 9, 10, wristRight);
            state.joints[HandJointIndex.MiddleProximal] = new JointPose(wristInverse * new RigidPose(rawJoints[9], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 10, 11, wristRight);
            state.joints[HandJointIndex.MiddleIntermediate] = new JointPose(wristInverse * new RigidPose(rawJoints[10], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 11, 12, wristRight);
            state.joints[HandJointIndex.MiddleDistal] = new JointPose(wristInverse * new RigidPose(rawJoints[11], linkedJointRot));
            state.joints[HandJointIndex.MiddleTip] = new JointPose(wristInverse * new RigidPose(rawJoints[12], linkedJointRot));

            linkedJointRot = CalculateFingerJointRotation(rawJoints, 13, 14, wristRight);
            state.joints[HandJointIndex.RingProximal] = new JointPose(wristInverse * new RigidPose(rawJoints[13], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 14, 15, wristRight);
            state.joints[HandJointIndex.RingIntermediate] = new JointPose(wristInverse * new RigidPose(rawJoints[14], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 15, 16, wristRight);
            state.joints[HandJointIndex.RingDistal] = new JointPose(wristInverse * new RigidPose(rawJoints[15], linkedJointRot));
            state.joints[HandJointIndex.RingTip] = new JointPose(wristInverse * new RigidPose(rawJoints[16], linkedJointRot));

            linkedJointRot = CalculateFingerJointRotation(rawJoints, 17, 18, wristRight);
            state.joints[HandJointIndex.PinkyProximal] = new JointPose(wristInverse * new RigidPose(rawJoints[17], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 18, 19, wristRight);
            state.joints[HandJointIndex.PinkyIntermediate] = new JointPose(wristInverse * new RigidPose(rawJoints[18], linkedJointRot));
            linkedJointRot = CalculateFingerJointRotation(rawJoints, 19, 20, wristRight);
            state.joints[HandJointIndex.PinkyDistal] = new JointPose(wristInverse * new RigidPose(rawJoints[19], linkedJointRot));
            state.joints[HandJointIndex.PinkyTip] = new JointPose(wristInverse * new RigidPose(rawJoints[20], linkedJointRot));
        }

        private static Quaternion CalculateFingerJointRotation(Vector3[] joints, int i, int iChild, Vector3 right)
        {
            var forward = joints[iChild] - joints[i];
            return Quaternion.LookRotation(forward, Vector3.Cross(forward, right));
        }

        private static Quaternion CalculateLinkedJointRotation(Vector3[] joints, int back, int mid, int front)
        {
            var m2f = joints[front] - joints[mid];
            var b2m = joints[mid] - joints[back];
            var right = Vector3.Cross(b2m, m2f);
            return Quaternion.LookRotation(m2f, Vector3.Cross(m2f, right));
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