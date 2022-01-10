//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Collections;
using HTC.UnityPlugin.LiteCoroutineSystem;
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
            public GestureResult rawData;
        }

        private static readonly string LOG_PREFIX = "[" + typeof(ViveHandTrackingSubmodule).Name + "] ";
        private const int RETRY_COUNT = 5;
        private const float RETRY_INTERVAL = 1f;
        private static readonly int sizeofGestureResult = Marshal.SizeOf(typeof(GestureResult));

        private GestureOption option = new GestureOption();
        private bool isStarted;
        private int retryCount;
        private WaitForTicks retryInterval = WaitForTicks.Seconds(RETRY_INTERVAL);
        private LiteCoroutine startDetectionCoroutine;
        private LiteTask startDetectionTask = new LiteTask();

        private int lastResultFrame;
        private HandResultData leftResult;
        private HandResultData rightResult;
        private uint leftDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint rightDeviceIndex = VRModule.INVALID_DEVICE_INDEX;

        public override bool ShouldActiveModule() { return VRModule.isOpenVRSupported && VRModuleSettings.activateViveHandTrackingSubmodule; }

        protected override void OnActivated()
        {
            retryCount = RETRY_COUNT;
            GestureInterface.UseExternalTransform(true);
        }

        protected override void OnDeactivated()
        {
            GestureInterface.StopGestureDetection();
            startDetectionTask.Cancel();
        }

        public override uint GetRightHandedIndex() { return rightDeviceIndex; }

        public override uint GetLeftHandedIndex() { return leftDeviceIndex; }

        private IEnumerator StartDetectionCoroutine()
        {
            while (true)
            {
                var error = GestureInterface.StartGestureDetection(option);

                lock (this)
                {
                    switch (error)
                    {
                        case GestureFailure.None:
                            retryCount = RETRY_COUNT;
                            lastResultFrame = -1;
                            isStarted = true;
                            Debug.Log(LOG_PREFIX + "Detection started");
                            break;
                        case GestureFailure.Camera:
                            --retryCount;
                            if (retryCount >= 0)
                            {
                                Debug.LogWarning(LOG_PREFIX + "StartGestureDetection fail. Front camera function not found. retrying(" + (retryCount + 1) + ")...");
                            }
                            else
                            {
                                Debug.LogWarning(LOG_PREFIX + "StartGestureDetection fail. Front camera function not found.");
                            }
                            break;
                        default:
                            retryCount = 0;
                            Debug.LogWarning(LOG_PREFIX + "StartGestureDetection fail. error:" + error);
                            break;

                    }

                    if (retryCount <= 0 || isStarted) { yield break; }
                }

                yield return retryInterval;
            }
        }

        protected override void OnUpdateDeviceConnectionAndPoses()
        {
            lock (this)
            {
                if (!isStarted && retryCount > 0 && startDetectionTask.IsDone)
                {
                    // try start engine detection
                    LiteCoroutine.StartCoroutine(ref startDetectionCoroutine, startDetectionTask.RestartTask(StartDetectionCoroutine()));
                }

                if (!isStarted) { return; }
            }

            var hmdPose = VRModule.GetDeviceState(VRModule.HMD_DEVICE_INDEX).pose;
            GestureInterface.SetCameraTransform(hmdPose.pos, hmdPose.rot);

            // fetch raw data from engine
            IntPtr resultPtr;
            int resultFrame;
            var resultSize = GestureInterface.GetGestureResult(out resultPtr, out resultFrame);

            if (resultFrame < 0)
            {
                Debug.Log(LOG_PREFIX + "Detection stopped");
                isStarted = false;
                return;
            }
            else if (resultFrame <= lastResultFrame)
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
                        rawData = result,
                    };
                }
                else
                {
                    rightResult = new HandResultData()
                    {
                        isConnected = true,
                        rawData = result,
                    };
                }
#if NET_4_6
                resultPtr = IntPtr.Add(resultPtr, sizeofGestureResult);
#else
                resultPtr = new IntPtr(resultPtr.ToInt64() + sizeofGestureResult);
#endif
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
                UpdateDeviceJoints(currState, ref resultData, isLeft);
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

#if VIU_VIVE_HAND_TRACKING_0_10_0_OR_NEWER
        [StructLayout(LayoutKind.Sequential)]
        private struct GestureResult
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool isLeft;
            public Vector3 position;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
            public Vector3[] points;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
            public Quaternion[] rotations;
            public GestureType gesture;
            public float confidence;
            public PinchInfo pinch;
        }

        private void UpdateDeviceInput(ref HandResultData resultData, uint index)
        {
            if (!resultData.isConnected) { return; }

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(index, out prevState, out currState);

            var pinched = resultData.rawData.pinch.pinchLevel >= 0.95f;
            var gFist = resultData.rawData.gesture == GestureType.Fist && resultData.rawData.confidence > 0.1f;
            var gFive = resultData.rawData.gesture == GestureType.Five && resultData.rawData.confidence > 0.1f;
            var gOK = resultData.rawData.gesture == GestureType.OK && resultData.rawData.confidence > 0.1f;
            var gLike = resultData.rawData.gesture == GestureType.Like && resultData.rawData.confidence > 0.1f;
            var gPoint = resultData.rawData.gesture == GestureType.Point && resultData.rawData.confidence > 0.1f;
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
            currState.SetButtonTouch(VRModuleRawButton.Grip, gFist);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, resultData.rawData.pinch.pinchLevel);
        }

        private static readonly Quaternion rotOffset = Quaternion.Inverse(Quaternion.LookRotation(Vector3.down, Vector3.forward));
        private static void UpdateDeviceJoints(IVRModuleDeviceStateRW state, ref HandResultData resultData, bool isLeft)
        {
            var joints = resultData.rawData.points;
            var rotations = resultData.rawData.rotations;

            state.pose = new RigidPose(joints[0], rotations[0] * rotOffset);

            state.handJoints[HandJointName.Wrist] = new JointPose(new RigidPose(joints[0], rotations[0] * rotOffset));

            state.handJoints[HandJointName.ThumbMetacarpal] = new JointPose(new RigidPose(joints[1], rotations[1] * rotOffset));
            state.handJoints[HandJointName.ThumbProximal] = new JointPose(new RigidPose(joints[2], rotations[2] * rotOffset));
            state.handJoints[HandJointName.ThumbDistal] = new JointPose(new RigidPose(joints[3], rotations[3] * rotOffset));
            state.handJoints[HandJointName.ThumbTip] = new JointPose(new RigidPose(joints[4], rotations[4] * rotOffset));

            state.handJoints[HandJointName.IndexProximal] = new JointPose(new RigidPose(joints[5], rotations[5] * rotOffset));
            state.handJoints[HandJointName.IndexIntermediate] = new JointPose(new RigidPose(joints[6], rotations[6] * rotOffset));
            state.handJoints[HandJointName.IndexDistal] = new JointPose(new RigidPose(joints[7], rotations[7] * rotOffset));
            state.handJoints[HandJointName.IndexTip] = new JointPose(new RigidPose(joints[8], rotations[8] * rotOffset));

            state.handJoints[HandJointName.MiddleProximal] = new JointPose(new RigidPose(joints[9], rotations[9] * rotOffset));
            state.handJoints[HandJointName.MiddleIntermediate] = new JointPose(new RigidPose(joints[10], rotations[10] * rotOffset));
            state.handJoints[HandJointName.MiddleDistal] = new JointPose(new RigidPose(joints[11], rotations[11] * rotOffset));
            state.handJoints[HandJointName.MiddleTip] = new JointPose(new RigidPose(joints[12], rotations[12] * rotOffset));

            state.handJoints[HandJointName.RingProximal] = new JointPose(new RigidPose(joints[13], rotations[13] * rotOffset));
            state.handJoints[HandJointName.RingIntermediate] = new JointPose(new RigidPose(joints[14], rotations[14] * rotOffset));
            state.handJoints[HandJointName.RingDistal] = new JointPose(new RigidPose(joints[15], rotations[15] * rotOffset));
            state.handJoints[HandJointName.RingTip] = new JointPose(new RigidPose(joints[16], rotations[16] * rotOffset));

            state.handJoints[HandJointName.PinkyProximal] = new JointPose(new RigidPose(joints[17], rotations[17] * rotOffset));
            state.handJoints[HandJointName.PinkyIntermediate] = new JointPose(new RigidPose(joints[18], rotations[18] * rotOffset));
            state.handJoints[HandJointName.PinkyDistal] = new JointPose(new RigidPose(joints[19], rotations[19] * rotOffset));
            state.handJoints[HandJointName.PinkyTip] = new JointPose(new RigidPose(joints[20], rotations[20] * rotOffset));
        }
#else
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

        private void UpdateDeviceInput(ref HandResultData resultData, uint index)
        {
            if (!resultData.isConnected) { return; }

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(index, out prevState, out currState);

            var pinched = resultData.rawData.pinchLevel >= 0.95f;
            var gFist = resultData.rawData.gesture == GestureType.Fist && resultData.rawData.confidence > 0.1f;
            var gFive = resultData.rawData.gesture == GestureType.Five && resultData.rawData.confidence > 0.1f;
            var gOK = resultData.rawData.gesture == GestureType.OK && resultData.rawData.confidence > 0.1f;
            var gLike = resultData.rawData.gesture == GestureType.Like && resultData.rawData.confidence > 0.1f;
            var gPoint = resultData.rawData.gesture == GestureType.Point && resultData.rawData.confidence > 0.1f;
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
            currState.SetButtonTouch(VRModuleRawButton.Grip, gFist);
            currState.SetAxisValue(VRModuleRawAxis.Trigger, resultData.rawData.pinchLevel);
        }

        private static void UpdateDeviceJoints(IVRModuleDeviceStateRW state, ref HandResultData resultData, bool isLeft)
        {
            var rawJoints = resultData.rawData.points;

            var roomSpaceWrist2index = rawJoints[5] - rawJoints[0];
            var roomSpaceWrist2middle = rawJoints[9] - rawJoints[0];
            var roomSpaceWrist2pinky = rawJoints[17] - rawJoints[0];
            var roomSpaceWristUp = isLeft ? Vector3.Cross(roomSpaceWrist2pinky, roomSpaceWrist2index) : Vector3.Cross(roomSpaceWrist2index, roomSpaceWrist2pinky);
            var roomSpaceWristPose = new RigidPose(rawJoints[0], Quaternion.LookRotation(roomSpaceWrist2middle, roomSpaceWristUp));

            state.pose = roomSpaceWristPose;
            state.handJoints[HandJointName.Wrist] = new JointPose(roomSpaceWristPose);
            state.handJoints[HandJointName.Palm] = new JointPose(new RigidPose((rawJoints[0] + rawJoints[9]) * 0.5f, roomSpaceWristPose.rot));

            var camSpaceFingerRight = roomSpaceWristPose.rot * Vector3.right;
            Quaternion roomSpaceRot;
            roomSpaceRot = CalculateJointRot(rawJoints, 1, 2, roomSpaceWristPose.rot * (isLeft ? new Vector3(0.1f, -5.67f, -0.1f) : new Vector3(0.1f, 5.67f, 0.1f)));
            state.handJoints[HandJointName.ThumbMetacarpal] = new JointPose(new RigidPose(rawJoints[1], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 2, 3, roomSpaceWristPose.rot * (isLeft ? new Vector3(0.1f, -5.67f, -0.1f) : new Vector3(0.1f, 5.67f, 0.1f)));
            state.handJoints[HandJointName.ThumbProximal] = new JointPose(new RigidPose(rawJoints[2], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 3, 4, roomSpaceWristPose.rot * (isLeft ? new Vector3(1.72f, -5.67f, -3.55f) : new Vector3(1.72f, 5.67f, 3.55f)));
            state.handJoints[HandJointName.ThumbDistal] = new JointPose(new RigidPose(rawJoints[3], roomSpaceRot));
            state.handJoints[HandJointName.ThumbTip] = new JointPose(new RigidPose(rawJoints[4], roomSpaceRot));

            roomSpaceRot = CalculateJointRot(rawJoints, 5, 6, camSpaceFingerRight);
            state.handJoints[HandJointName.IndexProximal] = new JointPose(new RigidPose(rawJoints[5], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 6, 7, camSpaceFingerRight);
            state.handJoints[HandJointName.IndexIntermediate] = new JointPose(new RigidPose(rawJoints[6], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 7, 8, camSpaceFingerRight);
            state.handJoints[HandJointName.IndexDistal] = new JointPose(new RigidPose(rawJoints[7], roomSpaceRot));
            state.handJoints[HandJointName.IndexTip] = new JointPose(new RigidPose(rawJoints[8], roomSpaceRot));

            roomSpaceRot = CalculateJointRot(rawJoints, 9, 10, camSpaceFingerRight);
            state.handJoints[HandJointName.MiddleProximal] = new JointPose(new RigidPose(rawJoints[9], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 10, 11, camSpaceFingerRight);
            state.handJoints[HandJointName.MiddleIntermediate] = new JointPose(new RigidPose(rawJoints[10], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 11, 12, camSpaceFingerRight);
            state.handJoints[HandJointName.MiddleDistal] = new JointPose(new RigidPose(rawJoints[11], roomSpaceRot));
            state.handJoints[HandJointName.MiddleTip] = new JointPose(new RigidPose(rawJoints[12], roomSpaceRot));

            roomSpaceRot = CalculateJointRot(rawJoints, 13, 14, camSpaceFingerRight);
            state.handJoints[HandJointName.RingProximal] = new JointPose(new RigidPose(rawJoints[13], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 14, 15, camSpaceFingerRight);
            state.handJoints[HandJointName.RingIntermediate] = new JointPose(new RigidPose(rawJoints[14], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 15, 16, camSpaceFingerRight);
            state.handJoints[HandJointName.RingDistal] = new JointPose(new RigidPose(rawJoints[15], roomSpaceRot));
            state.handJoints[HandJointName.RingTip] = new JointPose(new RigidPose(rawJoints[16], roomSpaceRot));

            roomSpaceRot = CalculateJointRot(rawJoints, 17, 18, camSpaceFingerRight);
            state.handJoints[HandJointName.PinkyProximal] = new JointPose(new RigidPose(rawJoints[17], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 18, 19, camSpaceFingerRight);
            state.handJoints[HandJointName.PinkyIntermediate] = new JointPose(new RigidPose(rawJoints[18], roomSpaceRot));
            roomSpaceRot = CalculateJointRot(rawJoints, 19, 20, camSpaceFingerRight);
            state.handJoints[HandJointName.PinkyDistal] = new JointPose(new RigidPose(rawJoints[19], roomSpaceRot));
            state.handJoints[HandJointName.PinkyTip] = new JointPose(new RigidPose(rawJoints[20], roomSpaceRot));
        }

        private static Quaternion CalculateJointRot(Vector3[] joints, int i, int iChild, Vector3 right)
        {
            var forward = joints[iChild] - joints[i];
            return Quaternion.LookRotation(forward, Vector3.Cross(forward, right));
        }
#endif

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