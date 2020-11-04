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
                resultPtr = IntPtr.Add(resultPtr, sizeofGestureResult);
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
            var roomSpaceCamPose = hmdPose;
            var camSpaceWrist2index = rawJoints[5] - rawJoints[0];
            var camSpaceWrist2middle = rawJoints[9] - rawJoints[0];
            var camSpaceWrist2pinky = rawJoints[17] - rawJoints[0];
            var camSpaceWristUp = isLeft ? Vector3.Cross(camSpaceWrist2pinky, camSpaceWrist2index) : Vector3.Cross(camSpaceWrist2index, camSpaceWrist2pinky);
            var camSpaceWristPose = new RigidPose(rawJoints[0], Quaternion.LookRotation(camSpaceWrist2middle, camSpaceWristUp));
            var roomSpaceWristPose = roomSpaceCamPose * camSpaceWristPose;

            state.pose = roomSpaceWristPose;
            state.joints[HandJointIndex.Wrist] = new JointPose(roomSpaceWristPose);
            state.joints[HandJointIndex.Palm] = new JointPose(roomSpaceCamPose * new RigidPose((rawJoints[0] + rawJoints[9]) * 0.5f, camSpaceWristPose.rot));

            var camSpaceThumbRight = camSpaceWristPose.rot * (isLeft ? new Vector3(-3f, -8f, -5f) : new Vector3(-3f, 8f, 5f));
            var camSpaceFingerRight = camSpaceWristPose.rot * Vector3.right;
            Quaternion camSpaceRot;
            camSpaceRot = CalculateJointRot(rawJoints, 1, 2, camSpaceThumbRight);
            state.joints[HandJointIndex.ThumbMetacarpal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[1], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 2, 3, camSpaceThumbRight);
            state.joints[HandJointIndex.ThumbProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[2], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 3, 4, camSpaceThumbRight);
            state.joints[HandJointIndex.ThumbDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[3], camSpaceRot));
            state.joints[HandJointIndex.ThumbTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[4], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 5, 6, camSpaceFingerRight);
            state.joints[HandJointIndex.IndexProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[5], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 6, 7, camSpaceFingerRight);
            state.joints[HandJointIndex.IndexIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[6], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 7, 8, camSpaceFingerRight);
            state.joints[HandJointIndex.IndexDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[7], camSpaceRot));
            state.joints[HandJointIndex.IndexTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[8], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 9, 10, camSpaceFingerRight);
            state.joints[HandJointIndex.MiddleProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[9], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 10, 11, camSpaceFingerRight);
            state.joints[HandJointIndex.MiddleIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[10], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 11, 12, camSpaceFingerRight);
            state.joints[HandJointIndex.MiddleDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[11], camSpaceRot));
            state.joints[HandJointIndex.MiddleTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[12], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 13, 14, camSpaceFingerRight);
            state.joints[HandJointIndex.RingProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[13], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 14, 15, camSpaceFingerRight);
            state.joints[HandJointIndex.RingIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[14], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 15, 16, camSpaceFingerRight);
            state.joints[HandJointIndex.RingDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[15], camSpaceRot));
            state.joints[HandJointIndex.RingTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[16], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 17, 18, camSpaceFingerRight);
            state.joints[HandJointIndex.PinkyProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[17], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 18, 19, camSpaceFingerRight);
            state.joints[HandJointIndex.PinkyIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[18], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 19, 20, camSpaceFingerRight);
            state.joints[HandJointIndex.PinkyDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[19], camSpaceRot));
            state.joints[HandJointIndex.PinkyTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[20], camSpaceRot));
        }

        private static Quaternion CalculateJointRot(Vector3[] joints, int i, int iChild, Vector3 right)
        {
            var forward = joints[iChild] - joints[i];
            return Quaternion.LookRotation(forward, Vector3.Cross(forward, right));
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