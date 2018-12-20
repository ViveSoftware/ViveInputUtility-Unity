using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System;
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
        private WVR_PoseOriginModel m_poseOrigin;
        private readonly WVR_DevicePosePair_t[] m_poses = new WVR_DevicePosePair_t[DEVICE_COUNT];  // HMD, R, L controllers.
        private readonly bool[] m_index2deviceTouched = new bool[DEVICE_COUNT];
        private WVR_AnalogState_t[] m_analogStates = new WVR_AnalogState_t[2];
        private Vector3 m_handedMultiplier;
        private IVRModuleDeviceStateRW m_headState;
        private IVRModuleDeviceStateRW m_rightState;
        private IVRModuleDeviceStateRW m_leftState;

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
            s_index2type = new WVR_DeviceType[DEVICE_COUNT];
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
            if (UnityEngine.Object.FindObjectOfType<WaveVR_Init>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<WaveVR_Init>();
            }

            EnsureDeviceStateLength(DEVICE_COUNT);

            UpdateTrackingSpaceType();
        }

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

        public override void Update()
        {
            for (uint deviceIndex = 0u; deviceIndex < DEVICE_COUNT; ++deviceIndex)
            {
                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                if (!TryGetValidDeviceState(deviceIndex, out prevState, out currState) || !currState.isConnected) { continue; }

                var deviceType = s_index2type[deviceIndex];
                // update input
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

                    currState.SetButtonPress(VRModuleRawButton.System, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_System)) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Menu)) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.Touchpad, (buttons & dpadMask) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.Trigger, (buttons & triggerBumperMask) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.Grip, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Grip)) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.DPadLeft, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Left)) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.DPadUp, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Up)) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.DPadRight, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Right)) != 0u);
                    currState.SetButtonPress(VRModuleRawButton.DPadDown, (buttons & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Down)) != 0u);

                    currState.SetButtonTouch(VRModuleRawButton.System, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_System)) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Menu)) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.Touchpad, (touches & dpadMask) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.Trigger, (touches & triggerBumperMask) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.Grip, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_Grip)) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.DPadLeft, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Left)) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.DPadUp, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Up)) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.DPadRight, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Right)) != 0u);
                    currState.SetButtonTouch(VRModuleRawButton.DPadDown, (touches & (1 << (int)WVR_InputId.WVR_InputId_Alias1_DPad_Down)) != 0u);

                    for (int j = 0, jmax = m_analogStates.Length; j < jmax; ++j)
                    {
                        switch (m_analogStates[j].id)
                        {
                            case WVR_InputId.WVR_InputId_Alias1_Trigger:
                                if (m_analogStates[j].type == WVR_AnalogType.WVR_AnalogType_Trigger)
                                {
                                    currState.SetAxisValue(VRModuleRawAxis.Trigger, m_analogStates[j].axis.x);
                                }
                                break;
                            case WVR_InputId.WVR_InputId_Alias1_Touchpad:
                                if (m_analogStates[j].type == WVR_AnalogType.WVR_AnalogType_TouchPad && currState.GetButtonTouch(VRModuleRawButton.Touchpad))
                                {
                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadX, m_analogStates[j].axis.x);
                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadY, m_analogStates[j].axis.y);
                                }
                                else
                                {
                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadX, 0f);
                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadY, 0f);
                                }
                                break;
                        }
                    }
                }
                else
                {
                    currState.buttonPressed = 0u;
                    currState.buttonTouched = 0u;
                    currState.ResetAxisValues();
                }
            }

            ProcessDeviceInputChanged();
        }

        public override void BeforeRenderUpdate()
        {
            if (WaveVR.Instance == null) { return; }

            Interop.WVR_GetSyncPose(m_poseOrigin, m_poses, DEVICE_COUNT);

            FlushDeviceState();

            for (int i = 0, imax = m_poses.Length; i < imax; ++i)
            {
                uint deviceIndex;
                var deviceType = m_poses[i].type;
                if (!TryGetAndTouchDeviceIndexByType(deviceType, out deviceIndex)) { continue; }

                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                EnsureValidDeviceState(deviceIndex, out prevState, out currState);

                if (!Interop.WVR_IsDeviceConnected(deviceType))
                {
                    if (prevState.isConnected)
                    {
                        currState.Reset();

                        switch (deviceType)
                        {
                            case WVR_DeviceType.WVR_DeviceType_HMD: m_headState = null; break;
                            case WVR_DeviceType.WVR_DeviceType_Controller_Right: m_rightState = null; break;
                            case WVR_DeviceType.WVR_DeviceType_Controller_Left: m_leftState = null; break;
                        }
                    }
                }
                else
                {
                    if (!prevState.isConnected)
                    {
                        currState.isConnected = true;
                        currState.deviceClass = s_type2class[(int)deviceType];
                        currState.deviceModel = s_type2model[(int)deviceType];
                        currState.serialNumber = deviceType.ToString();
                        currState.modelNumber = deviceType.ToString();
                        currState.renderModelName = deviceType.ToString();

                        switch (deviceType)
                        {
                            case WVR_DeviceType.WVR_DeviceType_HMD: m_headState = currState; break;
                            case WVR_DeviceType.WVR_DeviceType_Controller_Right: m_rightState = currState; break;
                            case WVR_DeviceType.WVR_DeviceType_Controller_Left: m_leftState = currState; break;
                        }
                    }

                    // update pose
                    var devicePose = m_poses[i].pose;
                    currState.velocity = new Vector3(devicePose.Velocity.v0, devicePose.Velocity.v1, -devicePose.Velocity.v2);
                    currState.angularVelocity = new Vector3(-devicePose.AngularVelocity.v0, -devicePose.AngularVelocity.v1, devicePose.AngularVelocity.v2);

                    var rigidTransform = new WaveVR_Utils.RigidTransform(devicePose.PoseMatrix);
                    currState.position = rigidTransform.pos;
                    currState.rotation = rigidTransform.rot;

                    currState.isPoseValid = currState.pose != RigidPose.identity;
                }
            }

            ApplyVirtualArmAndSimulateInput(m_rightState, m_headState, RIGHT_ARM_MULTIPLIER);
            ApplyVirtualArmAndSimulateInput(m_leftState, m_headState, LEFT_ARM_MULTIPLIER);

            ResetAndDisconnectUntouchedDevices();

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
        }

        public override void OnDeactivated()
        {
            m_headState = null;
            m_rightState = null;
            m_leftState = null;
            ResetTouchState();
        }

        // FIXME: WVR_IsInputFocusCapturedBySystem currently not implemented yet
        //public override bool HasInputFocus()
        //{
        //    return m_hasInputFocus;
        //}

        public override uint GetRightControllerDeviceIndex() { return s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Right]; }

        public override uint GetLeftControllerDeviceIndex() { return s_type2index[(int)WVR_DeviceType.WVR_DeviceType_Controller_Left]; }

        private bool TryGetAndTouchDeviceIndexByType(WVR_DeviceType type, out uint deviceIndex)
        {
            if (type < 0 || (int)type >= s_type2index.Length)
            {
                deviceIndex = INVALID_DEVICE_INDEX;
                return false;
            }

            deviceIndex = s_type2index[(int)type];
            if (VRModule.IsValidDeviceIndex(deviceIndex))
            {
                m_index2deviceTouched[deviceIndex] = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private int ResetAndDisconnectUntouchedDevices()
        {
            var disconnectedCout = 0;
            for (uint i = 0u, imax = (uint)m_index2deviceTouched.Length; i < imax; ++i)
            {
                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                if (!TryGetValidDeviceState(i, out prevState, out currState))
                {
                    Debug.Assert(!m_index2deviceTouched[i]);
                    continue;
                }

                if (!m_index2deviceTouched[i])
                {
                    if (currState.isConnected)
                    {
                        currState.Reset();
                        ++disconnectedCout;
                    }
                }
                else
                {
                    m_index2deviceTouched[i] = false;
                }
            }

            return disconnectedCout;
        }

        private void ResetTouchState()
        {
            Array.Clear(m_index2deviceTouched, 0, m_index2deviceTouched.Length);
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