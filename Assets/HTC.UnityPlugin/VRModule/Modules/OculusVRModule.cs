//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using HTC.UnityPlugin.Utility;
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

            // PC headsets
            Rift_DK1 = 0x1000,
            Rift_DK2,
            Rift_CV1,
            Rift_CB,
            Rift_S,
            Oculus_Link_Quest,
            Oculus_Link_Quest_2,
        }

        public override int moduleOrder { get { return (int)DefaultModuleOrder.OculusVR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.OculusVR; } }

        public const string OCULUS_XR_LOADER_NAME = "Oculus Loader";
        public const string OCULUS_XR_LOADER_CLASS_NAME = "OculusLoader";

#if VIU_OCULUSVR
        private class Skeleton
        {
            private const string LeftHandSkeletonName = "LeftHandSkeleton";
            private const string RightHandSkeletonName = "RightHandSkeleton";

            private static readonly Quaternion WristFixupRotation = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);
            private static readonly Quaternion LeftHandOpenXRFixRotation = Quaternion.Euler(0.0f, -90.0f, 180.0f);
            private static readonly Quaternion RightHandOpenXRFixRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

            public readonly OVRPlugin.SkeletonType Handedness;
            public readonly Transform Root;
            public readonly Transform[] Bones = new Transform[(int) OVRPlugin.BoneId.Max];

            public Skeleton(OVRPlugin.SkeletonType handedness)
            {
                Handedness = handedness;

                string name = Handedness == OVRPlugin.SkeletonType.HandLeft ? LeftHandSkeletonName : RightHandSkeletonName;
                Root = new GameObject(name).transform;
                OVRPlugin.Skeleton ovrSkeleton;
                if (OVRPlugin.GetSkeleton(Handedness, out ovrSkeleton))
                {
                    for (int i = 0; i < (int) OVRSkeleton.BoneId.Hand_End; i++)
                    {
                        OVRSkeleton.BoneId id = (OVRSkeleton.BoneId) ovrSkeleton.Bones[i].Id;
                        GameObject boneObj = new GameObject(id.ToString());
                        Bones[i] = boneObj.transform;

                        Vector3 pos = ovrSkeleton.Bones[i].Pose.Position.FromFlippedXVector3f();
                        Quaternion rot = ovrSkeleton.Bones[i].Pose.Orientation.FromFlippedXQuatf();
                        Bones[i].localPosition = pos;
                        Bones[i].localRotation = rot;
                    }

                    for (int i = 0; i < (int) OVRSkeleton.BoneId.Hand_End; i++)
                    {
                        int parentIndex = ovrSkeleton.Bones[i].ParentBoneIndex;
                        if (parentIndex == (int) OVRPlugin.BoneId.Invalid)
                        {
                            Bones[i].SetParent(Root);
                        }
                        else
                        {
                            Bones[i].SetParent(Bones[parentIndex]);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("OvrSkeleton not found: " + Handedness);
                }
            }

            public void Update(OVRPlugin.HandState handState)
            {
                Root.localPosition = handState.RootPose.Position.FromFlippedZVector3f();
                Root.localRotation = handState.RootPose.Orientation.FromFlippedZQuatf();
                Root.localScale = new Vector3(handState.HandScale, handState.HandScale, handState.HandScale);

                OVRPlugin.Skeleton ovrSkeleton;
                if (OVRPlugin.GetSkeleton(Handedness, out ovrSkeleton))
                {
                    for (int i = 0; i < (int) OVRSkeleton.BoneId.Hand_End; i++)
                    {
                        Vector3 pos = ovrSkeleton.Bones[i].Pose.Position.FromFlippedXVector3f();
                        Quaternion rot = handState.BoneRotations[i].FromFlippedXQuatf();
                        Bones[i].localPosition = pos;
                        Bones[i].localRotation = rot;

                        if (i == (int) OVRSkeleton.BoneId.Hand_WristRoot)
                        {
                            Bones[i].localRotation *= WristFixupRotation;
                        }
                    }
                }
            }

            public Quaternion GetOpenXRRotation(OVRPlugin.BoneId boneId)
            {
                Quaternion fixQuat = Handedness == OVRPlugin.SkeletonType.HandLeft ? LeftHandOpenXRFixRotation : RightHandOpenXRFixRotation;
                return Bones[(int) boneId].rotation * fixQuat;
            }
        }

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

#if VIU_OCULUSVR_AVATAR && VIU_OCULUSVR_1_32_0_OR_NEWER
        private class RenderModelCreator : RenderModelHook.RenderModelCreator
        {
            private uint m_index = INVALID_DEVICE_INDEX;
            private VIUOculusVRRenderModel m_controllerModel;
            private OculusHandRenderModel m_handModel;

            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void UpdateRenderModel()
            {
                if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }

                DisableAllModels();
                if (!VRModule.IsValidDeviceIndex(m_index))
                {
                    return;
                }

                if (IsHand())
                {
                    bool isLeftHand = m_index == s_leftHandIndex;
                    if (m_handModel == null)
                    {
                        GameObject handObj = new GameObject("OculusHandModel");
                        handObj.transform.SetParent(hook.transform.parent.parent, false);
                        m_handModel = handObj.AddComponent<OculusHandRenderModel>();
                        m_handModel.Initialize(isLeftHand);
                    }

                    m_handModel.gameObject.SetActive(true);
                    m_handModel.SetHand(isLeftHand);
                }
                else
                {
                    // create object for render model
                    if (m_controllerModel == null)
                    {
                        var go = new GameObject("OculusControllerModel");
                        go.transform.SetParent(hook.transform, false);
                        m_controllerModel = go.AddComponent<VIUOculusVRRenderModel>();
                    }

                    // set render model index
                    m_controllerModel.gameObject.SetActive(true);
                    m_controllerModel.shaderOverride = hook.overrideShader;
#if VIU_OCULUSVR_1_32_0_OR_NEWER && !VIU_OCULUSVR_1_37_0_OR_NEWER
                    m_controllerModel.gameObject.AddComponent<OvrAvatarTouchController>();
#endif
                    m_controllerModel.SetDeviceIndex(m_index);
                }
            }

            public override void CleanUpRenderModel()
            {
                if (m_handModel != null)
                {
                    Object.Destroy(m_handModel.gameObject);
                    m_handModel = null;
                }

                if (m_controllerModel != null)
                {
                    Object.Destroy(m_controllerModel.gameObject);
                    m_controllerModel = null;
                }

                m_index = INVALID_DEVICE_INDEX;
            }

            private void DisableAllModels()
            {
                if (m_controllerModel != null)
                {
                    m_controllerModel.gameObject.SetActive(false);
                }

                if (m_handModel != null)
                {
                    m_handModel.gameObject.SetActive(false);
                }
            }

            private bool IsHand()
            {
                return m_index == s_leftHandIndex || m_index == s_rightHandIndex;
            }
        }
#endif

        private const uint s_leftControllerIndex = 1;
        private const uint s_rightControllerIndex = 2;
        private const uint s_leftHandIndex = 7;
        private const uint s_rightHandIndex = 8;

        private static OculusVRModule s_moduleInstance;
        private static readonly OVRPlugin.Node[] s_index2node;
        private static readonly VRModuleDeviceClass[] s_index2class;
        private static readonly HandJointName[] s_ovrBoneIdToHandJointName;

        private OVRPlugin.SystemHeadset m_systemHeadsetType;
        private string m_systemHeadsetName;
        private OVRPlugin.TrackingOrigin m_prevTrackingSpace;

        private bool m_isLeftHandTracked;
        private bool m_isRightHandTracked;

        private Skeleton m_leftHandSkeleton;
        private Skeleton m_rightHandSkeleton;

        private Skeleton leftHandSkeleton
        {
            get
            {
                if (m_leftHandSkeleton == null)
                {
                    m_leftHandSkeleton = new Skeleton(OVRPlugin.SkeletonType.HandLeft);
                }

                return m_leftHandSkeleton;
            }
        }

        private Skeleton rightHandSkeleton
        {
            get
            {
                if (m_rightHandSkeleton == null)
                {
                    m_rightHandSkeleton = new Skeleton(OVRPlugin.SkeletonType.HandRight);
                }

                return m_rightHandSkeleton;
            }
        }

        static OculusVRModule()
        {
            s_index2node = new []
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

            s_index2class = new []
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
            s_ovrBoneIdToHandJointName = new HandJointName[(int) OVRPlugin.BoneId.Max];

            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_WristRoot] = HandJointName.Wrist;

            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Thumb0] = HandJointName.ThumbTrapezium;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Thumb1] = HandJointName.ThumbMetacarpal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Thumb2] = HandJointName.ThumbProximal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Thumb3] = HandJointName.ThumbDistal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_ThumbTip] = HandJointName.ThumbTip;

            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Index1] = HandJointName.IndexProximal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Index2] = HandJointName.IndexIntermediate;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Index3] = HandJointName.IndexDistal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_IndexTip] = HandJointName.IndexTip;

            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Middle1] = HandJointName.MiddleProximal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Middle2] = HandJointName.MiddleIntermediate;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Middle3] = HandJointName.MiddleDistal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_MiddleTip] = HandJointName.MiddleTip;

            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Ring1] = HandJointName.RingProximal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Ring2] = HandJointName.RingIntermediate;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Ring3] = HandJointName.RingDistal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_RingTip] = HandJointName.RingTip;

            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Pinky0] = HandJointName.PinkyMetacarpal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Pinky1] = HandJointName.PinkyProximal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Pinky2] = HandJointName.PinkyIntermediate;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_Pinky3] = HandJointName.PinkyDistal;
            s_ovrBoneIdToHandJointName[(int) OVRPlugin.BoneId.Hand_PinkyTip] = HandJointName.PinkyTip;
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
            Debug.Log("[VIU][OculusVRModule] OnActivated");

            m_systemHeadsetType = OVRPlugin.GetSystemHeadsetType();
            m_systemHeadsetName = m_systemHeadsetType.ToString();
            m_prevTrackingSpace = OVRPlugin.GetTrackingOriginType();
            UpdateTrackingSpaceType();

            EnsureDeviceStateLength((uint) s_index2node.Length);

            s_moduleInstance = this;
        }

        public override void OnDeactivated()
        {
            OVRPlugin.SetTrackingOriginType(m_prevTrackingSpace);
        }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.RoomScale:
#if !VIU_OCULUSVR_19_0_OR_NEWER
                    if (OVRPlugin.GetSystemHeadsetType().Equals(OVRPlugin.SystemHeadset.Oculus_Go))
                    {
                        OVRPlugin.SetTrackingOriginType(OVRPlugin.TrackingOrigin.EyeLevel);
                    }
                    else
#endif
                    {
                        OVRPlugin.SetTrackingOriginType(OVRPlugin.TrackingOrigin.FloorLevel);
                    }
                    break;
                case VRModuleTrackingSpaceType.Stationary:
                    OVRPlugin.SetTrackingOriginType(OVRPlugin.TrackingOrigin.EyeLevel);
                    break;
            }
        }

        public override void Update()
        {
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
            if (TryGetValidDeviceState(s_leftHandIndex, out prevState, out currState)) { return s_leftHandIndex; }
            if (TryGetValidDeviceState(s_leftControllerIndex, out prevState, out currState) && currState.isConnected) { return s_leftControllerIndex; }
            return INVALID_DEVICE_INDEX;
        }

        public override uint GetRightControllerDeviceIndex()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            if (TryGetValidDeviceState(s_rightHandIndex, out prevState, out currState)) { return s_rightHandIndex; }
            if (TryGetValidDeviceState(s_rightControllerIndex, out prevState, out currState) && currState.isConnected) { return s_rightControllerIndex; }
            return INVALID_DEVICE_INDEX;
        }

        private static RigidPose ToPose(OVRPlugin.Posef value)
        {
            var ovrPose = value.ToOVRPose();
            return new RigidPose(ovrPose.position, ovrPose.orientation);
        }

        public override void BeforeRenderUpdate()
        {
            FlushDeviceState();

            for (uint i = 0u, imax = GetDeviceStateLength(); i < imax; ++i)
            {
                var node = s_index2node[i];
                var deviceClass = s_index2class[i];

                if (node == OVRPlugin.Node.None) { continue; }

                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                EnsureValidDeviceState(i, out prevState, out currState);

                if (!OVRPlugin.GetNodePresent(node))
                {
                    if (prevState.isConnected)
                    {
                        Debug.Log("[VIU][OculusVRModule] device disconnected. name:" + prevState.modelNumber + " model:" + prevState.deviceModel);
                    }

                    currState.Reset();
                    continue;
                }

                // Hand state
                OVRPlugin.HandState handState = new OVRPlugin.HandState();
                if (node == OVRPlugin.Node.HandLeft)
                {
                    OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref handState);
                }
                else if (node == OVRPlugin.Node.HandRight)
                {
                    OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref handState);
                }

                bool isHandTracked = (handState.Status & OVRPlugin.HandStatus.HandTracked) != 0;
                if ((deviceClass == VRModuleDeviceClass.Controller && isHandTracked)
                    || (deviceClass == VRModuleDeviceClass.TrackedHand && !isHandTracked))
                {
                    currState.Reset();
                    continue;
                }

                if (node == OVRPlugin.Node.HandLeft)
                {
                    m_isLeftHandTracked = isHandTracked;
                }
                else if (node == OVRPlugin.Node.HandRight)
                {
                    m_isRightHandTracked = isHandTracked;
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
                                case OVRSystemHeadset.Oculus_Link_Quest_2:
                                case OVRSystemHeadset.Oculus_Quest:
                                case OVRSystemHeadset.Oculus_Quest_2:
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

                    Debug.Log("[VIU][OculusVRModule] device connected. name:" + deviceName + " model:" + currState.deviceModel);
                }

                // update device pose
                currState.pose = ToPose(OVRPlugin.GetNodePose(node, OVRPlugin.Step.Render));
                currState.velocity = OVRPlugin.GetNodeVelocity(node, OVRPlugin.Step.Render).FromFlippedZVector3f();
                currState.angularVelocity = OVRPlugin.GetNodeAngularVelocity(node, OVRPlugin.Step.Render).FromFlippedZVector3f();
                currState.isPoseValid = currState.pose != RigidPose.identity;
                currState.isConnected = OVRPlugin.GetNodePresent(node);

                // update device input
                switch (currState.deviceModel)
                {
                    case VRModuleDeviceModel.OculusTouchLeft:
                    case VRModuleDeviceModel.OculusQuestControllerLeft:
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

                            currState.SetAxisValue(VRModuleRawAxis.IndexPinch, handState.PinchStrength[(int) HandFinger.Index]);
                            currState.SetAxisValue(VRModuleRawAxis.MiddlePinch, handState.PinchStrength[(int) HandFinger.Middle]);
                            currState.SetAxisValue(VRModuleRawAxis.RingPinch, handState.PinchStrength[(int) HandFinger.Ring]);
                            currState.SetAxisValue(VRModuleRawAxis.PinkyPinch, handState.PinchStrength[(int) HandFinger.Pinky]);

                            // Map index pinch to trigger
                            currState.SetButtonPress(VRModuleRawButton.Trigger, currState.GetButtonPress(VRModuleRawButton.GestureIndexPinch));
                            currState.SetButtonTouch(VRModuleRawButton.Trigger, currState.GetButtonTouch(VRModuleRawButton.GestureIndexPinch));
                            currState.SetAxisValue(VRModuleRawAxis.Trigger, currState.GetAxisValue(VRModuleRawAxis.IndexPinch));
                        }
                        
                        if (isHandTracked)
                        {
                            Skeleton skeleton = node == OVRPlugin.Node.HandLeft ? leftHandSkeleton : rightHandSkeleton;
                            skeleton.Update(handState);

                            JointEnumArray jointArray = currState.handJoints;
                            for (int j = 0; j < (int) OVRPlugin.BoneId.Hand_End; j++)
                            {
                                Transform joint = skeleton.Bones[j];
                                jointArray[s_ovrBoneIdToHandJointName[j]] = new JointPose(joint.position, skeleton.GetOpenXRRotation((OVRPlugin.BoneId) j));
                            }

                            currState.pose = new RigidPose(currState.pose.pos, skeleton.GetOpenXRRotation(OVRPlugin.BoneId.Hand_WristRoot));
                        }

                        break;
#if !VIU_OCULUSVR_19_0_OR_NEWER
                    case VRModuleDeviceModel.OculusGoController:
                    case VRModuleDeviceModel.OculusGearVrController:
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
                }
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
            ProcessDeviceInputChanged();
        }
#endif
    }
}
