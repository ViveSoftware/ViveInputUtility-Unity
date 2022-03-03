//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Vive.WaveVRExtension;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if VIU_WAVEVR && UNITY_ANDROID
using wvr;
using Object = UnityEngine.Object;
#endif
#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL || VIU_WAVEXR_ESSENCE_RENDERMODEL
using Wave.Essence;
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

        public static readonly bool isWaveVRSupported =
#if VIU_WAVEVR_SUPPORT
            true;
#else
            false;
#endif
    }

    public sealed class WaveVRModule : VRModule.ModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.WaveVR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.WaveVR; } }

        [RenderModelHook.CreatorPriorityAttirbute(0)]
        private class XRRenderModelCreator : RenderModelHook.DefaultRenderModelCreator
        {
            private uint m_index = INVALID_DEVICE_INDEX;
            private GameObject m_modelObj;

            public override bool shouldActive
            {
                get
                {
#if (VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL || VIU_WAVEXR_ESSENCE_RENDERMODEL) && UNITY_ANDROID
                    return VIUSettings.enableWaveXRRenderModel;
#else
                    return false;
#endif
                }
            }

            public override void UpdateRenderModel()
            {
                if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }

                if (VRModule.GetDeviceState(m_index).deviceClass == VRModuleDeviceClass.Controller)
                {
                    if (m_index == VRModule.GetRightControllerDeviceIndex())
                    {
                        UpdateDefaultRenderModel(false);

                        if (m_modelObj == null)
                        {
                            m_modelObj = new GameObject("Model");
                            m_modelObj.transform.SetParent(hook.transform, false);
                            m_modelObj.SetActive(false);
#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL
#if VIU_WAVE_XRSDK_3_99_31_OR_NEWER
                            m_modelObj.AddComponent<PoseMode>();
#endif
                            GameObject controllerObj = new GameObject("Controller");
                            controllerObj.transform.SetParent(m_modelObj.transform, false);
                            controllerObj.AddComponent<Wave.Essence.Controller.Model.RenderModel>();
                            controllerObj.AddComponent<Wave.Essence.Controller.Model.ButtonEffect>();
#elif VIU_WAVEXR_ESSENCE_RENDERMODEL
                            m_modelObj.AddComponent<Wave.Essence.Controller.RenderModel>();
                            m_modelObj.AddComponent<Wave.Essence.Controller.ButtonEffect>();
#endif
                        }

                        m_modelObj.SetActive(true);
                    }
                    else
                    {
                        UpdateDefaultRenderModel(false);

                        if (m_modelObj == null)
                        {
                            m_modelObj = new GameObject("Model");
                            m_modelObj.transform.SetParent(hook.transform, false);
                            m_modelObj.SetActive(false);
#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL
#if VIU_WAVE_XRSDK_3_99_31_OR_NEWER
                            var pm = m_modelObj.AddComponent<PoseMode>();
                            pm.WhichHand = XR_Hand.NonDominant;
#endif
                            GameObject controllerObj = new GameObject("Controller");
                            controllerObj.transform.SetParent(m_modelObj.transform, false);
                            var rm = controllerObj.AddComponent<Wave.Essence.Controller.Model.RenderModel>();
                            rm.WhichHand = XR_Hand.NonDominant;
                            var be = controllerObj.AddComponent<Wave.Essence.Controller.Model.ButtonEffect>();
                            be.HandType = XR_Hand.NonDominant;
#elif VIU_WAVEXR_ESSENCE_RENDERMODEL
                            var rm = m_modelObj.AddComponent<Wave.Essence.Controller.RenderModel>();
                            rm.WhichHand = XR_Hand.NonDominant;
                            var be = m_modelObj.AddComponent<Wave.Essence.Controller.ButtonEffect>();
                            be.HandType = XR_Hand.NonDominant;
#endif
                        }

                        m_modelObj.SetActive(true);
                    }
                }
                else if (VRModule.GetDeviceState(m_index).deviceClass == VRModuleDeviceClass.TrackedHand
                         || VRModule.GetDeviceState(m_index).deviceClass == VRModuleDeviceClass.GenericTracker)
                {
                    //VIUWaveVRRenderModel currently doesn't support tracked hand
                    // Fallback to default model instead
                    UpdateDefaultRenderModel(true);

                    if (m_modelObj != null)
                    {
                        m_modelObj.SetActive(false);
                    }
                }
                else
                {
                    UpdateDefaultRenderModel(false);
                    // deacitvate object for render model
                    if (m_modelObj != null)
                    {
                        m_modelObj.SetActive(false);
                    }
                }
            }

            public override void CleanUpRenderModel()
            {
                base.CleanUpRenderModel();

                if (m_modelObj != null)
                {
                    UnityEngine.Object.Destroy(m_modelObj);
                    m_modelObj = null;
                    m_index = INVALID_DEVICE_INDEX;
                }
            }
        }

#if VIU_WAVEVR && UNITY_ANDROID
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
                if (hook.GetComponent<WaveVR_Render>() == null)
                {
                    hook.gameObject.AddComponent<WaveVR_Render>();
#if VIU_WAVEVR_3_1_3_OR_NEWER && UNITY_EDITOR
                    wvr.Interop.WVR_PostInit();
#endif
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
#elif VIU_WAVEVR_2_1_0_OR_NEWER
                                if (Interop.WVR_GetWaveRuntimeVersion() >= 3 && WaveVR_Controller.IsLeftHanded)
                                {
                                    loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Right;
                                }
                                else
                                {
                                    loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Left;
                                }
#else
                                loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Left;
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

            s_simulatedCtrlPosArray = new Vector3[s_type2index.Length];
        }

        public override bool ShouldActiveModule()
        {
#if VIU_WAVEVR_3_1_3_OR_NEWER && UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool("WaveVR/DirectPreview/Enable Direct Preview", false);
#elif !VIU_WAVEVR_2_1_0_OR_NEWER && UNITY_EDITOR
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

#if !UNITY_EDITOR && VIU_WAVEVR_3_1_0_OR_NEWER
            if (Object.FindObjectOfType<WaveVR_ButtonList>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<WaveVR_ButtonList>();

                var buttonList = VRModule.Instance.gameObject.GetComponent<WaveVR_ButtonList>();
                if (buttonList != null)
                {
                    buttonList.HmdButtons = new List<WaveVR_ButtonList.EHmdButtons>()
                    {
                        WaveVR_ButtonList.EHmdButtons.Enter
                    };
                    buttonList.DominantButtons = new List<WaveVR_ButtonList.EControllerButtons>()
                    {
                        WaveVR_ButtonList.EControllerButtons.Grip,
                        WaveVR_ButtonList.EControllerButtons.Menu,
                        WaveVR_ButtonList.EControllerButtons.Touchpad,
                        WaveVR_ButtonList.EControllerButtons.Trigger,
                        WaveVR_ButtonList.EControllerButtons.A_X,
                        WaveVR_ButtonList.EControllerButtons.B_Y
                    };
                    buttonList.NonDominantButtons = new List<WaveVR_ButtonList.EControllerButtons>()
                    {
                        WaveVR_ButtonList.EControllerButtons.Grip,
                        WaveVR_ButtonList.EControllerButtons.Menu,
                        WaveVR_ButtonList.EControllerButtons.Touchpad,
                        WaveVR_ButtonList.EControllerButtons.Trigger,
                        WaveVR_ButtonList.EControllerButtons.A_X,
                        WaveVR_ButtonList.EControllerButtons.B_Y
                    };
                }
            }
#elif !UNITY_EDITOR && VIU_WAVEVR_3_0_0_OR_NEWER
            if (Object.FindObjectOfType<WaveVR_ButtonList>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<WaveVR_ButtonList>();

                var buttonList = VRModule.Instance.gameObject.GetComponent<WaveVR_ButtonList>();
                if (buttonList != null)
                {
                    buttonList.HmdButtons = new List<WaveVR_ButtonList.EButtons>()
                    {
                        WaveVR_ButtonList.EButtons.HMDEnter
                    };
                    buttonList.DominantButtons = new List<WaveVR_ButtonList.EButtons>()
                    {
                        WaveVR_ButtonList.EButtons.Grip,
                        WaveVR_ButtonList.EButtons.Menu,
                        WaveVR_ButtonList.EButtons.Touchpad,
                        WaveVR_ButtonList.EButtons.Trigger
                    };
                    buttonList.NonDominantButtons = new List<WaveVR_ButtonList.EButtons>()
                    {
                        WaveVR_ButtonList.EButtons.Grip,
                        WaveVR_ButtonList.EButtons.Menu,
                        WaveVR_ButtonList.EButtons.Touchpad,
                        WaveVR_ButtonList.EButtons.Trigger
                    };
                }
            }
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
                var buttonAPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_A);
                var buttonBPressed = deviceInput.GetPress(WVR_InputId.WVR_InputId_Alias1_B);

                currState.SetButtonPress(VRModuleRawButton.System, systemPressed);
                currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuPressed);
                currState.SetButtonPress(VRModuleRawButton.Touchpad, touchpadPressed || dpadLeftPressed || dpadUpPressed || dpadRightPressed || dpadDownPressed);
                currState.SetButtonPress(VRModuleRawButton.Trigger, triggerPressed || digitalTriggerPressed);
                currState.SetButtonPress(VRModuleRawButton.Grip, gripPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadLeft, dpadLeftPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadUp, dpadUpPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadRight, dpadRightPressed);
                currState.SetButtonPress(VRModuleRawButton.DPadDown, dpadDownPressed);
                currState.SetButtonPress(VRModuleRawButton.A, buttonAPressed);
                currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, buttonBPressed);

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
                var gripAxis = deviceInput.GetAxis(WVR_InputId.WVR_InputId_Alias1_Grip);

                currState.SetAxisValue(VRModuleRawAxis.Trigger, triggerAxis.x);
                currState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, gripAxis.x);
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
                    string renderModelName;
                    if (!TryGetWVRStringParameter(s_index2type[(int)deviceIndex], "GetRenderModelName", out renderModelName))
                    {
                        renderModelName = "wvr_unknown_device";
                    }

                    currState.isConnected = true;
                    currState.deviceClass = s_type2class[(int)content.type];
                    currState.serialNumber = content.type.ToString();
                    currState.modelNumber = renderModelName;
                    currState.renderModelName = renderModelName;
                    currState.input2DType = VRModuleInput2DType.TouchpadOnly;

                    SetupKnownDeviceModel(currState);
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

        private void ApplyVirtualArmAndSimulateInput(IVRModuleDeviceStateRW ctrlState, IVRModuleDeviceStateRW headState, Vector3 handSideMultiplier)
        {
            if (!ctrlState.isConnected) { return; }
            if (!VIUSettings.waveVRAddVirtualArmTo3DoFController && !VIUSettings.simulateWaveVR6DofController) { return; }
            var deviceType = (int)s_index2type[ctrlState.deviceIndex];

#if !UNITY_EDITOR
            if (Interop.WVR_GetDegreeOfFreedom((WVR_DeviceType)deviceType) == WVR_NumDoF.WVR_NumDoF_6DoF) { return; }
#elif VIU_WAVEVR_3_1_3_OR_NEWER && UNITY_EDITOR
            if (!WaveVR.EnableSimulator || WVR_DirectPreview.WVR_GetDegreeOfFreedom_S(0) == (int)WVR_NumDoF.WVR_NumDoF_6DoF) { return; }
#elif VIU_WAVEVR_3_1_0_OR_NEWER && UNITY_EDITOR
            if (!WaveVR.EnableSimulator || WVR_Simulator.WVR_GetDegreeOfFreedom_S(0) == (int)WVR_NumDoF.WVR_NumDoF_6DoF) { return; }
#elif VIU_WAVEVR_2_1_0_OR_NEWER && UNITY_EDITOR
            if (!WaveVR.Instance.isSimulatorOn || WaveVR_Utils.WVR_GetDegreeOfFreedom_S() == (int)WVR_NumDoF.WVR_NumDoF_6DoF) { return; }
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

        private bool TryGetWVRStringParameter(WVR_DeviceType device, string paramName, out string result)
        {
            result = default(string);
            var resultLen = 0u;
            try
            {
                const int resultMaxLen = 128;
                var resultBuffer = resultMaxLen;
                var resultPtr = Marshal.AllocHGlobal(resultBuffer);
                var paramNamePtr = Marshal.StringToHGlobalAnsi(paramName);
                resultLen = Interop.WVR_GetParameters(device, paramNamePtr, resultPtr, resultMaxLen);

                if (resultLen > 0u)
                {
                    result = Marshal.PtrToStringAnsi(resultPtr);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return resultLen > 0u;
        }

#if VIU_WAVEVR_3_1_0_OR_NEWER
        public override void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85, float amplitude = 0.125f, float startSecondsFromNow = 0)
        {
            var deviceInput = WaveVR_Controller.Input(s_index2type[deviceIndex]);
            var intensity = default(WVR_Intensity);
            if (deviceInput != null)
            {
                if (0 <= amplitude || amplitude <= 0.2)
                {
                    intensity = WVR_Intensity.WVR_Intensity_Weak;
                }
                else if (0.2 < amplitude || amplitude <= 0.4)
                {
                    intensity = WVR_Intensity.WVR_Intensity_Light;
                }
                else if (0.4 < amplitude || amplitude <= 0.6)
                {
                    intensity = WVR_Intensity.WVR_Intensity_Normal;
                }
                else if (0.6 < amplitude || amplitude <= 0.8)
                {
                    intensity = WVR_Intensity.WVR_Intensity_Strong;
                }
                else if (0.8 < amplitude || amplitude <= 1)
                {
                    intensity = WVR_Intensity.WVR_Intensity_Severe;
                }
            }
            Interop.WVR_TriggerVibration(deviceInput.DeviceType, WVR_InputId.WVR_InputId_Alias1_Touchpad, (uint)(durationSeconds * 1000000), (uint)frequency, intensity);
        }
#endif
#endif
    }
}
