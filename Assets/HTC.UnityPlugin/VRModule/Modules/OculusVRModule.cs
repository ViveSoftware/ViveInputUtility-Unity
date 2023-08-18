//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using Object = UnityEngine.Object;
#if VIU_OCULUSVR
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Vive.OculusVRExtension;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using XRDevice = UnityEngine.VR.VRDevice;
using XRSettings = UnityEngine.VR.VRSettings;
#endif
#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public static readonly bool isOculusVRPluginDetected =
#if VIU_OCULUSVR
            true;
#else
            false;
#endif
        public static readonly bool isOculusVRDesktopSupported =
#if VIU_OCULUSVR_DESKTOP_SUPPORT
            true;
#else
            false;
#endif
        public static readonly bool isOculusVRAndroidSupported =
#if VIU_OCULUSVR_ANDROID_SUPPORT
            true;
#else
            false;
#endif

        public static readonly bool isOculusVRAvatarSupported =
#if VIU_OCULUSVR_AVATAR
            true;
#else
            false;
#endif
    }

    public sealed class OculusVRModule : VRModule.ModuleBase
    {
        // Should align OVRPlugin.SystemHeadset
        public enum OVRSystemHeadset
        {
            None = 0,

            GearVR_R320, // Note4 Innovator
            GearVR_R321, // S6 Innovator
            GearVR_R322, // Commercial 1
            GearVR_R323, // Commercial 2 (USB Type C)
            GearVR_R324, // Commercial 3 (USB Type C)
            GearVR_R325, // Commercial 4 (USB Type C)

            // Standalone headsets
            Oculus_Go = 7,
            Oculus_Quest,
            Oculus_Quest_2,
            Meta_Quest_Pro,

            // PC headsets
            Rift_DK1 = 0x1000,
            Rift_DK2,
            Rift_CV1,
            Rift_CB,
            Rift_S,
            Oculus_Link_Quest,
            Oculus_Link_Quest_2,
            Meta_Link_Quest_Pro,
        }

        public override int moduleOrder { get { return (int)DefaultModuleOrder.OculusVR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.OculusVR; } }

        public const string OCULUS_XR_LOADER_NAME = "Oculus Loader";
        public const string OCULUS_XR_LOADER_CLASS_NAME = "OculusLoader";
        private static OculusVRModule s_moduleInstance;

#if VIU_OCULUSVR
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
                if (hook.GetComponent<TrackedPoseDriver>() == null)
                {
                    hook.gameObject.AddComponent<TrackedPoseDriver>();
                }
#endif
            }
        }
#endif

#if VIU_OCULUSVR_AVATAR
        private class RenderModelCreator : RenderModelHook.DefaultRenderModelCreator
        {
            private static VIUOvrAvatar s_avatar;
            private uint m_index = INVALID_DEVICE_INDEX;
            private OculusHandRenderModel m_handModel;
            private VIUOvrAvatarComponent m_rightModel;
            private VIUOvrAvatarComponent m_leftModel;

            public override bool shouldActive
            {
                get
                {
#pragma warning disable 0162
                    if (!OculusHandRenderModel.SUPPORTED && !VIUOvrAvatar.SUPPORTED) { return false; }
#pragma warning restore 0162
                    if (!VIUSettings.EnableOculusSDKHandRenderModel && !VIUSettings.EnableOculusSDKControllerRenderModel) { return false; }
                    if (s_moduleInstance == null) { return false; }
                    if (!s_moduleInstance.isActivated) { return false; }
                    return true;
                }
            }

            public override void UpdateRenderModel()
            {
                if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }

                if (!VRModule.IsValidDeviceIndex(m_index))
                {
                    if (m_handModel != null) { m_handModel.gameObject.SetActive(false); }
                    if (m_rightModel != null) { m_rightModel.gameObject.SetActive(false); }
                    if (m_leftModel != null) { m_leftModel.gameObject.SetActive(false); }
                    return;
                }

                if (IsHand() && OculusHandRenderModel.SUPPORTED && VIUSettings.EnableOculusSDKHandRenderModel)
                {
                    var isLeftHand = m_index == s_leftHandIndex;
                    if (m_handModel == null)
                    {
                        var handObj = new GameObject(typeof(OculusHandRenderModel).Name);
                        handObj.transform.SetParent(hook.transform, false);
                        handObj.transform.localRotation *=
                            Quaternion.Inverse(
                                isLeftHand ?
                                Quaternion.LookRotation(Vector3.right, Vector3.down) :
                                Quaternion.LookRotation(Vector3.left, Vector3.up));
                        m_handModel = handObj.AddComponent<OculusHandRenderModel>();
                        m_handModel.Initialize(isLeftHand);
                    }

                    UpdateDefaultRenderModel(false);
                    m_handModel.gameObject.SetActive(true);
                    m_handModel.SetHand(isLeftHand);
                }
                else
                {
                    if (IsHand()) { UpdateDefaultRenderModel(true); }
                    if (m_handModel != null) { m_handModel.gameObject.SetActive(false); }
                }

                if (m_index == s_rightControllerIndex && VIUOvrAvatar.SUPPORTED && VIUSettings.EnableOculusSDKControllerRenderModel)
                {
                    LoadAvatar();
                    if (m_rightModel == null)
                    {
                        var go = new GameObject(typeof(VIUOvrAvatarComponent).Name);
                        go.transform.SetParent(hook.transform, false);
                        m_rightModel = go.AddComponent<VIUOvrAvatarComponent>();
                        m_rightModel.IsLeft = false;
                        m_rightModel.Owner = s_avatar;
                    }

                    UpdateDefaultRenderModel(false);
                    m_rightModel.gameObject.SetActive(true);
                }
                else
                {
                    if (m_index == s_rightControllerIndex) { UpdateDefaultRenderModel(true); }
                    if (m_rightModel != null) { m_rightModel.gameObject.SetActive(false); }
                }

                if (m_index == s_leftControllerIndex && VIUOvrAvatar.SUPPORTED && VIUSettings.EnableOculusSDKControllerRenderModel)
                {
                    LoadAvatar();
                    if (m_leftModel == null)
                    {
                        var go = new GameObject(typeof(VIUOvrAvatarComponent).Name);
                        go.transform.SetParent(hook.transform, false);
                        m_leftModel = go.AddComponent<VIUOvrAvatarComponent>();
                        m_leftModel.IsLeft = true;
                        m_leftModel.Owner = s_avatar;
                    }

                    UpdateDefaultRenderModel(false);
                    m_leftModel.gameObject.SetActive(true);
                }
                else
                {
                    if (m_index == s_leftControllerIndex) { UpdateDefaultRenderModel(true); }
                    if (m_leftModel != null) { m_leftModel.gameObject.SetActive(false); }
                }
            }

            public override void CleanUpRenderModel()
            {
                if (m_handModel != null)
                {
                    Object.Destroy(m_handModel.gameObject);
                    m_handModel = null;
                }

                if (m_rightModel != null)
                {
                    Object.Destroy(m_rightModel.gameObject);
                    m_rightModel = null;
                }

                if (m_leftModel != null)
                {
                    Object.Destroy(m_leftModel.gameObject);
                    m_leftModel = null;
                }

                m_index = INVALID_DEVICE_INDEX;
            }

            public static VIUOvrAvatar LoadAvatar()
            {
                if (s_avatar == null)
                {
                    var go = new GameObject(typeof(VIUOvrAvatar).Name);
                    s_avatar = go.AddComponent<VIUOvrAvatar>();
                }
                s_avatar.ShowHand = VIUSettings.EnableOculusSDKControllerRenderModelSkeleton;
                return s_avatar;
            }

            private bool IsHand()
            {
                return m_index == s_leftHandIndex || m_index == s_rightHandIndex;
            }
        }
#elif UNITY_2020_3_OR_NEWER && VIU_OCULUSVR_20_0_OR_NEWER
        private class RenderModelCreator : RenderModelHook.DefaultRenderModelCreator
        {
            private static bool s_mgrInit;
            private uint m_index = INVALID_DEVICE_INDEX;
            private bool m_isLeft;
            private bool m_isController;
            private bool m_isTrackedHand;

            private OVRControllerHelper m_controllerModel;
            private OculusHandRenderModel m_trackedHandModel;

            public override bool shouldActive
            {
                get
                {
                    if (!VIUSettings.EnableOculusSDKHandRenderModel && !VIUSettings.EnableOculusSDKControllerRenderModel) { return false; }
                    if (s_moduleInstance == null) { return false; }
                    if (!s_moduleInstance.isActivated) { return false; }
                    return true;
                }
            }

            public override void UpdateRenderModel()
            {
                if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }

                var isValidDevice = VRModule.IsValidDeviceIndex(m_index);
                if (!isValidDevice)
                {
                    m_isController = false;
                    m_isTrackedHand = false;
                }
                else
                {
                    var dvc = VRModule.GetDeviceState(m_index);
                    m_isLeft = dvc.deviceModel.IsLeft();
                    m_isController = dvc.deviceClass == VRModuleDeviceClass.Controller;
                    m_isTrackedHand = dvc.deviceClass == VRModuleDeviceClass.TrackedHand;
                }

                if (m_isController && VIUSettings.oculusVRControllerPrefab == null)
                {
                    m_isController = false;
                }

                if (m_isController)
                {
                    if (!s_mgrInit && !OVRManager.OVRManagerinitialized)
                    {
                        var go = new GameObject("OVRManager");
                        go.transform.SetParent(VRModule.GetInstanceGameObject().transform, false);
                        go.AddComponent<OVRManager>();
                        s_mgrInit = true;
                    }

                    if (m_controllerModel != null)
                    {
                        var modelIsLeft = m_controllerModel.m_controller == OVRInput.Controller.LTouch;
                        if (modelIsLeft != m_isLeft)
                        {
                            Object.Destroy(m_controllerModel.gameObject);
                            m_controllerModel = null;
                        }
                    }

                    if (m_controllerModel == null)
                    {
                        var go = Object.Instantiate(VIUSettings.oculusVRControllerPrefab);
                        go.name = VIUSettings.oculusVRControllerPrefab.name;
                        go.transform.SetParent(hook.transform, false);
                        go.SetActive(false);
                        m_controllerModel = go.GetComponent<OVRControllerHelper>();
                        m_controllerModel.m_controller = m_isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                    }

                    m_controllerModel.gameObject.SetActive(true);
                }
                else
                {
                    if (m_controllerModel != null)
                    {
                        m_controllerModel.gameObject.SetActive(false);
                    }
                }

                if (m_isTrackedHand)
                {
                    if (m_trackedHandModel == null)
                    {
                        var go = new GameObject(typeof(OculusHandRenderModel).Name);
                        go.transform.SetParent(hook.transform, false);
                        go.transform.localRotation *=
                            Quaternion.Inverse(
                                m_isLeft ?
                                Quaternion.LookRotation(Vector3.right, Vector3.down) :
                                Quaternion.LookRotation(Vector3.left, Vector3.up));
                        m_trackedHandModel = go.AddComponent<OculusHandRenderModel>();
                        m_trackedHandModel.Initialize(m_isLeft);
                    }

                    m_trackedHandModel.SetHand(m_isLeft);
                    m_trackedHandModel.gameObject.SetActive(true);
                }
                else
                {
                    if (m_trackedHandModel != null)
                    {
                        m_trackedHandModel.gameObject.SetActive(false);
                    }
                }

                UpdateDefaultRenderModel(!m_isController && !m_isTrackedHand);
            }
        }
#endif

#if VIU_OCULUSVR
        private const uint s_leftControllerIndex = 1;
        private const uint s_rightControllerIndex = 2;
        private const uint s_leftHandIndex = 7;
        private const uint s_rightHandIndex = 8;

        private static readonly OVRPlugin.Node[] s_index2node;
        private static readonly VRModuleDeviceClass[] s_index2class;
        private static readonly HandJointName[] s_ovrBoneIdToHandJointName;

        private OVRPlugin.SystemHeadset m_systemHeadsetType;
        private string m_systemHeadsetName;
        private OVRPlugin.TrackingOrigin m_prevTrackingSpace;

        private VRModule.SubmoduleBase.Collection submodules = new VRModule.SubmoduleBase.Collection();

        public override uint reservedDeviceIndex { get { return (uint)(s_index2node.Length - 1); } }

#if VIU_OCULUSVR_20_0_OR_NEWER
        private struct SkeletonData
        {
            public bool isLeft;
            public bool ready;
            public OVRPlugin.Skeleton data;
            public SkeletonData GetReady()
            {
                if (!ready)
                {
                    ready = OVRPlugin.GetSkeleton(isLeft ? OVRPlugin.SkeletonType.HandLeft : OVRPlugin.SkeletonType.HandRight, out data);
                }

                return this;
            }
        }

        private SkeletonData m_leftSkeletonData = new SkeletonData() { isLeft = true };
        private SkeletonData m_rightSkeletonData = new SkeletonData() { isLeft = false };
        private static readonly Quaternion m_leftRotOffset = Quaternion.LookRotation(Vector3.right, Vector3.down);
        private static readonly Quaternion m_rightRotOffset = Quaternion.LookRotation(Vector3.right, Vector3.up);
#endif

        static OculusVRModule()
        {
            s_index2node = new[]
            {
                OVRPlugin.Node.Head,
                OVRPlugin.Node.HandLeft,
                OVRPlugin.Node.HandRight,
                OVRPlugin.Node.TrackerZero,
                OVRPlugin.Node.TrackerOne,
                OVRPlugin.Node.TrackerTwo,
                OVRPlugin.Node.TrackerThree,
#if VIU_OCULUSVR_20_0_OR_NEWER
                OVRPlugin.Node.HandLeft,
                OVRPlugin.Node.HandRight,
#endif
            };

            s_index2class = new[]
            {
                VRModuleDeviceClass.HMD,
                VRModuleDeviceClass.Controller,
                VRModuleDeviceClass.Controller,
                VRModuleDeviceClass.TrackingReference,
                VRModuleDeviceClass.TrackingReference,
                VRModuleDeviceClass.TrackingReference,
                VRModuleDeviceClass.TrackingReference,
#if VIU_OCULUSVR_20_0_OR_NEWER
                VRModuleDeviceClass.TrackedHand,
                VRModuleDeviceClass.TrackedHand,
#endif
            };

#if VIU_OCULUSVR_20_0_OR_NEWER
            s_ovrBoneIdToHandJointName = new HandJointName[(int)OVRPlugin.BoneId.Max];

            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_WristRoot] = HandJointName.Wrist;

            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Thumb0] = HandJointName.ThumbTrapezium;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Thumb1] = HandJointName.ThumbMetacarpal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Thumb2] = HandJointName.ThumbProximal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Thumb3] = HandJointName.ThumbDistal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_ThumbTip] = HandJointName.ThumbTip;

            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Index1] = HandJointName.IndexProximal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Index2] = HandJointName.IndexIntermediate;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Index3] = HandJointName.IndexDistal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_IndexTip] = HandJointName.IndexTip;

            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Middle1] = HandJointName.MiddleProximal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Middle2] = HandJointName.MiddleIntermediate;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Middle3] = HandJointName.MiddleDistal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_MiddleTip] = HandJointName.MiddleTip;

            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Ring1] = HandJointName.RingProximal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Ring2] = HandJointName.RingIntermediate;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Ring3] = HandJointName.RingDistal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_RingTip] = HandJointName.RingTip;

            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Pinky0] = HandJointName.PinkyMetacarpal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Pinky1] = HandJointName.PinkyProximal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Pinky2] = HandJointName.PinkyIntermediate;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_Pinky3] = HandJointName.PinkyDistal;
            s_ovrBoneIdToHandJointName[(int)OVRPlugin.BoneId.Hand_PinkyTip] = HandJointName.PinkyTip;
#endif
        }

        public override bool ShouldActiveModule()
        {
            if (!VIUSettings.activateOculusVRModule) { return false; }
#pragma warning disable 0162
#if VIU_XR_GENERAL_SETTINGS
            return UnityXRModuleBase.HasActiveLoader(OCULUS_XR_LOADER_NAME);
#endif
#if UNITY_2019_3_OR_NEWER
            return false;
#else
            return XRSettings.enabled && XRSettings.loadedDeviceName == "Oculus";
#endif
#pragma warning restore 0162
        }

        public override void OnActivated()
        {
            Debug.Log("OculusVRModule activated.");

            submodules.ActivateAllModules();

            m_systemHeadsetType = OVRPlugin.GetSystemHeadsetType();
            m_systemHeadsetName = m_systemHeadsetType.ToString();
            m_prevTrackingSpace = OVRPlugin.GetTrackingOriginType();
            //UpdateTrackingSpaceType();

            EnsureDeviceStateLength((uint)s_index2node.Length);

            s_moduleInstance = this;
        }

        public override void OnDeactivated()
        {
            OVRPlugin.SetTrackingOriginType(m_prevTrackingSpace);
            s_moduleInstance = null;

            submodules.DeactivateAllModules();
        }

        public override void UpdateTrackingSpaceType()
        {
            OVRPlugin.TrackingOrigin demandTrackingOrigin;
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.RoomScale:
#if !VIU_OCULUSVR_19_0_OR_NEWER
                    if (OVRPlugin.GetSystemHeadsetType().Equals(OVRPlugin.SystemHeadset.Oculus_Go))
                    {
                        demandTrackingOrigin = OVRPlugin.TrackingOrigin.EyeLevel;
                    }
                    else
#endif
                    {
                        demandTrackingOrigin = OVRPlugin.TrackingOrigin.FloorLevel;
                    }
                    break;
                case VRModuleTrackingSpaceType.Stationary:
                    demandTrackingOrigin = OVRPlugin.TrackingOrigin.EyeLevel;
                    break;
                default:
                    return;
            }

            if (OVRPlugin.GetTrackingOriginType() != demandTrackingOrigin)
            {
                OVRPlugin.SetTrackingOriginType(demandTrackingOrigin);
            }
        }

        public override void Update()
        {
            UpdateTrackingSpaceType();

            // set physics update rate to vr render rate
            if (VRModule.lockPhysicsUpdateRateToRenderFrequency && Time.timeScale > 0.0f)
            {
                // FIXME: VRDevice.refreshRate returns zero in Unity 5.6.0 or older version
#if !UNITY_5_6_0 && UNITY_5_6_OR_NEWER
                Time.fixedDeltaTime = 1f / XRDevice.refreshRate;
#else
                Time.fixedDeltaTime = 1f / 90f;
#endif
            }
        }

        public override uint GetLeftControllerDeviceIndex()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            if (TryGetValidDeviceState(s_leftHandIndex, out prevState, out currState) && currState.isConnected) { return s_leftHandIndex; }
            if (TryGetValidDeviceState(s_leftControllerIndex, out prevState, out currState) && currState.isConnected) { return s_leftControllerIndex; }
            return INVALID_DEVICE_INDEX;
        }

        public override uint GetRightControllerDeviceIndex()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            if (TryGetValidDeviceState(s_rightHandIndex, out prevState, out currState) && currState.isConnected) { return s_rightHandIndex; }
            if (TryGetValidDeviceState(s_rightControllerIndex, out prevState, out currState) && currState.isConnected) { return s_rightControllerIndex; }
            return INVALID_DEVICE_INDEX;
        }

        private static RigidPose ToPose(OVRPlugin.Posef value)
        {
            var ovrPose = value.ToOVRPose();
            return new RigidPose(ovrPose.position, ovrPose.orientation);
        }

        private static Quaternion leftHandRotOffset = Quaternion.Inverse(Quaternion.LookRotation(Vector3.right, Vector3.down));
        private static Quaternion rightHandRotOffset = Quaternion.Inverse(Quaternion.LookRotation(Vector3.right, Vector3.down));
        private static RigidPose FromHandPose(OVRPlugin.Posef value, bool isLeft)
        {
            return FromHandPose(value);
            //var ovrPose = value.ToOVRPose();
            ////return new RigidPose(ovrPose.position, ovrPose.orientation * (isLeft ? leftHandRotOffset : rightHandRotOffset));
            //return new RigidPose(ovrPose.position, ovrPose.orientation);
        }
        private static RigidPose FromHandPose(OVRPlugin.Posef value)
        {
            return new RigidPose()
            {
                pos = value.Position.FromFlippedZVector3f(),
                rot = value.Orientation.FromFlippedZQuatf(),
            };
        }

        public override void BeforeRenderUpdate()
        {
            FlushDeviceState();

            for (uint i = 0u, imax = (uint)s_index2node.Length; i < imax; ++i)
            {
                var node = s_index2node[i];
                var deviceClass = s_index2class[i];

                if (node == OVRPlugin.Node.None) { continue; }

                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                EnsureValidDeviceState(i, out prevState, out currState);

#if VIU_OCULUSVR_20_0_OR_NEWER
                var handState = new OVRPlugin.HandState();
                if (deviceClass == VRModuleDeviceClass.TrackedHand)
                {
                    if (node == OVRPlugin.Node.HandLeft)
                    {
                        OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref handState);
                    }
                    else
                    {
                        OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref handState);
                    }

                    if ((handState.Status & (OVRPlugin.HandStatus.HandTracked | OVRPlugin.HandStatus.InputStateValid)) == 0)
                    {
                        if (prevState.isConnected)
                        {
                            Debug.Log("[VIU][OculusVRModule] " + prevState.deviceModel + " device disconnected.");
                            currState.Reset();
                        }
                        continue;
                    }
                }
                else
#endif
                if (!OVRPlugin.GetNodePresent(node))
                {
                    if (prevState.isConnected)
                    {
                        Debug.Log("[VIU][OculusVRModule] " + prevState.deviceModel + " device disconnected.");
                        currState.Reset();
                    }
                    continue;
                }

                // update device connected state
                if (!prevState.isConnected)
                {
                    var deviceName = m_systemHeadsetName + " " + node + " " + deviceClass;

                    currState.isConnected = true;
                    currState.deviceClass = deviceClass;
                    // FIXME: how to get device id from OVRPlugin?
                    currState.modelNumber = deviceName;
                    currState.renderModelName = deviceName;
                    currState.serialNumber = deviceName;

                    switch (deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            currState.deviceModel = VRModuleDeviceModel.OculusHMD;
                            break;
                        case VRModuleDeviceClass.TrackingReference:
                            currState.deviceModel = VRModuleDeviceModel.OculusSensor;
                            break;
                        case VRModuleDeviceClass.Controller:
                            switch ((OVRSystemHeadset)m_systemHeadsetType)
                            {
                                case OVRSystemHeadset.Oculus_Go:
                                    currState.deviceModel = VRModuleDeviceModel.OculusGoController;
                                    currState.input2DType = VRModuleInput2DType.TouchpadOnly;
                                    break;
                                case OVRSystemHeadset.GearVR_R320:
                                case OVRSystemHeadset.GearVR_R321:
                                case OVRSystemHeadset.GearVR_R322:
                                case OVRSystemHeadset.GearVR_R323:
                                case OVRSystemHeadset.GearVR_R324:
                                case OVRSystemHeadset.GearVR_R325:
                                    currState.deviceModel = VRModuleDeviceModel.OculusGearVrController;
                                    currState.input2DType = VRModuleInput2DType.TouchpadOnly;
                                    break;
                                case OVRSystemHeadset.Rift_DK1:
                                case OVRSystemHeadset.Rift_DK2:
                                case OVRSystemHeadset.Rift_CV1:
                                    if (node == OVRPlugin.Node.HandLeft)
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusTouchLeft;
                                    }
                                    else
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusTouchRight;
                                    }
                                    currState.input2DType = VRModuleInput2DType.JoystickOnly;
                                    break;
                                case OVRSystemHeadset.Oculus_Link_Quest:
                                case OVRSystemHeadset.Oculus_Quest:
                                case OVRSystemHeadset.Rift_S:
                                    if (node == OVRPlugin.Node.HandLeft)
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusQuestControllerLeft;
                                    }
                                    else
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusQuestControllerRight;
                                    }
                                    currState.input2DType = VRModuleInput2DType.JoystickOnly;
                                    break;
                                case OVRSystemHeadset.Oculus_Link_Quest_2:
                                case OVRSystemHeadset.Oculus_Quest_2:
                                    if (node == OVRPlugin.Node.HandLeft)
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusQuest2ControllerLeft;
                                    }
                                    else
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusQuest2ControllerRight;
                                    }
                                    currState.input2DType = VRModuleInput2DType.JoystickOnly;
                                    break;
                                case OVRSystemHeadset.Meta_Link_Quest_Pro:
                                case OVRSystemHeadset.Meta_Quest_Pro:
                                    if (node == OVRPlugin.Node.HandLeft)
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusTouchProLeft;
                                    }
                                    else
                                    {
                                        currState.deviceModel = VRModuleDeviceModel.OculusTouchProRight;
                                    }
                                    currState.input2DType = VRModuleInput2DType.JoystickOnly;
                                    break;
                            }
                            break;
                        case VRModuleDeviceClass.TrackedHand:
                            if (node == OVRPlugin.Node.HandLeft)
                            {
                                currState.deviceModel = VRModuleDeviceModel.OculusTrackedHandLeft;
                            }
                            else
                            {
                                currState.deviceModel = VRModuleDeviceModel.OculusTrackedHandRight;
                            }
                            break;
                    }

                    Debug.Log("[VIU][OculusVRModule] " + currState.deviceModel + " device connected. deviceName=\"" + deviceName + "\"");
                }

                // update device pose
                if (deviceClass != VRModuleDeviceClass.TrackedHand)
                {
                    currState.pose = ToPose(OVRPlugin.GetNodePose(node, OVRPlugin.Step.Render));
                    currState.velocity = OVRPlugin.GetNodeVelocity(node, OVRPlugin.Step.Render).FromFlippedZVector3f();
                    currState.angularVelocity = OVRPlugin.GetNodeAngularVelocity(node, OVRPlugin.Step.Render).FromFlippedZVector3f();
                    currState.isPoseValid = currState.pose != RigidPose.identity;
                }
                currState.isConnected = true;

                // update device input
                switch (currState.deviceModel)
                {
                    case VRModuleDeviceModel.OculusGoController:
                    case VRModuleDeviceModel.OculusGearVrController:
#if !VIU_OCULUSVR_19_0_OR_NEWER
                        switch (node)
                        {
                            case OVRPlugin.Node.HandLeft:
                                {
                                    var ctrlState = OVRPlugin.GetControllerState4((uint)OVRPlugin.Controller.LTrackedRemote);

                                    currState.SetButtonPress(VRModuleRawButton.Touchpad, (ctrlState.Buttons & (uint)OVRInput.RawButton.LTouchpad) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, (ctrlState.Buttons & (uint)OVRInput.RawButton.Back) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.Trigger, (ctrlState.Buttons & (uint)(OVRInput.RawButton.A | OVRInput.RawButton.LIndexTrigger)) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadLeft, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadLeft) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadUp, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadUp) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadRight, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadRight) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadDown, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadDown) != 0u);

                                    currState.SetButtonTouch(VRModuleRawButton.Touchpad, (ctrlState.Touches & (uint)OVRInput.RawTouch.LTouchpad) != 0u);

                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadX, ctrlState.LTouchpad.x);
                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadY, ctrlState.LTouchpad.y);
                                }
                                break;
                            case OVRPlugin.Node.HandRight:
                            default:
                                {
                                    var ctrlState = OVRPlugin.GetControllerState4((uint)OVRPlugin.Controller.RTrackedRemote);

                                    currState.SetButtonPress(VRModuleRawButton.Touchpad, (ctrlState.Buttons & unchecked((uint)OVRInput.RawButton.RTouchpad)) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, (ctrlState.Buttons & (uint)OVRInput.RawButton.Back) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.Trigger, (ctrlState.Buttons & (uint)(OVRInput.RawButton.A | OVRInput.RawButton.RIndexTrigger)) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadLeft, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadLeft) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadUp, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadUp) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadRight, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadRight) != 0u);
                                    currState.SetButtonPress(VRModuleRawButton.DPadDown, (ctrlState.Buttons & (uint)OVRInput.RawButton.DpadDown) != 0u);

                                    currState.SetButtonTouch(VRModuleRawButton.Touchpad, (ctrlState.Touches & unchecked((uint)OVRInput.RawTouch.RTouchpad)) != 0u);

                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadX, ctrlState.RTouchpad.x);
                                    currState.SetAxisValue(VRModuleRawAxis.TouchpadY, ctrlState.RTouchpad.y);
                                }
                                break;
                        }
                        break;
#endif
                    case VRModuleDeviceModel.OculusTouchLeft:
                    case VRModuleDeviceModel.OculusQuestControllerLeft:
                    case VRModuleDeviceModel.OculusQuest2ControllerLeft:
                    case VRModuleDeviceModel.OculusTouchProLeft:
                        {
                            var ctrlState = OVRPlugin.GetControllerState((uint)OVRPlugin.Controller.LTouch);

                            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, (ctrlState.Buttons & (uint)OVRInput.RawButton.Y) != 0u);
                            currState.SetButtonPress(VRModuleRawButton.A, (ctrlState.Buttons & (uint)OVRInput.RawButton.X) != 0u);
                            currState.SetButtonPress(VRModuleRawButton.Touchpad, (ctrlState.Buttons & (uint)OVRInput.RawButton.LThumbstick) != 0u);
                            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(currState.GetButtonPress(VRModuleRawButton.Trigger), ctrlState.LIndexTrigger, 0.55f, 0.45f));
                            currState.SetButtonPress(VRModuleRawButton.Grip, AxisToPress(currState.GetButtonPress(VRModuleRawButton.Grip), ctrlState.LHandTrigger, 0.55f, 0.45f));
                            currState.SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(currState.GetButtonPress(VRModuleRawButton.CapSenseGrip), ctrlState.LHandTrigger, 0.55f, 0.45f));
                            currState.SetButtonPress(VRModuleRawButton.System, (ctrlState.Buttons & (uint)OVRInput.RawButton.Start) != 0u);

                            currState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, (ctrlState.Touches & (uint)OVRInput.RawTouch.Y) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.A, (ctrlState.Touches & (uint)OVRInput.RawTouch.X) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.Touchpad, (ctrlState.Touches & (uint)OVRInput.RawTouch.LThumbstick) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.Trigger, (ctrlState.Touches & (uint)OVRInput.RawTouch.LIndexTrigger) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.Grip, AxisToPress(currState.GetButtonTouch(VRModuleRawButton.Grip), ctrlState.LHandTrigger, 0.25f, 0.20f));
                            currState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(currState.GetButtonTouch(VRModuleRawButton.CapSenseGrip), ctrlState.LHandTrigger, 0.25f, 0.20f));

                            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, ctrlState.LThumbstick.x);
                            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, ctrlState.LThumbstick.y);
                            currState.SetAxisValue(VRModuleRawAxis.Trigger, ctrlState.LIndexTrigger);
                            currState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, ctrlState.LHandTrigger);
                            break;
                        }
                    case VRModuleDeviceModel.OculusTouchRight:
                    case VRModuleDeviceModel.OculusQuestControllerRight:
                    case VRModuleDeviceModel.OculusQuest2ControllerRight:
                    case VRModuleDeviceModel.OculusTouchProRight:
                        {
                            var ctrlState = OVRPlugin.GetControllerState((uint)OVRPlugin.Controller.RTouch);

                            currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, (ctrlState.Buttons & (uint)OVRInput.RawButton.B) != 0u);
                            currState.SetButtonPress(VRModuleRawButton.A, (ctrlState.Buttons & (uint)OVRInput.RawButton.A) != 0u);
                            currState.SetButtonPress(VRModuleRawButton.Touchpad, (ctrlState.Buttons & (uint)OVRInput.RawButton.RThumbstick) != 0u);
                            currState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(currState.GetButtonPress(VRModuleRawButton.Trigger), ctrlState.RIndexTrigger, 0.55f, 0.45f));
                            currState.SetButtonPress(VRModuleRawButton.Grip, AxisToPress(currState.GetButtonPress(VRModuleRawButton.Grip), ctrlState.RHandTrigger, 0.55f, 0.45f));
                            currState.SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(currState.GetButtonPress(VRModuleRawButton.CapSenseGrip), ctrlState.RHandTrigger, 0.55f, 0.45f));

                            currState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, (ctrlState.Touches & (uint)OVRInput.RawTouch.B) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.A, (ctrlState.Touches & (uint)OVRInput.RawTouch.A) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.Touchpad, (ctrlState.Touches & (uint)OVRInput.RawTouch.RThumbstick) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.Trigger, (ctrlState.Touches & (uint)OVRInput.RawTouch.RIndexTrigger) != 0u);
                            currState.SetButtonTouch(VRModuleRawButton.Grip, AxisToPress(currState.GetButtonTouch(VRModuleRawButton.Grip), ctrlState.RHandTrigger, 0.25f, 0.20f));
                            currState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(currState.GetButtonTouch(VRModuleRawButton.CapSenseGrip), ctrlState.RHandTrigger, 0.25f, 0.20f));

                            currState.SetAxisValue(VRModuleRawAxis.TouchpadX, ctrlState.RThumbstick.x);
                            currState.SetAxisValue(VRModuleRawAxis.TouchpadY, ctrlState.RThumbstick.y);
                            currState.SetAxisValue(VRModuleRawAxis.Trigger, ctrlState.RIndexTrigger);
                            currState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, ctrlState.RHandTrigger);
                            break;
                        }
                    case VRModuleDeviceModel.OculusTrackedHandLeft:
                    case VRModuleDeviceModel.OculusTrackedHandRight:
#if VIU_OCULUSVR_20_0_OR_NEWER
                        {
                            var isLeft = node == OVRPlugin.Node.HandLeft;
                            if ((handState.Status & OVRPlugin.HandStatus.InputStateValid) != 0)
                            {
                                currState.SetButtonPress(VRModuleRawButton.GestureIndexPinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Index) == OVRPlugin.HandFingerPinch.Index);
                                currState.SetButtonPress(VRModuleRawButton.GestureMiddlePinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Middle) == OVRPlugin.HandFingerPinch.Middle);
                                currState.SetButtonPress(VRModuleRawButton.GestureRingPinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Ring) == OVRPlugin.HandFingerPinch.Ring);
                                currState.SetButtonPress(VRModuleRawButton.GesturePinkyPinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Pinky) == OVRPlugin.HandFingerPinch.Pinky);

                                currState.SetButtonTouch(VRModuleRawButton.GestureIndexPinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Index) == OVRPlugin.HandFingerPinch.Index);
                                currState.SetButtonTouch(VRModuleRawButton.GestureMiddlePinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Middle) == OVRPlugin.HandFingerPinch.Middle);
                                currState.SetButtonTouch(VRModuleRawButton.GestureRingPinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Ring) == OVRPlugin.HandFingerPinch.Ring);
                                currState.SetButtonTouch(VRModuleRawButton.GesturePinkyPinch, (handState.Pinches & OVRPlugin.HandFingerPinch.Pinky) == OVRPlugin.HandFingerPinch.Pinky);

                                currState.SetAxisValue(VRModuleRawAxis.IndexPinch, handState.PinchStrength[(int)HandFinger.Index]);
                                currState.SetAxisValue(VRModuleRawAxis.MiddlePinch, handState.PinchStrength[(int)HandFinger.Middle]);
                                currState.SetAxisValue(VRModuleRawAxis.RingPinch, handState.PinchStrength[(int)HandFinger.Ring]);
                                currState.SetAxisValue(VRModuleRawAxis.PinkyPinch, handState.PinchStrength[(int)HandFinger.Pinky]);

                                // Map index pinch to trigger
                                currState.SetButtonPress(VRModuleRawButton.Trigger, currState.GetButtonPress(VRModuleRawButton.GestureIndexPinch));
                                currState.SetButtonTouch(VRModuleRawButton.Trigger, currState.GetButtonTouch(VRModuleRawButton.GestureIndexPinch));
                                currState.SetAxisValue(VRModuleRawAxis.Trigger, currState.GetAxisValue(VRModuleRawAxis.IndexPinch));
                            }

                            if ((handState.Status & OVRPlugin.HandStatus.HandTracked) != 0)
                            {
                                var rotOffset = isLeft ? m_leftRotOffset : m_rightRotOffset;
                                var rotOffsetInv = Quaternion.Inverse(rotOffset);

                                currState.isPoseValid = true;
                                currState.pose = new RigidPose()
                                {
                                    pos = handState.RootPose.Position.FromFlippedZVector3f(),
                                    rot = handState.RootPose.Orientation.FromFlippedZQuatf() * rotOffsetInv,
                                };

                                // TODO: PointerPose?
                                //currState.handJoints[HandJointName.Palm] = new JointPose()
                                //{
                                //    isValid = true,
                                //    pose = new RigidPose()
                                //    {
                                //        pos = handState.PointerPose.Position.FromFlippedZVector3f(),
                                //        rot = handState.PointerPose.Orientation.FromFlippedZQuatf() * rotOffsetInv,
                                //    },
                                //};

                                var skeletonData = isLeft ? m_leftSkeletonData.GetReady() : m_rightSkeletonData.GetReady();
                                if (skeletonData.ready)
                                {
                                    for (var boneId = OVRSkeleton.BoneId.Hand_Start; boneId < OVRSkeleton.BoneId.Hand_End; ++boneId)
                                    {
                                        var bone = skeletonData.data.Bones[(int)boneId];

                                        RigidPose parentPose;
                                        if (bone.ParentBoneIndex == (short)OVRPlugin.BoneId.Invalid)
                                        {
                                            parentPose = currState.pose;
                                        }
                                        else
                                        {
                                            parentPose = currState.handJoints[s_ovrBoneIdToHandJointName[bone.ParentBoneIndex]].pose;
                                        }

                                        currState.handJoints[s_ovrBoneIdToHandJointName[(int)boneId]] = new JointPose()
                                        {
                                            isValid = true,
                                            pose = parentPose *
                                            new RigidPose()
                                            {
                                                pos = Vector3.zero,
                                                rot = rotOffset,
                                            } *
                                            new RigidPose()
                                            {
                                                pos = bone.Pose.Position.FromFlippedZVector3f() * handState.HandScale,
                                                rot = handState.BoneRotations[(int)boneId].FromFlippedZQuatf() * rotOffsetInv,
                                            },
                                        };
                                    }
                                }
                            }
                            else
                            {
                                currState.isPoseValid = false;
                                currState.pose = RigidPose.identity;
                            }
                        }
#endif
                        break;
                }

                //if (!prevState.isPoseValid)
                //{
                //    if (currState.isPoseValid)
                //    {
                //        Debug.Log("[VIU][OculusVRModule] " + currState.deviceModel + " pose valid.");
                //    }
                //}
                //else
                //{
                //    if (!currState.isPoseValid)
                //    {
                //        Debug.Log("[VIU][OculusVRModule] " + prevState.deviceModel + " pose invalid.");
                //    }
                //}
            }

            submodules.UpdateAllModulesActivity();
            submodules.UpdateModulesDeviceConnectionAndPoses();
            submodules.UpdateModulesDeviceInput();

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
            ProcessDeviceInputChanged();
        }
#endif
    }
}
