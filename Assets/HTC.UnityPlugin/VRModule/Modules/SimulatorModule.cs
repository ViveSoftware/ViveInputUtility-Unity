//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;
using UnityEngine;
using System;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public interface ISimulatorVRModule
    {
        event Action onActivated;
        event Action onDeactivated;
        event SimulatorVRModule.UpdateDeviceStateHandler onUpdateDeviceState;
    }

    public sealed class SimulatorVRModule : VRModule.ModuleBase, ISimulatorVRModule
    {
        public delegate void UpdateDeviceStateHandler(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState);

        public const uint SIMULATOR_MAX_DEVICE_COUNT = 16u;
        private const uint RIGHT_INDEX = 1;
        private const uint LEFT_INDEX = 2;

        private static readonly RigidPose s_initHmdPose = new RigidPose(new Vector3(0f, 1.75f, 0f), Quaternion.identity);
        private static RigidPose s_offsetLeftController = RigidPose.identity;
        private static RigidPose s_offsetRightController = RigidPose.identity;
        private static RigidPose s_offsetTracker = RigidPose.identity;

        private bool m_prevXREnabled;
        private bool m_resetDevices;
        private IMGUIHandle m_guiHandle;

        public event Action onActivated;
        public event Action onDeactivated;
        public event UpdateDeviceStateHandler onUpdateDeviceState;

        public uint selectedDeviceIndex { get; private set; }

        public bool hasControlFocus { get; private set; }

        public override bool ShouldActiveModule() { return VIUSettings.activateSimulatorModule; }

        public override void OnActivated()
        {
            if (m_prevXREnabled = XRSettings.enabled)
            {
                XRSettings.enabled = false;
            }

            m_resetDevices = true;
            hasControlFocus = true;
            selectedDeviceIndex = VRModule.INVALID_DEVICE_INDEX;

            // Simulator instructions GUI
            m_guiHandle = VRModule.Instance.gameObject.GetComponent<IMGUIHandle>();
            if (m_guiHandle == null)
            {
                m_guiHandle = VRModule.Instance.gameObject.AddComponent<IMGUIHandle>();
                m_guiHandle.simulator = this;
            }

            if (onActivated != null)
            {
                onActivated();
            }
        }

        public override void OnDeactivated()
        {
            if (m_guiHandle != null)
            {
                m_guiHandle.simulator = null;
                UnityEngine.Object.Destroy(m_guiHandle);
                m_guiHandle = null;
            }

            UpdateMainCamTracking();

            XRSettings.enabled = m_prevXREnabled;

            if (onDeactivated != null)
            {
                onDeactivated();
            }
        }

        public override uint GetRightControllerDeviceIndex() { return RIGHT_INDEX; }

        public override uint GetLeftControllerDeviceIndex() { return LEFT_INDEX; }

        public override void Update()
        {
            if (VIUSettings.enableSimulatorKeyboardMouseControl)
            {
                UpdateKeyDown();

                Cursor.visible = !hasControlFocus;
                Cursor.lockState = hasControlFocus ? CursorLockMode.Locked : CursorLockMode.None;
            }
        }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            if (VIUSettings.enableSimulatorKeyboardMouseControl && hasControlFocus)
            {
                if (IsEscapeKeyDown())
                {
                    if (IsDeviceSelected())
                    {
                        DeselectDevice();
                    }
                    else
                    {
                        //SetSimulatorActive(false);
                        hasControlFocus = false;
                    }
                }

                // reset to default state
                if (m_resetDevices || IsResetAllKeyDown())
                {
                    m_resetDevices = false;

                    foreach (var state in currState)
                    {
                        switch (state.deviceIndex)
                        {
                            case VRModule.HMD_DEVICE_INDEX:
                            case RIGHT_INDEX:
                            case LEFT_INDEX:
                                InitializeDevice(currState[VRModule.HMD_DEVICE_INDEX], state);
                                break;
                            default:
                                if (state.isConnected)
                                {
                                    state.Reset();
                                }
                                break;
                        }
                    }

                    DeselectDevice();
                }

                // align devices with hmd
                if (IsResetDevicesKeyDown())
                {
                    foreach (var state in currState)
                    {
                        switch (state.deviceIndex)
                        {
                            case VRModule.HMD_DEVICE_INDEX:
                                break;
                            case RIGHT_INDEX:
                                state.pose = currState[VRModule.HMD_DEVICE_INDEX].pose * s_offsetRightController;
                                break;
                            case LEFT_INDEX:
                                state.pose = currState[VRModule.HMD_DEVICE_INDEX].pose * s_offsetLeftController;
                                break;
                            default:
                                if (state.isConnected)
                                {
                                    state.pose = currState[VRModule.HMD_DEVICE_INDEX].pose * s_offsetTracker;
                                }
                                break;
                        }
                    }
                }

                // select/deselect device
                IVRModuleDeviceStateRW keySelectDevice;
                if (GetDeviceByInputDownKeyCode(currState, out keySelectDevice))
                {
                    if (IsShiftKeyPressed())
                    {
                        // remove device
                        if (keySelectDevice.isConnected && keySelectDevice.deviceIndex != VRModule.HMD_DEVICE_INDEX)
                        {
                            if (IsSelectedDevice(keySelectDevice))
                            {
                                DeselectDevice();
                            }

                            keySelectDevice.Reset();
                        }
                    }
                    else
                    {
                        if (IsSelectedDevice(keySelectDevice))
                        {
                            DeselectDevice();
                        }
                        else
                        {
                            // select device
                            if (!keySelectDevice.isConnected)
                            {
                                InitializeDevice(currState[VRModule.HMD_DEVICE_INDEX], keySelectDevice);
                            }

                            SelectDevice(keySelectDevice);
                        }
                    }
                }

                var selectedDevice = VRModule.IsValidDeviceIndex(selectedDeviceIndex) && currState[selectedDeviceIndex].isConnected ? currState[selectedDeviceIndex] : null;
                if (selectedDevice != null)
                {
                    // control selected device
                    ControlDevice(selectedDevice);

                    if (selectedDevice.deviceClass == VRModuleDeviceClass.Controller || selectedDevice.deviceClass == VRModuleDeviceClass.GenericTracker)
                    {
                        HandleDeviceInput(selectedDevice);
                    }
                }
                else if (hasControlFocus)
                {
                    // control device group
                    ControlDeviceGroup(currState);
                }

                // control camera (TFGH)
                if (currState[VRModule.HMD_DEVICE_INDEX].isConnected)
                {
                    ControlCamera(currState[VRModule.HMD_DEVICE_INDEX]);
                }
            }
            else if (IsDeviceSelected())
            {
                DeselectDevice();
            }

            if (onUpdateDeviceState != null)
            {
                onUpdateDeviceState(prevState, currState);
            }

            UpdateMainCamTracking();
        }

        private bool m_autoTrackMainCam;
        private RigidPose m_mainCamStartPose;
        private void UpdateMainCamTracking()
        {
            if (VIUSettings.simulatorAutoTrackMainCamera)
            {
                if (!m_autoTrackMainCam)
                {
                    m_autoTrackMainCam = true;
                    m_mainCamStartPose = new RigidPose(Camera.main.transform, true);
                }

                if (Camera.main != null)
                {
                    var hmd = VRModule.GetDeviceState(VRModule.HMD_DEVICE_INDEX);
                    if (hmd.isConnected)
                    {
                        RigidPose.SetPose(Camera.main.transform, hmd.pose);
                    }
                }
            }
            else
            {
                if (m_autoTrackMainCam)
                {
                    m_autoTrackMainCam = false;

                    if (Camera.main != null)
                    {
                        RigidPose.SetPose(Camera.main.transform, m_mainCamStartPose);
                    }
                }
            }
        }

        private bool IsSelectedDevice(IVRModuleDeviceStateRW state)
        {
            return selectedDeviceIndex == state.deviceIndex;
        }

        private bool IsDeviceSelected()
        {
            return VRModule.IsValidDeviceIndex(selectedDeviceIndex);
        }

        private void SelectDevice(IVRModuleDeviceStateRW state)
        {
            selectedDeviceIndex = state.deviceIndex;
        }

        private void DeselectDevice()
        {
            selectedDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        }

        // Input.GetKeyDown in UpdateDeviceState is not working
        private bool m_menuKeyPressed;
        private bool m_resetDevicesKeyPressed;
        private bool m_resetAllKeyPressed;
        private bool m_escapeKeyPressed;
        private bool m_shiftKeyPressed;
        private bool[] m_alphaKeyDownState = new bool[10];

        private void UpdateKeyDown()
        {
            m_menuKeyPressed = Input.GetKeyDown(KeyCode.M);
            m_resetDevicesKeyPressed = Input.GetKeyDown(KeyCode.F2);
            m_resetAllKeyPressed = Input.GetKeyDown(KeyCode.F3);
            m_escapeKeyPressed = Input.GetKeyDown(KeyCode.Escape);
            m_shiftKeyPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            m_alphaKeyDownState[0] = Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0);
            m_alphaKeyDownState[1] = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            m_alphaKeyDownState[2] = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            m_alphaKeyDownState[3] = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
            m_alphaKeyDownState[4] = Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            m_alphaKeyDownState[5] = Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
            m_alphaKeyDownState[6] = Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6);
            m_alphaKeyDownState[7] = Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7);
            m_alphaKeyDownState[8] = Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8);
            m_alphaKeyDownState[9] = Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9);
        }

        private bool IsMenuKeyDown() { return m_menuKeyPressed; }
        private bool IsResetAllKeyDown() { return m_resetAllKeyPressed; }
        private bool IsResetDevicesKeyDown() { return m_resetDevicesKeyPressed; }
        private bool IsEscapeKeyDown() { return m_escapeKeyPressed; }
        private bool IsShiftKeyPressed() { return m_shiftKeyPressed; }
        private bool IsAlphaKeyDown(int num) { return m_alphaKeyDownState[num]; }

        private bool GetDeviceByInputDownKeyCode(IVRModuleDeviceStateRW[] deviceStates, out IVRModuleDeviceStateRW deviceState)
        {
            var backQuotePressed = Input.GetKey(KeyCode.BackQuote);
            if (!backQuotePressed && IsAlphaKeyDown(0)) { deviceState = deviceStates[0]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(1)) { deviceState = deviceStates[1]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(2)) { deviceState = deviceStates[2]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(3)) { deviceState = deviceStates[3]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(4)) { deviceState = deviceStates[4]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(5)) { deviceState = deviceStates[5]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(6)) { deviceState = deviceStates[6]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(7)) { deviceState = deviceStates[7]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(8)) { deviceState = deviceStates[8]; return true; }
            if (!backQuotePressed && IsAlphaKeyDown(9)) { deviceState = deviceStates[9]; return true; }
            if (backQuotePressed && IsAlphaKeyDown(0)) { deviceState = deviceStates[10]; return true; }
            if (backQuotePressed && IsAlphaKeyDown(1)) { deviceState = deviceStates[11]; return true; }
            if (backQuotePressed && IsAlphaKeyDown(2)) { deviceState = deviceStates[12]; return true; }
            if (backQuotePressed && IsAlphaKeyDown(3)) { deviceState = deviceStates[13]; return true; }
            if (backQuotePressed && IsAlphaKeyDown(4)) { deviceState = deviceStates[14]; return true; }
            if (backQuotePressed && IsAlphaKeyDown(5)) { deviceState = deviceStates[15]; return true; }

            deviceState = null;
            return false;
        }

        private void InitializeDevice(IVRModuleDeviceStateRW hmdState, IVRModuleDeviceStateRW deviceState)
        {
            switch (deviceState.deviceIndex)
            {
                case VRModule.HMD_DEVICE_INDEX:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.HMD;
                        deviceState.serialNumber = "VIU Simulator HMD Device";
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveHMD;

                        deviceState.isPoseValid = true;
                        deviceState.pose = s_initHmdPose;

                        break;
                    }
                case RIGHT_INDEX:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.Controller;
                        deviceState.serialNumber = "VIU Simulator Controller Device " + RIGHT_INDEX;
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveController;

                        var pose = new RigidPose(new Vector3(0.3f, -0.25f, 0.7f), Quaternion.identity);
                        deviceState.isPoseValid = true;
                        deviceState.pose = (hmdState.isConnected ? hmdState.pose : s_initHmdPose) * pose;
                        s_offsetRightController = RigidPose.FromToPose(hmdState.isConnected ? hmdState.pose : s_initHmdPose, deviceState.pose);
                        deviceState.buttonPressed = 0ul;
                        deviceState.buttonTouched = 0ul;
                        deviceState.ResetAxisValues();
                        break;
                    }
                case LEFT_INDEX:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.Controller;
                        deviceState.serialNumber = "VIU Simulator Controller Device " + LEFT_INDEX;
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveController;

                        var pose = new RigidPose(new Vector3(-0.3f, -0.25f, 0.7f), Quaternion.identity);
                        deviceState.isPoseValid = true;
                        deviceState.pose = (hmdState.isConnected ? hmdState.pose : s_initHmdPose) * pose;
                        s_offsetLeftController = RigidPose.FromToPose(hmdState.isConnected ? hmdState.pose : s_initHmdPose, deviceState.pose);
                        deviceState.buttonPressed = 0ul;
                        deviceState.buttonTouched = 0ul;
                        deviceState.ResetAxisValues();
                        break;
                    }
                default:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.GenericTracker;
                        deviceState.serialNumber = "VIU Simulator Generic Tracker Device " + deviceState.deviceIndex;
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveTracker;

                        var pose = new RigidPose(new Vector3(0f, -0.25f, 0.7f), Quaternion.identity);
                        deviceState.isPoseValid = true;
                        deviceState.pose = (hmdState.isConnected ? hmdState.pose : s_initHmdPose) * pose;
                        s_offsetTracker = RigidPose.FromToPose(hmdState.isConnected ? hmdState.pose : s_initHmdPose, deviceState.pose);
                        deviceState.buttonPressed = 0ul;
                        deviceState.buttonTouched = 0ul;
                        deviceState.ResetAxisValues();
                        break;
                    }
            }
        }

        private void ControlDevice(IVRModuleDeviceStateRW deviceState)
        {
            var pose = deviceState.pose;
            var poseEuler = pose.rot.eulerAngles;
            var deltaAngle = Time.unscaledDeltaTime * VIUSettings.simulatorMouseRotateSpeed;
            var deltaKeyAngle = Time.unscaledDeltaTime * VIUSettings.simulatorKeyRotateSpeed;

            poseEuler.x = Mathf.Repeat(poseEuler.x + 180f, 360f) - 180f;

            if (!IsShiftKeyPressed())
            {
                var pitchDelta = -Input.GetAxisRaw("Mouse Y") * deltaAngle;
                if (pitchDelta > 0f)
                {
                    if (poseEuler.x < 90f && poseEuler.x > -180f)
                    {
                        poseEuler.x = Mathf.Min(90f, poseEuler.x + pitchDelta);
                    }
                }
                else if (pitchDelta < 0f)
                {
                    if (poseEuler.x < 180f && poseEuler.x > -90f)
                    {
                        poseEuler.x = Mathf.Max(-90f, poseEuler.x + pitchDelta);
                    }
                }

                poseEuler.y += Input.GetAxisRaw("Mouse X") * deltaAngle;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                if (poseEuler.x < 90f && poseEuler.x > -180f)
                {
                    poseEuler.x = Mathf.Min(90f, poseEuler.x + deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                if (poseEuler.x < 180f && poseEuler.x > -90f)
                {
                    poseEuler.x = Mathf.Max(-90f, poseEuler.x - deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.RightArrow)) { poseEuler.y += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.LeftArrow)) { poseEuler.y -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.C)) { poseEuler.z += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.Z)) { poseEuler.z -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.X)) { poseEuler.z = 0f; }

            pose.rot = Quaternion.Euler(poseEuler);

            var deltaMove = Time.unscaledDeltaTime * VIUSettings.simulatorKeyMoveSpeed;
            var moveForward = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.forward;
            var moveRight = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.right;
            if (Input.GetKey(KeyCode.D)) { pose.pos += moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.A)) { pose.pos -= moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.E)) { pose.pos += Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.Q)) { pose.pos -= Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.W)) { pose.pos += moveForward * deltaMove; }
            if (Input.GetKey(KeyCode.S)) { pose.pos -= moveForward * deltaMove; }

            deviceState.pose = pose;
        }

        private void ControlDeviceGroup(IVRModuleDeviceStateRW[] deviceStates)
        {
            var hmdPose = deviceStates[VRModule.HMD_DEVICE_INDEX].pose;
            var hmdPoseEuler = hmdPose.rot.eulerAngles;

            var oldRigPose = new RigidPose(hmdPose.pos, Quaternion.Euler(0f, hmdPoseEuler.y, 0f));

            var deltaAngle = Time.unscaledDeltaTime * VIUSettings.simulatorMouseRotateSpeed;
            var deltaKeyAngle = Time.unscaledDeltaTime * VIUSettings.simulatorKeyRotateSpeed;

            // translate and rotate HMD
            hmdPoseEuler.x = Mathf.Repeat(hmdPoseEuler.x + 180f, 360f) - 180f;

            var pitchDelta = -Input.GetAxisRaw("Mouse Y") * deltaAngle;
            if (pitchDelta > 0f)
            {
                if (hmdPoseEuler.x < 90f && hmdPoseEuler.x > -180f)
                {
                    hmdPoseEuler.x = Mathf.Min(90f, hmdPoseEuler.x + pitchDelta);
                }
            }
            else if (pitchDelta < 0f)
            {
                if (hmdPoseEuler.x < 180f && hmdPoseEuler.x > -90f)
                {
                    hmdPoseEuler.x = Mathf.Max(-90f, hmdPoseEuler.x + pitchDelta);
                }
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                if (hmdPoseEuler.x < 90f && hmdPoseEuler.x > -180f)
                {
                    hmdPoseEuler.x = Mathf.Min(90f, hmdPoseEuler.x + deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                if (hmdPoseEuler.x < 180f && hmdPoseEuler.x > -90f)
                {
                    hmdPoseEuler.x = Mathf.Max(-90f, hmdPoseEuler.x - deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.RightArrow)) { hmdPoseEuler.y += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.LeftArrow)) { hmdPoseEuler.y -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.C)) { hmdPoseEuler.z += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.Z)) { hmdPoseEuler.z -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.X)) { hmdPoseEuler.z = 0f; }

            hmdPoseEuler.y += Input.GetAxisRaw("Mouse X") * deltaAngle;

            hmdPose.rot = Quaternion.Euler(hmdPoseEuler);

            var deltaMove = Time.unscaledDeltaTime * VIUSettings.simulatorKeyMoveSpeed;
            var moveForward = Quaternion.Euler(0f, hmdPoseEuler.y, 0f) * Vector3.forward;
            var moveRight = Quaternion.Euler(0f, hmdPoseEuler.y, 0f) * Vector3.right;
            if (Input.GetKey(KeyCode.D)) { hmdPose.pos += moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.A)) { hmdPose.pos -= moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.E)) { hmdPose.pos += Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.Q)) { hmdPose.pos -= Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.W)) { hmdPose.pos += moveForward * deltaMove; }
            if (Input.GetKey(KeyCode.S)) { hmdPose.pos -= moveForward * deltaMove; }

            deviceStates[VRModule.HMD_DEVICE_INDEX].pose = hmdPose;

            var rigPoseOffset = new RigidPose(hmdPose.pos, Quaternion.Euler(0f, hmdPose.rot.eulerAngles.y, 0f)) * oldRigPose.GetInverse();

            for (int i = deviceStates.Length - 1; i >= 0; --i)
            {
                if (i == VRModule.HMD_DEVICE_INDEX) { continue; }

                var state = deviceStates[i];
                if (!state.isConnected) { continue; }

                state.pose = rigPoseOffset * state.pose;
            }
        }

        private void ControlCamera(IVRModuleDeviceStateRW deviceState)
        {
            var pose = deviceState.pose;
            var poseEuler = pose.rot.eulerAngles;
            var deltaKeyAngle = Time.unscaledDeltaTime * VIUSettings.simulatorKeyRotateSpeed;

            poseEuler.x = Mathf.Repeat(poseEuler.x + 180f, 360f) - 180f;

            if (Input.GetKey(KeyCode.K))
            {
                if (poseEuler.x < 90f && poseEuler.x > -180f)
                {
                    poseEuler.x = Mathf.Min(90f, poseEuler.x + deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.I))
            {
                if (poseEuler.x < 180f && poseEuler.x > -90f)
                {
                    poseEuler.x = Mathf.Max(-90f, poseEuler.x - deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.L)) { poseEuler.y += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.J)) { poseEuler.y -= deltaKeyAngle; }

            if (Input.GetKey(KeyCode.N)) { poseEuler.z += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.V)) { poseEuler.z -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.B)) { poseEuler.z = 0f; }

            pose.rot = Quaternion.Euler(poseEuler);

            var deltaMove = Time.unscaledDeltaTime * VIUSettings.simulatorKeyMoveSpeed;
            var moveForward = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.forward;
            var moveRight = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.right;
            if (Input.GetKey(KeyCode.H)) { pose.pos += moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.F)) { pose.pos -= moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.Y)) { pose.pos += Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.R)) { pose.pos -= Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.T)) { pose.pos += moveForward * deltaMove; }
            if (Input.GetKey(KeyCode.G)) { pose.pos -= moveForward * deltaMove; }

            deviceState.pose = pose;
        }

        private void HandleDeviceInput(IVRModuleDeviceStateRW deviceState)
        {
            var leftPressed = Input.GetMouseButton(0);
            var rightPressed = Input.GetMouseButton(1);
            var midPressed = Input.GetMouseButton(2);

            deviceState.SetButtonPress(VRModuleRawButton.Trigger, leftPressed);
            deviceState.SetButtonTouch(VRModuleRawButton.Trigger, leftPressed);
            deviceState.SetAxisValue(VRModuleRawAxis.Trigger, leftPressed ? 1f : 0f);

            deviceState.SetButtonPress(VRModuleRawButton.Grip, midPressed);
            deviceState.SetButtonTouch(VRModuleRawButton.Grip, midPressed);
            deviceState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, midPressed ? 1f : 0f);

            deviceState.SetButtonPress(VRModuleRawButton.Touchpad, rightPressed);

            deviceState.SetButtonPress(VRModuleRawButton.ApplicationMenu, IsMenuKeyDown());
            deviceState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, IsMenuKeyDown());

            if (VIUSettings.simulateTrackpadTouch && IsShiftKeyPressed())
            {
                deviceState.SetButtonTouch(VRModuleRawButton.Touchpad, true);
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadX, deviceState.GetAxisValue(VRModuleRawAxis.TouchpadX) + (Input.GetAxisRaw("Mouse X") * 0.1f));
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadY, deviceState.GetAxisValue(VRModuleRawAxis.TouchpadY) + (Input.GetAxisRaw("Mouse Y") * 0.1f));
            }
            else
            {
                deviceState.SetButtonTouch(VRModuleRawButton.Touchpad, rightPressed);
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadX, 0f);
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadY, 0f);
            }
        }

        private class IMGUIHandle : MonoBehaviour
        {
            public SimulatorVRModule simulator { get; set; }

            private bool showGUI { get; set; }

            private void Start()
            {
                showGUI = true;
            }

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    showGUI = !showGUI;
                }
            }

            private static string Bold(string s) { return "<b>" + s + "</b>"; }

            private static string SetColor(string s, string color) { return "<color=" + color + ">" + s + "</color>"; }

            private void OnGUI()
            {
                if (!VIUSettings.enableSimulatorKeyboardMouseControl) { return; }

                if (!showGUI || simulator == null) { return; }

                var hints = string.Empty;

                if (simulator.hasControlFocus)
                {
                    GUI.skin.box.stretchWidth = false;
                    GUI.skin.box.stretchHeight = false;
                    GUI.skin.box.alignment = TextAnchor.UpperLeft;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.box.normal.textColor = Color.white;

                    // device status grids
                    GUI.skin.box.padding = new RectOffset(10, 10, 5, 5);

                    GUILayout.BeginArea(new Rect(5f, 5f, Screen.width, 30f));
                    GUILayout.BeginHorizontal();

                    for (uint i = 0u; i < SIMULATOR_MAX_DEVICE_COUNT; ++i)
                    {
                        var isHmd = i == VRModule.HMD_DEVICE_INDEX;
                        var isSelectedDevice = i == simulator.selectedDeviceIndex;
                        var isConndected = VRModule.GetCurrentDeviceState(i).isConnected;

                        var deviceName = isHmd ? "HMD 0" : i.ToString();
                        var colorName = !isConndected ? "grey" : isSelectedDevice ? "lime" : "white";

                        GUILayout.Box(SetColor(Bold(deviceName), colorName));
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();

                    var selectedDeviceClass = VRModule.GetCurrentDeviceState(simulator.selectedDeviceIndex).deviceClass;
                    // instructions
                    if (selectedDeviceClass == VRModuleDeviceClass.Invalid)
                    {
                        hints += "Pause simulator: " + Bold("ESC") + "\n";
                        hints += "Toggle instructions: " + Bold("F1") + "\n";
                        hints += "Align devices to HMD: " + Bold("F2") + "\n";
                        hints += "Reset all devices to initial state: " + Bold("F3") + "\n\n";

                        hints += "Move: " + Bold("WASD / QE") + "\n";
                        hints += "Rotate: " + Bold("Mouse") + "\n";
                        hints += "Add and select a device: \n";
                        hints += "    [N] " + Bold("Num 0~9") + "\n";
                        hints += "    [10+N] " + Bold("` + Num 0~5") + "\n";
                        hints += "Remove and deselect a device: \n";
                        hints += "    [N] " + Bold("Shift + Num 0~9") + "\n";
                        hints += "    [10+N] " + Bold("Shift + ` + Num 0~5") + "\n";
                    }
                    else
                    {
                        hints += "Toggle instructions: " + Bold("F1") + "\n";
                        hints += "Align devices with HMD: " + Bold("F2") + "\n";
                        hints += "Reset all devices to initial state: " + Bold("F3") + "\n\n";

                        hints += "Currently controlling ";
                        hints += SetColor(Bold("Device " + simulator.selectedDeviceIndex.ToString()) + " " + Bold("(" + selectedDeviceClass.ToString() + ")") + "\n", "lime");
                        if (simulator.selectedDeviceIndex <= 9)
                        {
                            hints += "Deselect this device: " + Bold("ESC") + " / " + Bold("Num " + simulator.selectedDeviceIndex) + "\n";
                        }
                        else
                        {
                            hints += "Deselect this device: " + Bold("ESC") + " / " + Bold("` + Num " + simulator.selectedDeviceIndex) + "\n";
                        }
                        hints += "Add and select a device: \n";
                        hints += "    [N] " + Bold("Num 0~9") + "\n";
                        hints += "    [10+N] " + Bold("` + Num 0~5") + "\n";
                        hints += "Remove and deselect a device: \n";
                        hints += "    [N] " + Bold("Shift + Num 0~9") + "\n";
                        hints += "    [10+N] " + Bold("Shift + ` + Num 0~5") + "\n";

                        hints += "\n";
                        hints += "Move: " + Bold("WASD / QE") + "\n";
                        hints += "Rotate (pitch and yaw): " + Bold("Mouse") + " or " + Bold("Arrow Keys") + "\n";
                        hints += "Rotate (roll): " + Bold("ZC") + "\n";
                        hints += "Reset roll: " + Bold("X") + "\n";

                        if (selectedDeviceClass == VRModuleDeviceClass.Controller || selectedDeviceClass == VRModuleDeviceClass.GenericTracker)
                        {
                            hints += "\n";
                            hints += "Trigger press: " + Bold("Mouse Left") + "\n";
                            hints += "Grip press: " + Bold("Mouse Middle") + "\n";
                            hints += "Trackpad press: " + Bold("Mouse Right") + "\n";
                            hints += "Trackpad touch: " + Bold("Hold Shift") + " + " + Bold("Mouse") + "\n";
                            hints += "Menu button press: " + Bold("M") + "\n";
                        }
                    }

                    hints += "\n";
                    hints += "HMD Move: " + Bold("TFGH / RY") + "\n";
                    hints += "HMD Rotate (pitch and yaw): " + Bold("IJKL") + "\n";
                    hints += "HMD Rotate (roll): " + Bold("VN") + "\n";
                    hints += "HMD Reset roll: " + Bold("B");

                    GUI.skin.box.padding = new RectOffset(10, 10, 10, 10);

                    GUILayout.BeginArea(new Rect(5f, 35f, Screen.width, Screen.height));
                    GUILayout.Box(hints);
                    GUILayout.EndArea();
                }
                else
                {
                    // simulator resume button
                    int buttonHeight = 30;
                    int buttonWidth = 130;
                    Rect ButtonRect = new Rect((Screen.width * 0.5f) - (buttonWidth * 0.5f), (Screen.height * 0.5f) - buttonHeight, buttonWidth, buttonHeight);

                    if (GUI.Button(ButtonRect, Bold("Back to simulator")))
                    {
                        simulator.hasControlFocus = true;
                    }

                    GUI.skin.box.padding = new RectOffset(10, 10, 5, 5);

                    GUILayout.BeginArea(new Rect(5f, 5f, Screen.width, 30f));
                    GUILayout.BeginHorizontal();

                    hints += "Toggle instructions: " + Bold("F1");
                    GUILayout.Box(hints);

                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                }
            }
        }
    }
}