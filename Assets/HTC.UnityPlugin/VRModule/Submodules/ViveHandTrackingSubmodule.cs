﻿//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
#if VIU_VIVE_HAND_TRACKING
using ViveHandTracking;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class ViveHandTrackingSubmodule : VRModule.SubmoduleBase
    {
#if VIU_VIVE_HAND_TRACKING && UNITY_STANDALONE
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
        private bool isStarted;
        private int retryCount;
        private float nextRetryTime;

        private int lastResultFrame;
        private HandResultData leftResult;
        private HandResultData rightResult;
        private uint leftDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint rightDeviceIndex = VRModule.INVALID_DEVICE_INDEX;

        public override bool ShouldActiveModule() { return VRModule.isOpenVRSupported && VRModuleSettings.activateViveHandTrackingSubmodule; }

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

        public override uint GetRightHandedIndex() { return rightDeviceIndex; }

        public override uint GetLeftHandedIndex() { return leftDeviceIndex; }

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
                                Debug.LogWarning("[ViveHandTrackingSubmodule] StartGestureDetection fail. Front camera function not found. retrying...");
                            }
                            else
                            {
                                Debug.LogWarning("[ViveHandTrackingSubmodule] StartGestureDetection fail. Front camera function not found.");
                            }
                            break;
                        default:
                            retryCount = 0;
                            Debug.LogWarning("[ViveHandTrackingSubmodule] StartGestureDetection fail. error:" + error);
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

            UpdateDeviceConnectionAndPoses(ref leftResult, ref leftDeviceIndex, true);
            UpdateDeviceConnectionAndPoses(ref rightResult, ref rightDeviceIndex, false);
        }

        private void UpdateDeviceConnectionAndPoses(ref HandResultData resultData, ref uint index, bool isLeft)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            // update connection/pose for left hand devices
            if (resultData.isConnected)
            {
                if (index != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(index, out prevState, out currState);
                }
                else
                {
                    index = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

                    var name = isLeft ? "ViveHandTrackingLeft" : "ViveHandTrackingRight";
                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.serialNumber = name;
                    currState.modelNumber = name;
                    currState.renderModelName = name;

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.deviceModel = isLeft ? VRModuleDeviceModel.ViveHandTrackingTrackedHandLeft : VRModuleDeviceModel.ViveHandTrackingTrackedHandRight;
                    currState.input2DType = VRModuleInput2DType.None;
                }

                currState.isConnected = true;
                currState.isPoseValid = true;
                UpdateDeviceJoints(currState, resultData.joints, isLeft);
            }
            else
            {
                if (index != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(index, out prevState, out currState);
                    currState.Reset();
                    index = VRModule.INVALID_DEVICE_INDEX;
                }
            }
        }

        protected override void OnUpdateDeviceInput()
        {
            if (!isStarted) { return; }

            UpdateDeviceInput(ref leftResult, leftDeviceIndex);
            UpdateDeviceInput(ref rightResult, rightDeviceIndex);
        }

        private void UpdateDeviceInput(ref HandResultData resultData, uint index)
        {
            if (!resultData.isConnected) { return; }

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(index, out prevState, out currState);

            var pinched = resultData.pinchLevel >= 0.95f;
            var gFist = resultData.gesture == GestureType.Fist && resultData.confidence > 0.1f;
            var gFive = resultData.gesture == GestureType.Five && resultData.confidence > 0.1f;
            var gOK = resultData.gesture == GestureType.OK && resultData.confidence > 0.1f;
            var gLike = resultData.gesture == GestureType.Like && resultData.confidence > 0.1f;
            var gPoint = resultData.gesture == GestureType.Point && resultData.confidence > 0.1f;
            currState.SetButtonPress(VRModuleRawButton.GestureIndexPinch, pinched);
            currState.SetButtonTouch(VRModuleRawButton.GestureIndexPinch, pinched);
            currState.SetButtonPress(VRModuleRawButton.GestureFist, gFist);
            currState.SetButtonTouch(VRModuleRawButton.GestureFist, gFist);
            currState.SetButtonPress(VRModuleRawButton.GestureFive, gFive);
            currState.SetButtonTouch(VRModuleRawButton.GestureFive, gFive);
            currState.SetButtonPress(VRModuleRawButton.GestureOk, gOK);
            currState.SetButtonTouch(VRModuleRawButton.GestureOk, gOK);
            currState.SetButtonPress(VRModuleRawButton.GestureThumbUp, gLike);
            currState.SetButtonTouch(VRModuleRawButton.GestureThumbUp, gLike);
            currState.SetButtonPress(VRModuleRawButton.GestureIndexUp, gPoint);
            currState.SetButtonTouch(VRModuleRawButton.GestureIndexUp, gPoint);
            currState.SetButtonPress(VRModuleRawButton.Grip, gFist);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, resultData.pinchLevel);
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
            state.handJoints[HandJointName.Wrist] = new JointPose(roomSpaceWristPose);
            state.handJoints[HandJointName.Palm] = new JointPose(roomSpaceCamPose * new RigidPose((rawJoints[0] + rawJoints[9]) * 0.5f, camSpaceWristPose.rot));

            var camSpaceFingerRight = camSpaceWristPose.rot * Vector3.right;
            Quaternion camSpaceRot;
            camSpaceRot = CalculateJointRot(rawJoints, 1, 2, camSpaceWristPose.rot * (isLeft ? new Vector3(0.1f, -5.67f, -0.1f) : new Vector3(0.1f, 5.67f, 0.1f)));
            state.handJoints[HandJointName.ThumbMetacarpal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[1], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 2, 3, camSpaceWristPose.rot * (isLeft ? new Vector3(0.1f, -5.67f, -0.1f) : new Vector3(0.1f, 5.67f, 0.1f)));
            state.handJoints[HandJointName.ThumbProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[2], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 3, 4, camSpaceWristPose.rot * (isLeft ? new Vector3(1.72f, -5.67f, -3.55f) : new Vector3(1.72f, 5.67f, 3.55f)));
            state.handJoints[HandJointName.ThumbDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[3], camSpaceRot));
            state.handJoints[HandJointName.ThumbTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[4], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 5, 6, camSpaceFingerRight);
            state.handJoints[HandJointName.IndexProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[5], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 6, 7, camSpaceFingerRight);
            state.handJoints[HandJointName.IndexIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[6], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 7, 8, camSpaceFingerRight);
            state.handJoints[HandJointName.IndexDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[7], camSpaceRot));
            state.handJoints[HandJointName.IndexTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[8], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 9, 10, camSpaceFingerRight);
            state.handJoints[HandJointName.MiddleProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[9], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 10, 11, camSpaceFingerRight);
            state.handJoints[HandJointName.MiddleIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[10], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 11, 12, camSpaceFingerRight);
            state.handJoints[HandJointName.MiddleDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[11], camSpaceRot));
            state.handJoints[HandJointName.MiddleTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[12], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 13, 14, camSpaceFingerRight);
            state.handJoints[HandJointName.RingProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[13], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 14, 15, camSpaceFingerRight);
            state.handJoints[HandJointName.RingIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[14], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 15, 16, camSpaceFingerRight);
            state.handJoints[HandJointName.RingDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[15], camSpaceRot));
            state.handJoints[HandJointName.RingTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[16], camSpaceRot));

            camSpaceRot = CalculateJointRot(rawJoints, 17, 18, camSpaceFingerRight);
            state.handJoints[HandJointName.PinkyProximal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[17], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 18, 19, camSpaceFingerRight);
            state.handJoints[HandJointName.PinkyIntermediate] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[18], camSpaceRot));
            camSpaceRot = CalculateJointRot(rawJoints, 19, 20, camSpaceFingerRight);
            state.handJoints[HandJointName.PinkyDistal] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[19], camSpaceRot));
            state.handJoints[HandJointName.PinkyTip] = new JointPose(roomSpaceCamPose * new RigidPose(rawJoints[20], camSpaceRot));
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
            private const string DLLPath = "aristo_interface";

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