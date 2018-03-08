using HTC.UnityPlugin.Utility;
using UnityEngine;
#if VIU_WAVEVR && UNITY_ANDROID
using wvr;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class WaveVRModule : VRModule.ModuleBase
    {
#if VIU_WAVEVR && UNITY_ANDROID
        private const uint HMD_INDEX = 0;
        private const uint RIGHT_INDEX = 1;
        private const uint LEFT_INDEX = 2;
        private const uint DEVICE_COUNT = 3;
        private const uint INPUT_TYPE = (uint)(WVR_InputType.WVR_InputType_Button | WVR_InputType.WVR_InputType_Touch | WVR_InputType.WVR_InputType_Analog);

        public static readonly Vector3 DEFAULT_NECK_POSITION = new Vector3(0.0f, -0.15f, 0.0f);
        public static readonly Vector3 DEFAULT_ELBOW_REST_POSITION = new Vector3(0.195f, -0.5f, 0.005f);
        public static readonly Vector3 DEFAULT_WRIST_REST_POSITION = new Vector3(0.0f, 0.0f, 0.25f);
        public static readonly Vector3 DEFAULT_CONTROLLER_REST_POSITION = new Vector3(0.0f, 0.0f, 0.05f);
        public static readonly Vector3 DEFAULT_ARM_EXTENSION_OFFSET = new Vector3(-0.13f, 0.14f, 0.08f);
        public static readonly Vector3 RIGHT_ARM_MULTIPLIER = new Vector3(1f, 1f, 1f);
        public static readonly Vector3 LEFT_ARM_MULTIPLIER = new Vector3(1f, 1f, 1f);
        public const float DEFAULT_ELBOW_BEND_RATIO = 0.6f;
        public const float MIN_EXTENSION_ANGLE = 7.0f;
        public const float MAX_EXTENSION_ANGLE = 60.0f;
        public const float EXTENSION_WEIGHT = 0.4f;

        private Vector3 handedMultiplier;

        private WVR_DeviceType[] m_deviceTypes = new WVR_DeviceType[]
        {
            WVR_DeviceType.WVR_DeviceType_HMD,
            WVR_DeviceType.WVR_DeviceType_Controller_Right,
            WVR_DeviceType.WVR_DeviceType_Controller_Left,
        };
        private WVR_DevicePosePair_t[] m_poses = new WVR_DevicePosePair_t[DEVICE_COUNT];  // HMD, R, L controllers.
        private WVR_AnalogState_t[] m_analogStates = new WVR_AnalogState_t[2];

        public override bool ShouldActiveModule()
        {
            return true;
        }

        public override void OnActivated()
        {
            var instance = Object.FindObjectOfType<WaveVR_Init>();
            if (instance == null)
            {
                VRModule.Instance.gameObject.AddComponent<WaveVR_Init>();
            }
        }

        public override void OnDeactivated() { }

        public override uint GetRightControllerDeviceIndex() { return RIGHT_INDEX; }

        public override uint GetLeftControllerDeviceIndex() { return LEFT_INDEX; }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            if (WaveVR.Instance == null) { return; }

            WVR_PoseOriginModel poseOrigin;
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.RoomScale:
                    { poseOrigin = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnGround; break; }
                case VRModuleTrackingSpaceType.Stationary:
                default:
                    { poseOrigin = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead; break; }
            }

            IVRModuleDeviceStateRW headState = null;
            IVRModuleDeviceStateRW rightState = null;
            IVRModuleDeviceStateRW leftState = null;

            Interop.WVR_GetSyncPose(poseOrigin, m_poses, DEVICE_COUNT);

            for (int i = 0; i < DEVICE_COUNT; ++i)
            {
                var deviceType = m_poses[i].type;
                var devicePose = m_poses[i].pose;

                if (deviceType == m_deviceTypes[i] && devicePose.IsValidPose)
                {
                    currState[i].isConnected = true;
                    if (deviceType == WVR_DeviceType.WVR_DeviceType_HMD)
                    {
                        currState[i].deviceClass = VRModuleDeviceClass.HMD;
                        currState[i].deviceModel = VRModuleDeviceModel.ViveFocusHMD;
                    }
                    else
                    {
                        currState[i].deviceClass = VRModuleDeviceClass.Controller;
                        currState[i].deviceModel = VRModuleDeviceModel.ViveFocusFinch;
                    }

                    var rigidTransform = new WaveVR_Utils.RigidTransform(devicePose.PoseMatrix);

                    currState[i].isPoseValid = true;
                    currState[i].isOutOfRange = false;
                    currState[i].isCalibrating = false;
                    currState[i].isUninitialized = false;

                    currState[i].velocity = new Vector3(devicePose.Velocity.v0, devicePose.Velocity.v1, -devicePose.Velocity.v2);
                    currState[i].angularVelocity = new Vector3(-devicePose.AngularVelocity.v0, -devicePose.AngularVelocity.v1, devicePose.AngularVelocity.v2);

                    currState[i].position = rigidTransform.pos;
                    currState[i].rotation = rigidTransform.rot;

                    switch (deviceType)
                    {
                        case WVR_DeviceType.WVR_DeviceType_HMD:
                            headState = currState[i];
                            break;
                        case WVR_DeviceType.WVR_DeviceType_Controller_Right:
                            rightState = currState[i];
                            break;
                        case WVR_DeviceType.WVR_DeviceType_Controller_Left:
                            leftState = currState[i];
                            break;
                    }

                    uint buttons = 0;
                    uint touches = 0;

                    // FIXME: What does WVR_GetInputTypeCount means?
                    //var analogCount = Interop.WVR_GetInputTypeCount(deviceType, WVR_InputType.WVR_InputType_Analog);
                    //if (m_analogStates == null || m_analogStates.Length < analogCount) { m_analogStates = new WVR_AnalogState_t[analogCount]; }

                    if (Interop.WVR_GetInputDeviceState(deviceType, INPUT_TYPE, ref buttons, ref touches, m_analogStates, m_analogStates.Length))
                    {
                        const uint dpadMask =
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_Touchpad)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Left)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Up)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Right)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Down));

                        const uint triggerBumperMask =
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_Trigger)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_Bumper));

                        currState[i].SetButtonPress(VRModuleRawButton.System, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_System)) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.ApplicationMenu, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Menu)) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.Touchpad, (buttons & dpadMask) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.Trigger, (buttons & triggerBumperMask) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.Grip, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Grip)) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.DPadLeft, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Left)) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.DPadUp, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Up)) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.DPadRight, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Right)) != 0u);
                        currState[i].SetButtonPress(VRModuleRawButton.DPadDown, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Down)) != 0u);

                        currState[i].SetButtonTouch(VRModuleRawButton.System, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_System)) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.ApplicationMenu, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Menu)) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.Touchpad, (touches & dpadMask) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.Trigger, (touches & triggerBumperMask) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.Grip, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Grip)) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.DPadLeft, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Left)) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.DPadUp, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Up)) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.DPadRight, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Right)) != 0u);
                        currState[i].SetButtonTouch(VRModuleRawButton.DPadDown, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Down)) != 0u);

                        for (int j = 0, jmax = m_analogStates.Length; j < jmax; ++j)
                        {
                            switch (m_analogStates[j].id)
                            {
                                case WVR_InputId.WVR_InputId_Alias1_Trigger:
                                    if (m_analogStates[j].type == WVR_AnalogType.WVR_AnalogType_Trigger)
                                    {
                                        currState[i].SetAxisValue(VRModuleRawAxis.Trigger, m_analogStates[j].axis.x);
                                    }
                                    break;
                                case WVR_InputId.WVR_InputId_Alias1_Touchpad:
                                    if (m_analogStates[j].type == WVR_AnalogType.WVR_AnalogType_TouchPad && currState[i].GetButtonTouch(VRModuleRawButton.Touchpad))
                                    {
                                        currState[i].SetAxisValue(VRModuleRawAxis.TouchpadX, m_analogStates[j].axis.x);
                                        currState[i].SetAxisValue(VRModuleRawAxis.TouchpadY, m_analogStates[j].axis.y);
                                    }
                                    else
                                    {
                                        currState[i].SetAxisValue(VRModuleRawAxis.TouchpadX, 0f);
                                        currState[i].SetAxisValue(VRModuleRawAxis.TouchpadY, 0f);
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        currState[i].buttonPressed = 0u;
                        currState[i].buttonTouched = 0u;
                        for (int j = 0, jmax = currState[i].axisValue.Length; j < jmax; ++j) { currState[i].axisValue[j] = 0f; }
                    }
                }
                else
                {
                    if (prevState[i].isConnected)
                    {
                        currState[i].Reset();
                    }
                }
            }

            // add right arm
            if (rightState != null && rightState.isConnected && rightState.position == Vector3.zero)
            {
                rightState.position = GetControllerPositionWithVirtualArm(GetNeckPose(headState.pose), rightState.rotation, RIGHT_ARM_MULTIPLIER);
            }

            // add left arm
            if (leftState != null && leftState.isConnected && leftState.position == Vector3.zero)
            {
                leftState.position = GetControllerPositionWithVirtualArm(GetNeckPose(headState.pose), leftState.rotation, LEFT_ARM_MULTIPLIER);
            }
        }

        private static RigidPose GetNeckPose(RigidPose headPose)
        {
            var headForward = headPose.forward;
            return new RigidPose(headPose.pos + DEFAULT_NECK_POSITION, Quaternion.FromToRotation(Vector3.forward, new Vector3(headForward.x, 0f, headForward.z)));
        }

        private static float GetExtensionRatio(Vector3 v)
        {
            var xAngle = 90f - Vector3.Angle(v, Vector3.up);
            return Mathf.Clamp01(Mathf.InverseLerp(MIN_EXTENSION_ANGLE, MAX_EXTENSION_ANGLE, xAngle));
        }

        private static Quaternion GetLerpRotation(Quaternion xyRotation, float extensionRatio)
        {
            float totalAngle = Quaternion.Angle(xyRotation, Quaternion.identity);
            float lerpSuppresion = 1.0f - Mathf.Pow(totalAngle / 180.0f, 6.0f);
            float inverseElbowBendRatio = 1.0f - DEFAULT_ELBOW_BEND_RATIO;
            float lerpValue = inverseElbowBendRatio + DEFAULT_ELBOW_BEND_RATIO * extensionRatio * EXTENSION_WEIGHT;
            lerpValue *= lerpSuppresion;
            return Quaternion.Lerp(Quaternion.identity, xyRotation, lerpValue);
        }

        private static Vector3 GetControllerPositionWithVirtualArm(RigidPose neckPose, Quaternion ctrlRot, Vector3 sideMultiplier)
        {
            var localCtrlForward = (Quaternion.Inverse(neckPose.rot) * ctrlRot) * Vector3.forward;
            var localCtrlXYRot = Quaternion.FromToRotation(Vector3.forward, localCtrlForward);
            var extensionRatio = GetExtensionRatio(localCtrlForward);
            var lerpRotation = GetLerpRotation(localCtrlXYRot, extensionRatio);

            var elbowPose = new RigidPose(
                Vector3.Scale(DEFAULT_ELBOW_REST_POSITION, sideMultiplier) + Vector3.Scale(DEFAULT_ARM_EXTENSION_OFFSET, sideMultiplier) * extensionRatio,
                Quaternion.Inverse(lerpRotation) * localCtrlXYRot);
            var wristPose = new RigidPose(
                Vector3.Scale(DEFAULT_WRIST_REST_POSITION, sideMultiplier),
                lerpRotation);
            var palmPose = new RigidPose(
                Vector3.Scale(DEFAULT_CONTROLLER_REST_POSITION, sideMultiplier),
                Quaternion.identity);

            var finalCtrlPose = neckPose * elbowPose * wristPose * palmPose;
            return finalCtrlPose.pos;
        }

        public override void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            Interop.WVR_TriggerVibrator(m_deviceTypes[deviceIndex], WVR_InputId.WVR_InputId_Alias1_Touchpad, durationMicroSec);
        }
#endif
    }
}