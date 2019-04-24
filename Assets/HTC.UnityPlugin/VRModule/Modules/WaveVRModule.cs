//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System;
using System.Collections.Generic;
using UnityEngine;
#if VIU_WAVEVR && UNITY_ANDROID
using wvr;
using Object = UnityEngine.Object;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public static readonly bool isWaveVRPluginDetected =
#if VIU_WAVEVR
            true;
#else
            false;
#endif
    }

    public sealed class WaveVRModule : VRModule.ModuleBase
    {
        public override int moduleIndex { get { return (int)VRModuleActiveEnum.WaveVR; } }

#if VIU_WAVEVR && UNITY_ANDROID
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
                if (hook.GetComponent<WaveVR_Render>() == null)
                {
                    hook.gameObject.AddComponent<WaveVR_Render>();
                }
                if (hook.GetComponent<VivePoseTracker>() == null)
                {
                    hook.gameObject.AddComponent<VivePoseTracker>().viveRole.SetEx(DeviceRole.Hmd);
                }
                if (hook.GetComponent<AudioListener>() != null)
                {
                    Object.Destroy(hook.GetComponent<AudioListener>());
                }
            }
        }

        private class RenderModelCreator : RenderModelHook.RenderModelCreator
        {
            private uint m_index = INVALID_DEVICE_INDEX;
            private GameObject m_model;
            private WVR_DeviceType m_loadedHandType;

            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void UpdateRenderModel()
            {
                if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }

                var hasValidModel = false;
                var handType = default(WVR_DeviceType);
                if (VRModule.IsValidDeviceIndex(m_index))
                {
                    if (m_index == VRModule.GetRightControllerDeviceIndex())
                    {
                        hasValidModel = true;
                        handType = WVR_DeviceType.WVR_DeviceType_Controller_Right;
                    }
                    else if (m_index == VRModule.GetLeftControllerDeviceIndex())
                    {
                        hasValidModel = true;
                        handType = WVR_DeviceType.WVR_DeviceType_Controller_Left;
                    }
                }

                // NOTE: load renderModel only if it hasn't been loaded or user changes handType
                if (hasValidModel)
                {
                    if (m_model != null && m_loadedHandType != handType)
                    {
                        CleanUpRenderModel();
                    }

                    if (m_model == null)
                    {
                        // Create WaveVR_ControllerLoader silently (to avoid Start and OnEnable)
                        var loaderGO = new GameObject("Loader");
                        loaderGO.transform.SetParent(hook.transform, false);
                        loaderGO.SetActive(false);
                        var loader = loaderGO.AddComponent<WaveVR_ControllerLoader>();
                        loader.TrackPosition = false;
                        loader.TrackRotation = false;
                        loader.showIndicator = false;
                        // Call onLoadController to create model (chould be Finch/Link/Pico/QIYIVR)
                        switch (handType)
                        {
                            case WVR_DeviceType.WVR_DeviceType_Controller_Right:
#if VIU_WAVEVR_3_0_0_OR_NEWER
                                loader.WhichHand = s_moduleInstance.m_deviceHands[RIGHT_INDEX];
#else
                                loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Right;
#endif
                                loaderGO.SetActive(true);

                                if (WaveVR.Instance.getDeviceByType(handType).pose.pose.Is6DoFPose && WaveVR_Controller.IsLeftHanded)
                                {
                                    loaderGO.SendMessage("onLoadController", WVR_DeviceType.WVR_DeviceType_Controller_Left);
                                }
                                else
                                {
                                    loaderGO.SendMessage("onLoadController", WVR_DeviceType.WVR_DeviceType_Controller_Right);
                                }
                                break;
                            case WVR_DeviceType.WVR_DeviceType_Controller_Left:
#if VIU_WAVEVR_3_0_0_OR_NEWER
                                loader.WhichHand = s_moduleInstance.m_deviceHands[LEFT_INDEX];
#else
                                if (Interop.WVR_GetWaveRuntimeVersion() >= 3 && WaveVR_Controller.IsLeftHanded)
                                {
                                    loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Right;
                                }
                                else
                                {
                                    loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Left;
                                }
#endif
                                loaderGO.SetActive(true);

                                if (WaveVR.Instance.getDeviceByType(handType).pose.pose.Is6DoFPose && WaveVR_Controller.IsLeftHanded)
                                {
                                    loaderGO.SendMessage("onLoadController", WVR_DeviceType.WVR_DeviceType_Controller_Right);
                                }
                                else
                                {
                                    loaderGO.SendMessage("onLoadController", WVR_DeviceType.WVR_DeviceType_Controller_Left);
                                }
                                break;
                        }

                        // Find transform that only contains controller model (include animator, exclude PoseTracker/Beam/UIPointer)
                        // and remove other unnecessary objects
                        var ctrllerActions = FindWaveVRControllerActionsObjInChildren();
                        if (ctrllerActions != null)
                        {
                            ctrllerActions.transform.SetParent(hook.transform, false);
                            ctrllerActions.transform.SetAsFirstSibling();
                            for (int i = hook.transform.childCount - 1; i >= 1; --i)
                            {
                                Object.Destroy(hook.transform.GetChild(i).gameObject);
                            }
                            ctrllerActions.gameObject.SetActive(true);
                            m_model = ctrllerActions.gameObject;
                        }
                        else
                        {
                            Debug.LogWarning("FindWaveVRControllerActionsObjInChildren failed");
                            for (int i = hook.transform.childCount - 1; i >= 0; --i)
                            {
                                Object.Destroy(hook.transform.GetChild(i).gameObject);
                            }
                        }

                        m_loadedHandType = handType;
                    }

                    m_model.SetActive(true);
                }
                else
                {
                    if (m_model != null)
                    {
                        m_model.SetActive(false);
                    }
                }
            }

            public override void CleanUpRenderModel()
            {
                if (m_model != null)
                {
                    Object.Destroy(m_model);
                    m_model = null;
                }
            }

            // FIXME: This is for finding Controller model with animator, is reliable?
            private Transform FindWaveVRControllerActionsObjInChildren()
            {
                var nodes = new List<Transform>();
                nodes.Add(hook.transform);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    var parent = nodes[i];
                    for (int j = 0, jmax = parent.childCount; j < jmax; ++j)
                    {
                        var child = parent.GetChild(j);
                        nodes.Add(child);
                        if (child.GetComponent<WaveVR_PoseTrackerManager>() != null) { continue; }
                        if (child.GetComponent<WaveVR_Beam>() != null) { continue; }
                        if (child.GetComponent<WaveVR_ControllerPointer>() != null) { continue; }
                        if (child.GetComponent<WaveVR_ControllerLoader>() != null) { continue; }
                        return child;
                    }
                }

                return null;
            }
        }

        private const uint DEVICE_COUNT = 3;
        private const uint HEAD_INDEX = 0;
        private const uint RIGHT_INDEX = 1;
        private const uint LEFT_INDEX = 2;

        public static readonly Vector3 RIGHT_ARM_MULTIPLIER = new Vector3(1f, 1f, 1f);
        public static readonly Vector3 LEFT_ARM_MULTIPLIER = new Vector3(-1f, 1f, 1f);
        public const float DEFAULT_ELBOW_BEND_RATIO = 0.6f;
        public const float MIN_EXTENSION_ANGLE = 7.0f;
        public const float MAX_EXTENSION_ANGLE = 60.0f;
        public const float EXTENSION_WEIGHT = 0.4f;
        private static WaveVRModule s_moduleInstance;
        private static readonly WVR_DeviceType[] s_index2type;
        private static readonly uint[] s_type2index;
        private static readonly VRModuleDeviceClass[] s_type2class;
        private static readonly VRModuleDeviceModel[] s_type2model;

        //private bool m_hasInputFocus;
        private readonly bool[] m_index2deviceTouched = new bool[DEVICE_COUNT];
        private IVRModuleDeviceStateRW m_headState;
        private IVRModuleDeviceStateRW m_rightState;
        private IVRModuleDeviceStateRW m_leftState;
        private WaveVR_ControllerLoader.ControllerHand[] m_deviceHands = new WaveVR_ControllerLoader.ControllerHand[DEVICE_COUNT];

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
#if UNITY_EDITOR && !VIU_WAVEVR_2_1_0_OR_NEWER
            return false;
#else
            return VIUSettings.activateWaveVRModule;
#endif
        }

        public override void OnActivated()
        {
            if (Object.FindObjectOfType<WaveVR_Init>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<WaveVR_Init>();
            }

#if !UNITY_EDITOR && VIU_WAVEVR_3_0_0_OR_NEWER
            if (Object.FindObjectOfType<WaveVR_ButtonList>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<WaveVR_ButtonList>();
            }
#endif

#if VIU_WAVEVR_3_0_0_OR_NEWER
            var digitalCapability = (uint)WVR_InputType.WVR_InputType_Button;
            var analogCapability = (uint)(WVR_InputType.WVR_InputType_Button | WVR_InputType.WVR_InputType_Touch | WVR_InputType.WVR_InputType_Analog);
            var inputRequests = new WVR_InputAttribute_t[]
            {
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Menu, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Grip, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_DPad_Left, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_DPad_Up, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_DPad_Right, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_DPad_Down, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Volume_Up, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Volume_Down, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Enter, axis_type = WVR_AnalogType.WVR_AnalogType_None, capability = digitalCapability },

                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Touchpad, axis_type = WVR_AnalogType.WVR_AnalogType_2D, capability = analogCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Thumbstick, axis_type = WVR_AnalogType.WVR_AnalogType_2D, capability = analogCapability },

                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Digital_Trigger, axis_type = WVR_AnalogType.WVR_AnalogType_1D, capability = analogCapability },
                new WVR_InputAttribute_t() { id = WVR_InputId.WVR_InputId_Alias1_Trigger, axis_type = WVR_AnalogType.WVR_AnalogType_1D, capability = analogCapability },
            };

#if !UNITY_EDITOR
            Interop.WVR_SetInputRequest(WVR_DeviceType.WVR_DeviceType_Controller_Right, inputRequests, (uint)inputRequests.Length);
            Interop.WVR_SetInputRequest(WVR_DeviceType.WVR_DeviceType_Controller_Left, inputRequests, (uint)inputRequests.Length);
#endif
#endif

            EnsureDeviceStateLength(DEVICE_COUNT);

            UpdateTrackingSpaceType();

            s_moduleInstance = this;

            WaveVR_Utils.Event.Listen(WaveVR_Utils.Event.NEW_POSES, OnNewPoses);
        }

        public override void OnDeactivated()
        {
            WaveVR_Utils.Event.Remove(WaveVR_Utils.Event.NEW_POSES, OnNewPoses);

            m_headState = null;
            m_rightState = null;
            m_leftState = null;
            ResetTouchState();

            s_moduleInstance = null;
        }

        public override void UpdateTrackingSpaceType()
        {
            if (WaveVR_Render.Instance != null)
            {
                // Only effected when origin is OnHead or OnGround
                // This way you can manually set WaveVR_Render origin to other value lik OnTrackingObserver or OnHead_3DoF
                if (VRModule.trackingSpaceType == VRModuleTrackingSpaceType.RoomScale)
                {
                    if (WaveVR_Render.Instance.origin == WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead)
                    {
                        WaveVR_Render.Instance.origin = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnGround;
                    }
                }
                else if (VRModule.trackingSpaceType == VRModuleTrackingSpaceType.Stationary)
                {
                    if (WaveVR_Render.Instance.origin == WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnGround)
                    {
                        WaveVR_Render.Instance.origin = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead;
                    }
                }
            }
        }

        public override void Update()
        {
            var rightDevice = GetWVRControllerDevice(WVR_DeviceType.WVR_DeviceType_Controller_Right);
            UpdateDeviceInput(1, rightDevice);
            var leftDevice = GetWVRControllerDevice(WVR_DeviceType.WVR_DeviceType_Controller_Left);
            UpdateDeviceInput(2, leftDevice);

            ProcessDeviceInputChanged();
        }

        private WaveVR_Controller.Device GetWVRControllerDevice(WVR_DeviceType deviceType)
        {
            switch (deviceType)
            {
                case WVR_DeviceType.WVR_DeviceType_Controller_Right:
                    return WaveVR_Controller.Input(WaveVR_Controller.IsLeftHanded ? WVR_DeviceType.WVR_DeviceType_Controller_Left : WVR_DeviceType.WVR_DeviceType_Controller_Right);
                case WVR_DeviceType.WVR_DeviceType_Controller_Left:
                    return WaveVR_Controller.Input(WaveVR_Controller.IsLeftHanded ? WVR_DeviceType.WVR_DeviceType_Controller_Right : WVR_DeviceType.WVR_DeviceType_Controller_Left);
                default:
                    return null;
            }
        }

        private WaveVR.Device GetWVRDevice(WVR_DeviceType deviceType)
        {
            switch (deviceType)
            {
                case WVR_DeviceType.WVR_DeviceType_HMD:
                    return WaveVR.Instance.getDeviceByType(deviceType);
                case WVR_DeviceType.WVR_DeviceType_Controller_Right:
                    return WaveVR.Instance.getDeviceByType(WaveVR_Controller.IsLeftHanded ? WVR_DeviceType.WVR_DeviceType_Controller_Left : WVR_DeviceType.WVR_DeviceType_Controller_Right);
                case WVR_DeviceType.WVR_DeviceType_Controller_Left:
                    return WaveVR.Instance.getDeviceByType(WaveVR_Controller.IsLeftHanded ? WVR_DeviceType.WVR_DeviceType_Controller_Right : WVR_DeviceType.WVR_DeviceType_Controller_Left);
                default:
                    return null;
            }
        }

        private void UpdateDeviceInput(uint deviceIndex, WaveVR_Controller.Device deviceInput)
        {
#if VIU_WAVEVR_2_1_0_OR_NEWER
            const WVR_InputId digitalTrggerBumpID = WVR_InputId.WVR_InputId_Alias1_Digital_Trigger;
#else
            const WVR_InputId digitalTrggerBumpID = WVR_InputId.WVR_InputId_Alias1_Bumper;
#endif

            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            if (!TryGetValidDeviceState(deviceIndex, out prevState, out currState) || !deviceInput.connected) { return; }

            if (deviceInput != null)
            {
                var systemPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_System);
                var menuPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_Menu);
                var triggerPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_Trigger);
                var digitalTriggerPressed = deviceInput.GetPress(digitalTrggerBumpID);
                var gripPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_Grip);
                var touchpadPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_Touchpad);
                var dpadLeftPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_DPad_Left);
                var dpadUpPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_DPad_Up);
                var dpadRightPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_DPad_Right);
                var dpadDownPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_DPad_Down);
                currState.SetButtonPress(VRModuleRawButton.System, systemPressed);
                currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuPressed);
                currState.SetButtonPress(VRModuleRawButton.Touchpad, touchpadPressed || dpadLeftPressed || dpadUpPressed || dpadRightPressed || dpadDownPressed);
                currState.SetButtonPress(VRModuleRawButton.Trigger, triggerPressed || digitalTriggerPressed);
                currState.SetButtonPress(VRModuleRawButton.Grip, gripPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadLeft, dpadLeftPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadUp, dpadUpPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadRight, dpadRightPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadDown, dpadDownPressed);

                var systemTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_System);
                var menuTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_Menu);
                var triggerTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_Trigger);
                var digitalTriggerTouched = deviceInput.GetTouch(digitalTrggerBumpID);
                var gripTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_Grip);
                var touchpadTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_Touchpad);
                var dpadLeftTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_DPad_Left);
                var dpadUpTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_DPad_Up);
                var dpadRightTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_DPad_Right);
                var dpadDownTouched = deviceInput.GetTouch(WVR_InputId.WVR_InputId_Alias1_DPad_Down);
                currState.SetButtonTouch(VRModuleRawButton.System, systemTouched);
                currState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, menuTouched);
                currState.SetButtonTouch(VRModuleRawButton.Touchpad, touchpadTouched || dpadLeftTouched || dpadUpTouched || dpadRightTouched || dpadDownTouched);
                currState.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouched || digitalTriggerTouched);
                currState.SetButtonTouch(VRModuleRawButton.Grip, gripTouched);
                currState.SetButtonTouch(VRModuleRawButton.DPadLeft, dpadLeftTouched);
                currState.SetButtonTouch(VRModuleRawButton.DPadUp, dpadUpTouched);
                currState.SetButtonTouch(VRModuleRawButton.DPadRight, dpadRightTouched);
                currState.SetButtonTouch(VRModuleRawButton.DPadDown, dpadDownTouched);

                var triggerAxis = deviceInput.GetAxis(WVR_InputId.WVR_InputId_Alias1_Trigger);
                var touchAxis = deviceInput.GetAxis(WVR_InputId.WVR_InputId_Alias1_Touchpad);
                currState.SetAxisValue(VRModuleRawAxis.Trigger, triggerAxis.x);
                currState.SetAxisValue(VRModuleRawAxis.TouchpadX, touchAxis.x);
                currState.SetAxisValue(VRModuleRawAxis.TouchpadY, touchAxis.y);
            }
            else
            {
                currState.buttonPressed = 0u;
                currState.buttonTouched = 0u;
                currState.ResetAxisValues();
            }
        }

        private IVRModuleDeviceStateRW UpdateDevicePose(uint deviceIndex, WaveVR.Device content)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            EnsureValidDeviceState(deviceIndex, out prevState, out currState);

            var deviceConnected = content.type == WVR_DeviceType.WVR_DeviceType_HMD ? true : content.connected;

            if (!deviceConnected)
            {
                if (prevState.isConnected)
                {
                    currState.Reset();

                    switch (content.type)
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
                    currState.deviceClass = s_type2class[(int)content.type];
                    currState.deviceModel = s_type2model[(int)content.type];
                    currState.serialNumber = content.type.ToString();
                    currState.modelNumber = content.type.ToString();
                    currState.renderModelName = content.type.ToString();
                }

                // update pose
                var devicePose = content.pose.pose;
                currState.velocity = new Vector3(devicePose.Velocity.v0, devicePose.Velocity.v1, -devicePose.Velocity.v2);
                currState.angularVelocity = new Vector3(-devicePose.AngularVelocity.v0, -devicePose.AngularVelocity.v1, devicePose.AngularVelocity.v2);

                var rigidTransform = content.rigidTransform;
                currState.position = rigidTransform.pos;
                currState.rotation = rigidTransform.rot;

                currState.isPoseValid = devicePose.IsValidPose;
            }

            return currState;
        }

        private void OnNewPoses(params object[] args)
        {
            if (WaveVR.Instance == null) { return; }

            FlushDeviceState();

            var headDevice = GetWVRDevice(WVR_DeviceType.WVR_DeviceType_HMD);
            m_headState = UpdateDevicePose(0, headDevice);
            var rightDevice = GetWVRDevice(WVR_DeviceType.WVR_DeviceType_Controller_Right);
            m_rightState = UpdateDevicePose(1, rightDevice);
            var leftDevice = GetWVRDevice(WVR_DeviceType.WVR_DeviceType_Controller_Left);
            m_leftState = UpdateDevicePose(2, leftDevice);

#if VIU_WAVEVR_3_0_0_OR_NEWER
            if (WaveVR_Controller.IsLeftHanded)
            {
                m_deviceHands[RIGHT_INDEX] = WaveVR_ControllerLoader.ControllerHand.Non_Dominant;
                m_deviceHands[LEFT_INDEX] = WaveVR_ControllerLoader.ControllerHand.Dominant;
            }
            else
            {
                m_deviceHands[RIGHT_INDEX] = WaveVR_ControllerLoader.ControllerHand.Dominant;
                m_deviceHands[LEFT_INDEX] = WaveVR_ControllerLoader.ControllerHand.Non_Dominant;
            }
#endif


            if (m_rightState != null && !rightDevice.pose.pose.Is6DoFPose)
            {
                ApplyVirtualArmAndSimulateInput(m_rightState, m_headState, RIGHT_ARM_MULTIPLIER);
            }

            if (m_leftState != null && !leftDevice.pose.pose.Is6DoFPose)
            {
                ApplyVirtualArmAndSimulateInput(m_leftState, m_headState, LEFT_ARM_MULTIPLIER);
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
        }

        // FIXME: WVR_IsInputFocusCapturedBySystem currently not implemented yet
        //public override bool HasInputFocus()
        //{
        //    return m_hasInputFocus;
        //}

        public override uint GetRightControllerDeviceIndex() { return RIGHT_INDEX; }

        public override uint GetLeftControllerDeviceIndex() { return LEFT_INDEX; }

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
#if VIU_WAVEVR_2_1_0_OR_NEWER && UNITY_EDITOR
            if (!WaveVR.Instance.isSimulatorOn || WaveVR_Utils.WVR_GetDegreeOfFreedom_S() == (int)WVR_NumDoF.WVR_NumDoF_6DoF) { return; }
#else
            if (Interop.WVR_GetDegreeOfFreedom((WVR_DeviceType)deviceType) == WVR_NumDoF.WVR_NumDoF_6DoF) { return; }
#endif

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
            var deviceInput = WaveVR_Controller.Input(s_index2type[deviceIndex]);
            if (deviceInput != null)
            {
                deviceInput.TriggerHapticPulse(durationMicroSec);
            }
        }
#endif
    }
}