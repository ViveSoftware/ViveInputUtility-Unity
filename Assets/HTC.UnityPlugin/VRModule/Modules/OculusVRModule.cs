﻿//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

#if VIU_OCULUSVR
using HTC.UnityPlugin.PoseTracker;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed class OculusVRModule : VRModule.ModuleBase
    {
#if VIU_OCULUSVR
        private static readonly OVRPlugin.Node[] s_index2node;
        private static readonly uint[] s_node2index;
        private static readonly VRModuleDeviceClass[] s_node2class;

        private OVRPlugin.TrackingOrigin m_prevTrackingSpace;

        static OculusVRModule()
        {
            s_index2node = new OVRPlugin.Node[VRModule.MAX_DEVICE_COUNT];
            for (int i = 0; i < s_index2node.Length; ++i) { s_index2node[i] = OVRPlugin.Node.None; }
            s_index2node[0] = OVRPlugin.Node.Head;
            s_index2node[1] = OVRPlugin.Node.HandLeft;
            s_index2node[2] = OVRPlugin.Node.HandRight;
            s_index2node[3] = OVRPlugin.Node.TrackerZero;
            s_index2node[4] = OVRPlugin.Node.TrackerOne;
            s_index2node[5] = OVRPlugin.Node.TrackerTwo;
            s_index2node[6] = OVRPlugin.Node.TrackerThree;

            s_node2index = new uint[(int)OVRPlugin.Node.Count];
            for (int i = 0; i < s_node2index.Length; ++i) { s_node2index[i] = INVALID_DEVICE_INDEX; }
            s_node2index[(int)OVRPlugin.Node.Head] = 0;
            s_node2index[(int)OVRPlugin.Node.HandLeft] = 1;
            s_node2index[(int)OVRPlugin.Node.HandRight] = 2;
            s_node2index[(int)OVRPlugin.Node.TrackerZero] = 3;
            s_node2index[(int)OVRPlugin.Node.TrackerOne] = 4;
            s_node2index[(int)OVRPlugin.Node.TrackerTwo] = 5;
            s_node2index[(int)OVRPlugin.Node.TrackerThree] = 6;

            s_node2class = new VRModuleDeviceClass[(int)OVRPlugin.Node.Count];
            for (int i = 0; i < s_node2class.Length; ++i) { s_node2class[i] = VRModuleDeviceClass.Invalid; }
            s_node2class[(int)OVRPlugin.Node.Head] = VRModuleDeviceClass.HMD;
            s_node2class[(int)OVRPlugin.Node.HandLeft] = VRModuleDeviceClass.Controller;
            s_node2class[(int)OVRPlugin.Node.HandRight] = VRModuleDeviceClass.Controller;
            s_node2class[(int)OVRPlugin.Node.TrackerZero] = VRModuleDeviceClass.TrackingReference;
            s_node2class[(int)OVRPlugin.Node.TrackerOne] = VRModuleDeviceClass.TrackingReference;
            s_node2class[(int)OVRPlugin.Node.TrackerTwo] = VRModuleDeviceClass.TrackingReference;
            s_node2class[(int)OVRPlugin.Node.TrackerThree] = VRModuleDeviceClass.TrackingReference;
        }

        public override bool ShouldActiveModule() { return VRSettings.enabled && VRSettings.loadedDeviceName == "Oculus"; }

        public override void OnActivated()
        {
            m_prevTrackingSpace = OVRPlugin.GetTrackingOriginType();
            UpdateTrackingSpaceType();
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
                    OVRPlugin.SetTrackingOriginType(OVRPlugin.TrackingOrigin.FloorLevel);
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
#if UNITY_5_6_1 || UNITY_2017 || UNITY_2017_1_OR_NEWER
                Time.fixedDeltaTime = 1f / VRDevice.refreshRate;
#else
                Time.fixedDeltaTime = 1f / 90f;
#endif
            }
        }

        public override uint GetLeftControllerDeviceIndex()
        {
            return s_node2index[(int)OVRPlugin.Node.HandLeft];
        }

        public override uint GetRightControllerDeviceIndex()
        {
            return s_node2index[(int)OVRPlugin.Node.HandRight];
        }

        private static Pose ToPose(OVRPlugin.Posef value)
        {
            var ovrPose = value.ToOVRPose();
            return new Pose(ovrPose.position, ovrPose.orientation);
        }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            for (uint i = 0; i < MAX_DEVICE_COUNT; ++i)
            {
                var node = s_index2node[i];
                if (node == OVRPlugin.Node.None) { continue; }

                currState[i].isConnected = OVRPlugin.GetNodePresent(node);

                if (currState[i].isConnected)
                {
                    if (!prevState[i].isConnected)
                    {
                        currState[i].deviceClass = s_node2class[(int)node];
                        // FIXME: how to get product id from OVRPlugin?
                        currState[i].serialNumber = OVRPlugin.productName + " " + node.ToString();
                        currState[i].modelNumber = OVRPlugin.productName + " " + node.ToString();
                        currState[i].renderModelName = OVRPlugin.productName + " " + node.ToString();

                        SetupKnownDeviceModel(currState[i]);
                    }

                    // update device status
                    currState[i].pose = ToPose(OVRPlugin.GetNodePose(node, OVRPlugin.Step.Render));
                    currState[i].velocity = OVRPlugin.GetNodeVelocity(node, OVRPlugin.Step.Render).FromFlippedZVector3f();
                    currState[i].angularVelocity = OVRPlugin.GetNodeAngularVelocity(node, OVRPlugin.Step.Render).FromFlippedZVector3f();

                    currState[i].isPoseValid = currState[i].pose != Pose.identity;

                    // update device input
                    switch (currState[i].deviceModel)
                    {
                        case VRModuleDeviceModel.OculusTouchLeft:
                            {
                                var ctrlState = OVRPlugin.GetControllerState((uint)OVRPlugin.Controller.LTouch);

                                currState[i].SetButtonPress(VRModuleRawButton.ApplicationMenu, (ctrlState.Buttons & (uint)OVRInput.RawButton.Y) != 0u);
                                currState[i].SetButtonPress(VRModuleRawButton.A, (ctrlState.Buttons & (uint)OVRInput.RawButton.X) != 0u);
                                currState[i].SetButtonPress(VRModuleRawButton.Touchpad, (ctrlState.Buttons & (uint)OVRInput.RawButton.LThumbstick) != 0u);
                                currState[i].SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(currState[i].GetButtonPress(VRModuleRawButton.Trigger), ctrlState.LIndexTrigger, 0.55f, 0.45f));
                                currState[i].SetButtonPress(VRModuleRawButton.Grip, AxisToPress(currState[i].GetButtonPress(VRModuleRawButton.Grip), ctrlState.LHandTrigger, 0.55f, 0.45f));
                                currState[i].SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(currState[i].GetButtonPress(VRModuleRawButton.CapSenseGrip), ctrlState.LHandTrigger, 0.55f, 0.45f));

                                currState[i].SetButtonTouch(VRModuleRawButton.ApplicationMenu, (ctrlState.Touches & (uint)OVRInput.RawTouch.Y) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.A, (ctrlState.Touches & (uint)OVRInput.RawTouch.X) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.Touchpad, (ctrlState.Touches & (uint)OVRInput.RawTouch.LThumbstick) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.Trigger, (ctrlState.Touches & (uint)OVRInput.RawTouch.LIndexTrigger) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(currState[i].GetButtonTouch(VRModuleRawButton.CapSenseGrip), ctrlState.LHandTrigger, 0.25f, 0.20f));

                                currState[i].SetAxisValue(VRModuleRawAxis.TouchpadX, ctrlState.LThumbstick.x);
                                currState[i].SetAxisValue(VRModuleRawAxis.TouchpadY, ctrlState.LThumbstick.y);
                                currState[i].SetAxisValue(VRModuleRawAxis.Trigger, ctrlState.LIndexTrigger);
                                currState[i].SetAxisValue(VRModuleRawAxis.CapSenseGrip, ctrlState.LHandTrigger);
                                break;
                            }
                        case VRModuleDeviceModel.OculusTouchRight:
                            {
                                var ctrlState = OVRPlugin.GetControllerState((uint)OVRPlugin.Controller.RTouch);

                                currState[i].SetButtonPress(VRModuleRawButton.ApplicationMenu, (ctrlState.Buttons & (uint)OVRInput.RawButton.B) != 0u);
                                currState[i].SetButtonPress(VRModuleRawButton.A, (ctrlState.Buttons & (uint)OVRInput.RawButton.A) != 0u);
                                currState[i].SetButtonPress(VRModuleRawButton.Touchpad, (ctrlState.Buttons & (uint)OVRInput.RawButton.RThumbstick) != 0u);
                                currState[i].SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(currState[i].GetButtonPress(VRModuleRawButton.Trigger), ctrlState.RIndexTrigger, 0.55f, 0.45f));
                                currState[i].SetButtonPress(VRModuleRawButton.Grip, AxisToPress(currState[i].GetButtonPress(VRModuleRawButton.Grip), ctrlState.RHandTrigger, 0.55f, 0.45f));
                                currState[i].SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(currState[i].GetButtonPress(VRModuleRawButton.CapSenseGrip), ctrlState.RHandTrigger, 0.55f, 0.45f));

                                currState[i].SetButtonTouch(VRModuleRawButton.ApplicationMenu, (ctrlState.Touches & (uint)OVRInput.RawTouch.B) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.A, (ctrlState.Touches & (uint)OVRInput.RawTouch.A) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.Touchpad, (ctrlState.Touches & (uint)OVRInput.RawTouch.RThumbstick) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.Trigger, (ctrlState.Touches & (uint)OVRInput.RawTouch.RIndexTrigger) != 0u);
                                currState[i].SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(currState[i].GetButtonTouch(VRModuleRawButton.CapSenseGrip), ctrlState.RHandTrigger, 0.25f, 0.20f));

                                currState[i].SetAxisValue(VRModuleRawAxis.TouchpadX, ctrlState.RThumbstick.x);
                                currState[i].SetAxisValue(VRModuleRawAxis.TouchpadY, ctrlState.RThumbstick.y);
                                currState[i].SetAxisValue(VRModuleRawAxis.Trigger, ctrlState.RIndexTrigger);
                                currState[i].SetAxisValue(VRModuleRawAxis.CapSenseGrip, ctrlState.RHandTrigger);
                                break;
                            }
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
        }
#endif
    }
}