using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using UnityEngine;
#if VIU_WAVEVR && UNITY_ANDROID
using wvr;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed class WaveVRModule : VRModule.ModuleBase
    {
#if VIU_WAVEVR && UNITY_ANDROID
        private const uint DEVICE_COUNT = 3;

        public static readonly Vector3 RIGHT_ARM_MULTIPLIER = new Vector3(1f, 1f, 1f);
        public static readonly Vector3 LEFT_ARM_MULTIPLIER = new Vector3(-1f, 1f, 1f);
        public const float DEFAULT_ELBOW_BEND_RATIO = 0.6f;
        public const float MIN_EXTENSION_ANGLE = 7.0f;
        public const float MAX_EXTENSION_ANGLE = 60.0f;
        public const float EXTENSION_WEIGHT = 0.4f;
        private static readonly WVR_DeviceType[] s_index2type;
        private static readonly uint[] s_type2index;
        private static readonly VRModuleDeviceClass[] s_type2class;
        private static readonly VRModuleDeviceModel[] s_type2model;

        private bool m_hasInputFocus;
        private Vector3 m_handedMultiplier;
        private WVR_DevicePosePair_t[] m_poses = new WVR_DevicePosePair_t[DEVICE_COUNT];  // HMD, R, L controllers.
        private WVR_AnalogState_t[] m_analogStates = new WVR_AnalogState_t[2];
        private WVR_PoseOriginModel m_poseOrigin;

        #region 6Dof Controller Simulation

        private enum Simulate6DoFControllerMode
        {
            KeyboardWASD,
            KeyboardModifierTrackpad,
        }

        private static Simulate6DoFControllerMode s_simulationMode = Simulate6DoFControllerMode.KeyboardWASD;
        private static Vector3[] s_simulatedCtrlPosArray;

        #endregion

        static WaveVRModule()
        {
            s_index2type = new WVR_DeviceType[VRModule.MAX_DEVICE_COUNT];
            s_index2type[0] = WVR_DeviceType.WVR_DeviceType_HMD;
            s_index2type[1] = WVR_DeviceType.WVR_DeviceType_Controller_Right;
            s_index2type[2] = WVR_DeviceType.WVR_DeviceType_Controller_Left;

            s_type2index = new uint[EnumUtils.GetMaxValue(typeof(WVR_DeviceType)) + 1];
            for (int i = 0; i < s_type2index.Length; ++i) { s_type2index[i] = INVALID_DEVICE_INDEX; }
            s_type2index[(int)WVR_DeviceType.WVR_DeviceType_HMD] = 0u;
            s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Right] = 1u;
            s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Left] = 2u;

            s_type2class = new VRModuleDeviceClass[s_type2index.Length];
            for (int i = 0; i < s_type2class.Length; ++i) { s_type2class[i] = VRModuleDeviceClass.Invalid; }
            s_type2class[(int)WVR_DeviceType.WVR_DeviceType_HMD] = VRModuleDeviceClass.HMD;
            s_type2class[(int)WVR_DeviceType.WVR_DeviceType_Controller_Right] = VRModuleDeviceClass.Controller;
            s_type2class[(int)WVR_DeviceType.WVR_DeviceType_Controller_Left] = VRModuleDeviceClass.Controller;

            s_type2model = new VRModuleDeviceModel[s_type2index.Length];
            for (int i = 0; i < s_type2model.Length; ++i) { s_type2model[i] = VRModuleDeviceModel.Unknown; }
            s_type2model[(int)WVR_DeviceType.WVR_DeviceType_HMD] = VRModuleDeviceModel.ViveFocusHMD;
            s_type2model[(int)WVR_DeviceType.WVR_DeviceType_Controller_Right] = VRModuleDeviceModel.ViveFocusFinch;
            s_type2model[(int)WVR_DeviceType.WVR_DeviceType_Controller_Left] = VRModuleDeviceModel.ViveFocusFinch;

            s_simulatedCtrlPosArray = new Vector3[s_type2index.Length];
        }

        public override bool ShouldActiveModule()
        {
            return !Application.isEditor && VIUSettings.activateWaveVRModule;
        }

        public override void OnActivated()
        {
            var instance = Object.FindObjectOfType<WaveVR_Init>();
            if (instance == null)
            {
                VRModule.Instance.gameObject.AddComponent<WaveVR_Init>();
            }

            UpdateTrackingSpaceType();
        }

        public override void OnDeactivated() { }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.RoomScale:
                    m_poseOrigin = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnGround;
                    break;
                case VRModuleTrackingSpaceType.Stationary:
                    m_poseOrigin = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead;
                    break;
            }
        }

        // FIXME: WVR_IsInputFocusCapturedBySystem currently not implemented yet
        //public override bool HasInputFocus()
        //{
        //    return m_hasInputFocus;
        //}

        public override uint GetRightControllerDeviceIndex() { return s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Right]; }

        public override uint GetLeftControllerDeviceIndex() { return s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Left]; }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            if (WaveVR.Instance == null) { return; }

            // FIXME: WVR_IsInputFocusCapturedBySystem currently not implemented yet
            //m_hasInputFocus = Interop.WVR_IsInputFocusCapturedBySystem();

            Interop.WVR_GetSyncPose(m_poseOrigin, m_poses, DEVICE_COUNT);

            for (int i = 0; i < DEVICE_COUNT; ++i)
            {
                var deviceType = m_poses[i].type;
                if (deviceType < 0 || (int)deviceType >= s_type2index.Length) { continue; }

                var deviceIndex = s_type2index[(int)deviceType];
                if (!VRModule.IsValidDeviceIndex(deviceIndex)) { continue; }

                var cState = currState[deviceIndex];
                var pState = prevState[deviceIndex];

                cState.isConnected = Interop.WVR_IsDeviceConnected(deviceType);

                if (cState.isConnected)
                {
                    if (!pState.isConnected)
                    {
                        cState.deviceClass = s_type2class[(int)deviceType];
                        cState.deviceModel = s_type2model[(int)deviceType];
                    }

                    // fetch tracking data
                    cState.isOutOfRange = false;
                    cState.isCalibrating = false;
                    cState.isUninitialized = false;

                    var devicePose = m_poses[i].pose;
                    cState.velocity = new Vector3(devicePose.Velocity.v0, devicePose.Velocity.v1, -devicePose.Velocity.v2);
                    cState.angularVelocity = new Vector3(-devicePose.AngularVelocity.v0, -devicePose.AngularVelocity.v1, devicePose.AngularVelocity.v2);

                    var rigidTransform = new WaveVR_Utils.RigidTransform(devicePose.PoseMatrix);
                    cState.position = rigidTransform.pos;
                    cState.rotation = rigidTransform.rot;

                    cState.isPoseValid = cState.pose != RigidPose.identity;

                    // fetch buttons input
                    var buttons = 0u;
                    var touches = 0u;
                    // FIXME: What does WVR_GetInputTypeCount means?
                    var analogCount = Interop.WVR_GetInputTypeCount(deviceType, WVR_InputType.WVR_InputType_Analog);
                    if (m_analogStates == null || m_analogStates.Length < analogCount) { m_analogStates = new WVR_AnalogState_t[analogCount]; }
                    const uint inputType = (uint)(WVR_InputType.WVR_InputType_Button | WVR_InputType.WVR_InputType_Touch | WVR_InputType.WVR_InputType_Analog);
#if VIU_WAVEVR_2_0_32_OR_NEWER
                    if (Interop.WVR_GetInputDeviceState(deviceType, inputType, ref buttons, ref touches, m_analogStates, (uint)analogCount))
#else
                    if (Interop.WVR_GetInputDeviceState(deviceType, inputType, ref buttons, ref touches, m_analogStates, analogCount))
#endif
                    {
                        const uint dpadMask =
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_Touchpad)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Left)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Up)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Right)) |
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_DPad_Down));

                        const uint triggerBumperMask =
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_Trigger)) |
#if VIU_WAVEVR_2_1_0_OR_NEWER
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_Digital_Trigger));
#else
                            (1 << (int)(WVR_InputId.WVR_InputId_Alias1_Bumper));
#endif

                        cState.SetButtonPress(VRModuleRawButton.System, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_System)) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.ApplicationMenu, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Menu)) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.Touchpad, (buttons & dpadMask) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.Trigger, (buttons & triggerBumperMask) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.Grip, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Grip)) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.DPadLeft, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Left)) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.DPadUp, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Up)) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.DPadRight, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Right)) != 0u);
                        cState.SetButtonPress(VRModuleRawButton.DPadDown, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Down)) != 0u);

                        cState.SetButtonTouch(VRModuleRawButton.System, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_System)) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Menu)) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.Touchpad, (touches & dpadMask) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.Trigger, (touches & triggerBumperMask) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.Grip, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Grip)) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.DPadLeft, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Left)) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.DPadUp, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Up)) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.DPadRight, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Right)) != 0u);
                        cState.SetButtonTouch(VRModuleRawButton.DPadDown, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Down)) != 0u);

                        for (int j = 0, jmax = m_analogStates.Length; j < jmax; ++j)
                        {
                            switch (m_analogStates[j].id)
                            {
                                case WVR_InputId.WVR_InputId_Alias1_Trigger:
                                    if (m_analogStates[j].type == WVR_AnalogType.WVR_AnalogType_Trigger)
                                    {
                                        cState.SetAxisValue(VRModuleRawAxis.Trigger, m_analogStates[j].axis.x);
                                    }
                                    break;
                                case WVR_InputId.WVR_InputId_Alias1_Touchpad:
                                    if (m_analogStates[j].type == WVR_AnalogType.WVR_AnalogType_TouchPad && cState.GetButtonTouch(VRModuleRawButton.Touchpad))
                                    {
                                        cState.SetAxisValue(VRModuleRawAxis.TouchpadX, m_analogStates[j].axis.x);
                                        cState.SetAxisValue(VRModuleRawAxis.TouchpadY, m_analogStates[j].axis.y);
                                    }
                                    else
                                    {
                                        cState.SetAxisValue(VRModuleRawAxis.TouchpadX, 0f);
                                        cState.SetAxisValue(VRModuleRawAxis.TouchpadY, 0f);
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        cState.buttonPressed = 0u;
                        cState.buttonTouched = 0u;
                        for (int j = 0, jmax = cState.axisValue.Length; j < jmax; ++j) { cState.axisValue[j] = 0f; }
                    }
                }
                else
                {
                    if (pState.isConnected)
                    {
                        cState.Reset();
                    }
                }
            }

            var headState = currState[s_type2index[(int)WVR_DeviceType.WVR_DeviceType_HMD]];
            var rightState = currState[s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Right]];
            var leftState = currState[s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Left]];
            ApplyVirtualArmAndSimulateInput(rightState, headState, RIGHT_ARM_MULTIPLIER);
            ApplyVirtualArmAndSimulateInput(leftState, headState, LEFT_ARM_MULTIPLIER);
        }

        private void ApplyVirtualArmAndSimulateInput(IVRModuleDeviceStateRW ctrlState, IVRModuleDeviceStateRW headState, Vector3 handSideMultiplier)
        {
            if (!ctrlState.isConnected) { return; }
            if (!VIUSettings.waveVRAddVirtualArmTo3DoFController && !VIUSettings.simulateWaveVR6DofController) { return; }

            var deviceType = (int)s_index2type[ctrlState.deviceIndex];
            if (Interop.WVR_GetDegreeOfFreedom((WVR_DeviceType)deviceType) != WVR_NumDoF.WVR_NumDoF_3DoF) { return; }


            if (VIUSettings.simulateWaveVR6DofController)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) { s_simulationMode = Simulate6DoFControllerMode.KeyboardWASD; }
                if (Input.GetKeyDown(KeyCode.Alpha2)) { s_simulationMode = Simulate6DoFControllerMode.KeyboardModifierTrackpad; }
                if (Input.GetKeyDown(KeyCode.BackQuote)) { s_simulatedCtrlPosArray[deviceType] = Vector3.zero; }

                var deltaMove = Time.unscaledDeltaTime;
                var rotY = Quaternion.Euler(0f, ctrlState.rotation.eulerAngles.y, 0f);
                var moveForward = rotY * Vector3.forward;
                var moveRight = rotY * Vector3.right;

                switch (s_simulationMode)
                {
                    case Simulate6DoFControllerMode.KeyboardWASD:
                        {
                            if (Input.GetKey(KeyCode.D)) { s_simulatedCtrlPosArray[deviceType] += moveRight * deltaMove; }
                            if (Input.GetKey(KeyCode.A)) { s_simulatedCtrlPosArray[deviceType] -= moveRight * deltaMove; }
                            if (Input.GetKey(KeyCode.E)) { s_simulatedCtrlPosArray[deviceType] += Vector3.up * deltaMove; }
                            if (Input.GetKey(KeyCode.Q)) { s_simulatedCtrlPosArray[deviceType] -= Vector3.up * deltaMove; }
                            if (Input.GetKey(KeyCode.W)) { s_simulatedCtrlPosArray[deviceType] += moveForward * deltaMove; }
                            if (Input.GetKey(KeyCode.S)) { s_simulatedCtrlPosArray[deviceType] -= moveForward * deltaMove; }

                            break;
                        }

                    case Simulate6DoFControllerMode.KeyboardModifierTrackpad:
                        {
                            float speedModifier = 2f;
                            float x = ctrlState.GetAxisValue(VRModuleRawAxis.TouchpadX) * speedModifier;
                            float y = ctrlState.GetAxisValue(VRModuleRawAxis.TouchpadY) * speedModifier;

                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                s_simulatedCtrlPosArray[deviceType] += x * moveRight * deltaMove;
                                s_simulatedCtrlPosArray[deviceType] += y * moveForward * deltaMove;
                            }

                            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                s_simulatedCtrlPosArray[deviceType] += x * moveRight * deltaMove;
                                s_simulatedCtrlPosArray[deviceType] += y * Vector3.up * deltaMove;
                            }

                            break;
                        }
                }
            }

            if (VIUSettings.waveVRAddVirtualArmTo3DoFController)
            {
                var neckPose = new RigidPose(s_simulatedCtrlPosArray[deviceType], Quaternion.identity) * GetNeckPose(headState.pose);
                ctrlState.position = GetControllerPositionWithVirtualArm(neckPose, ctrlState.rotation, handSideMultiplier);
            }
            else
            {
                ctrlState.position += s_simulatedCtrlPosArray[deviceType];
            }
        }

        private static RigidPose GetNeckPose(RigidPose headPose)
        {
            var headForward = headPose.forward;
            return new RigidPose(headPose.pos + VIUSettings.waveVRVirtualNeckPosition, Quaternion.FromToRotation(Vector3.forward, new Vector3(headForward.x, 0f, headForward.z)));
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
                Vector3.Scale(VIUSettings.waveVRVirtualElbowRestPosition, sideMultiplier) + Vector3.Scale(VIUSettings.waveVRVirtualArmExtensionOffset, sideMultiplier) * extensionRatio,
                Quaternion.Inverse(lerpRotation) * localCtrlXYRot);
            var wristPose = new RigidPose(
                Vector3.Scale(VIUSettings.waveVRVirtualWristRestPosition, sideMultiplier),
                lerpRotation);
            var palmPose = new RigidPose(
                Vector3.Scale(VIUSettings.waveVRVirtualHandRestPosition, sideMultiplier),
                Quaternion.identity);

            var finalCtrlPose = neckPose * elbowPose * wristPose * palmPose;
            return finalCtrlPose.pos;
        }

        public override void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            Interop.WVR_TriggerVibrator(s_index2type[deviceIndex], WVR_InputId.WVR_InputId_Alias1_Touchpad, durationMicroSec);
        }
#endif
    }
}