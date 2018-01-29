//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

#if UNITY_2017_1_OR_NEWER

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
using XRNodeState = UnityEngine.VR.VRNodeState;
using XRNode = UnityEngine.VR.VRNode;
#endif

#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
#if UNITY_2017_1_OR_NEWER
        private uint m_leftIndex = INVALID_DEVICE_INDEX;
        private uint m_rightIndex = INVALID_DEVICE_INDEX;

        private Dictionary<ulong, uint> m_node2Index = new Dictionary<ulong, uint>();
        private bool[] m_nodeStatesValid = new bool[MAX_DEVICE_COUNT];
        private List<XRNodeState> m_nodeStateList = new List<XRNodeState>();

        private IndexedSet<ulong> m_prevExistNodeUids = new IndexedSet<ulong>();
        private IndexedSet<ulong> m_currExistNodeUids = new IndexedSet<ulong>();

        private TrackingSpaceType m_prevTrackingSpace;

        public override void OnActivated()
        {
            m_prevTrackingSpace = XRDevice.GetTrackingSpaceType();
            UpdateTrackingSpaceType();
        }

        public override void OnDeactivated()
        {
            XRDevice.SetTrackingSpaceType(m_prevTrackingSpace);
        }

        public override uint GetLeftControllerDeviceIndex() { return m_leftIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_rightIndex; }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.Stationary:
                    XRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);
                    break;
                case VRModuleTrackingSpaceType.RoomScale:
                    XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);
                    break;
            }
        }

        private bool IsTrackingDeviceNode(XRNodeState nodeState)
        {
            switch (nodeState.nodeType)
            {
                case XRNode.Head:
                case XRNode.RightHand:
                case XRNode.LeftHand:
                case XRNode.GameController:
                case XRNode.HardwareTracker:
                case XRNode.TrackingReference:
                    return true;
                default:
                    return false;
            }
        }

        private bool TryGetNodeDeviceIndex(XRNodeState nodeState, out uint deviceIndex)
        {
            // only tracking certain type of node (some nodes share same uniqueID)
            if (!IsTrackingDeviceNode(nodeState)) { deviceIndex = 0; return false; }
            //Debug.Log(Time.frameCount + " TryGetNodeDeviceIndex " + nodeState.nodeType + " tracked=" + nodeState.tracked + " id=" + nodeState.uniqueID + " name=" + (InputTracking.GetNodeName(nodeState.uniqueID) ?? string.Empty));
            if (!m_node2Index.TryGetValue(nodeState.uniqueID, out deviceIndex))
            {
                // FIXME: 0ul is invalid id?
                if (nodeState.uniqueID == 0ul) { return false; }

                var validIndexFound = false;

                if (nodeState.nodeType == XRNode.Head)
                {
                    if (m_nodeStatesValid[0])
                    {
                        //Debug.LogWarning("[" + Time.frameCount + "] Multiple Head node found! drop node id:" + nodeState.uniqueID.ToString("X8") + " type:" + nodeState.nodeType + " name:" + InputTracking.GetNodeName(nodeState.uniqueID) + " tracked=" + nodeState.tracked);
                        return false;
                    }

                    validIndexFound = true;
                    m_nodeStatesValid[0] = true;
                    m_node2Index.Add(nodeState.uniqueID, 0u);
                    deviceIndex = 0;
                }
                else
                {
                    for (uint i = 1; i < MAX_DEVICE_COUNT; ++i)
                    {
                        if (!m_nodeStatesValid[i])
                        {
                            validIndexFound = true;
                            m_nodeStatesValid[i] = true;
                            m_node2Index.Add(nodeState.uniqueID, i);

                            switch (nodeState.nodeType)
                            {
                                case XRNode.RightHand: m_rightIndex = i; break;
                                case XRNode.LeftHand: m_leftIndex = i; break;
                            }

                            deviceIndex = i;

                            break;
                        }
                    }
                }

                if (!validIndexFound)
                {
                    Debug.LogWarning("[" + Time.frameCount + "] XRNode added, but device index out of range, drop the node id:" + nodeState.uniqueID.ToString("X8") + " type:" + nodeState.nodeType + " name:" + InputTracking.GetNodeName(nodeState.uniqueID) + " tracked=" + nodeState.tracked);
                    return false;
                }

                //Debug.Log("[" + Time.frameCount + "] Add node device index [" + deviceIndex + "] id=" + nodeState.uniqueID.ToString("X8") + " type=" + nodeState.nodeType + " tracked=" + nodeState.tracked);
            }

            return true;
        }

        private uint RemoveNodeDeviceIndex(ulong uniqueID)
        {
            var deviceIndex = INVALID_DEVICE_INDEX;
            if (m_node2Index.TryGetValue(uniqueID, out deviceIndex))
            {
                m_node2Index.Remove(uniqueID);
                m_nodeStatesValid[deviceIndex] = false;

                if (deviceIndex == m_rightIndex) { m_rightIndex = INVALID_DEVICE_INDEX; }
                if (deviceIndex == m_leftIndex) { m_leftIndex = INVALID_DEVICE_INDEX; }
            }

            return deviceIndex;
        }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            if (XRSettings.isDeviceActive && XRDevice.isPresent)
            {
                InputTracking.GetNodeStates(m_nodeStateList);
            }

            var rightIndex = INVALID_DEVICE_INDEX;
            var leftIndex = INVALID_DEVICE_INDEX;

            for (int i = 0, imax = m_nodeStateList.Count; i < imax; ++i)
            {
                uint deviceIndex;
                if (!TryGetNodeDeviceIndex(m_nodeStateList[i], out deviceIndex))
                {
                    continue;
                }

                m_prevExistNodeUids.Remove(m_nodeStateList[i].uniqueID);
                m_currExistNodeUids.Add(m_nodeStateList[i].uniqueID);

                var prevDeviceState = prevState[deviceIndex];
                var currDeviceState = currState[deviceIndex];

                currDeviceState.isConnected = true;

                switch (m_nodeStateList[i].nodeType)
                {
                    case XRNode.Head:
                        currDeviceState.deviceClass = VRModuleDeviceClass.HMD;
                        break;
                    case XRNode.RightHand:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Controller;
                        rightIndex = deviceIndex;
                        break;
                    case XRNode.LeftHand:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Controller;
                        leftIndex = deviceIndex;
                        break;
                    case XRNode.GameController:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Controller;
                        break;
                    case XRNode.HardwareTracker:
                        currDeviceState.deviceClass = VRModuleDeviceClass.GenericTracker;
                        break;
                    case XRNode.TrackingReference:
                        currDeviceState.deviceClass = VRModuleDeviceClass.TrackingReference;
                        break;
                    default:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Invalid;
                        break;
                }

                if (!prevDeviceState.isConnected)
                {
                    // FIXME: getting wrong name in Unity 2017.1f1
                    //currDeviceState.serialNumber = InputTracking.GetNodeName(m_nodeStateList[i].uniqueID) ?? string.Empty;
                    currDeviceState.serialNumber = XRDevice.model + " " + m_nodeStateList[i].uniqueID.ToString("X8");
                    currDeviceState.modelNumber = XRDevice.model + " " + m_nodeStateList[i].nodeType;
                    currDeviceState.renderModelName = XRDevice.model + " " + m_nodeStateList[i].nodeType;

                    SetupKnownDeviceModel(currDeviceState);
                }

                // update device status
                currDeviceState.isPoseValid = m_nodeStateList[i].tracked;

                var velocity = default(Vector3);
                if (m_nodeStateList[i].TryGetVelocity(out velocity)) { currDeviceState.velocity = velocity; }

                var position = default(Vector3);
                if (m_nodeStateList[i].TryGetPosition(out position)) { currDeviceState.position = position; }

                var rotation = default(Quaternion);
                if (m_nodeStateList[i].TryGetRotation(out rotation)) { currDeviceState.rotation = rotation; }
            }

            m_nodeStateList.Clear();

            if (VRModule.IsValidDeviceIndex(rightIndex))
            {
                var rightCurrState = currState[m_rightIndex];
                var rightPrevState = prevState[m_rightIndex];

                var rightMenuPress = Input.GetKey(ButtonKeyCode.RMenuPress);
                var rightAButtonPress = Input.GetKey(ButtonKeyCode.RAKeyPress);
                var rightPadPress = Input.GetKey(ButtonKeyCode.RPadPress);

                var rightMenuTouch = Input.GetKey(ButtonKeyCode.RMenuTouch);
                var rightAButtonTouch = Input.GetKey(ButtonKeyCode.RAKeyTouch);
                var rightPadTouch = Input.GetKey(ButtonKeyCode.RPadTouch);
                var rightTriggerTouch = Input.GetKey(ButtonKeyCode.RTriggerTouch);

                var rightTrackpadX = Input.GetAxisRaw(ButtonAxisName.RPadX);
                var rightTrackpadY = Input.GetAxisRaw(ButtonAxisName.RPadY);
                var rightTrigger = Input.GetAxisRaw(ButtonAxisName.RTrigger);
                var rightGrip = Input.GetAxisRaw(ButtonAxisName.RGrip);

                rightCurrState.SetButtonPress(VRModuleRawButton.ApplicationMenu, rightMenuPress);
                rightCurrState.SetButtonPress(VRModuleRawButton.A, rightAButtonPress);
                rightCurrState.SetButtonPress(VRModuleRawButton.Touchpad, rightPadPress);
                rightCurrState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(rightPrevState.GetButtonPress(VRModuleRawButton.Trigger), rightTrigger, 0.55f, 0.45f));
                rightCurrState.SetButtonPress(VRModuleRawButton.Grip, AxisToPress(rightPrevState.GetButtonPress(VRModuleRawButton.Grip), rightGrip, 0.55f, 0.45f));
                rightCurrState.SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(rightPrevState.GetButtonPress(VRModuleRawButton.CapSenseGrip), rightGrip, 0.55f, 0.45f));

                rightCurrState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, rightMenuTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.A, rightAButtonTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.Touchpad, rightPadTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.Trigger, rightTriggerTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(rightPrevState.GetButtonTouch(VRModuleRawButton.CapSenseGrip), rightGrip, 0.25f, 0.20f));

                rightCurrState.SetAxisValue(VRModuleRawAxis.TouchpadX, rightTrackpadX);
                rightCurrState.SetAxisValue(VRModuleRawAxis.TouchpadY, rightTrackpadY);
                rightCurrState.SetAxisValue(VRModuleRawAxis.Trigger, rightTrigger);
                rightCurrState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, rightGrip);
            }

            if (VRModule.IsValidDeviceIndex(leftIndex))
            {
                var leftCurrState = currState[m_leftIndex];
                var leftPrevState = prevState[m_leftIndex];

                var leftMenuPress = Input.GetKey(ButtonKeyCode.LMenuPress);
                var leftAButtonPress = Input.GetKey(ButtonKeyCode.LAKeyPress);
                var leftPadPress = Input.GetKey(ButtonKeyCode.LPadPress);

                var leftMenuTouch = Input.GetKey(ButtonKeyCode.LMenuTouch);
                var leftAButtonTouch = Input.GetKey(ButtonKeyCode.LAKeyTouch);
                var leftPadTouch = Input.GetKey(ButtonKeyCode.LPadTouch);
                var leftTriggerTouch = Input.GetKey(ButtonKeyCode.LTriggerTouch);

                var leftTrackpadX = Input.GetAxisRaw(ButtonAxisName.LPadX);
                var leftTrackpadY = Input.GetAxisRaw(ButtonAxisName.LPadY);
                var leftTrigger = Input.GetAxisRaw(ButtonAxisName.LTrigger);
                var leftGrip = Input.GetAxisRaw(ButtonAxisName.LGrip);

                leftCurrState.SetButtonPress(VRModuleRawButton.ApplicationMenu, leftMenuPress);
                leftCurrState.SetButtonPress(VRModuleRawButton.A, leftAButtonPress);
                leftCurrState.SetButtonPress(VRModuleRawButton.Touchpad, leftPadPress);
                leftCurrState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(leftPrevState.GetButtonPress(VRModuleRawButton.Trigger), leftTrigger, 0.55f, 0.45f));
                leftCurrState.SetButtonPress(VRModuleRawButton.Grip, AxisToPress(leftPrevState.GetButtonPress(VRModuleRawButton.Grip), leftGrip, 0.55f, 0.45f));
                leftCurrState.SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(leftPrevState.GetButtonPress(VRModuleRawButton.CapSenseGrip), leftGrip, 0.55f, 0.45f));

                leftCurrState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, leftMenuTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.A, leftAButtonTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.Touchpad, leftPadTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.Trigger, leftTriggerTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(leftPrevState.GetButtonTouch(VRModuleRawButton.CapSenseGrip), leftGrip, 0.25f, 0.20f));

                leftCurrState.SetAxisValue(VRModuleRawAxis.TouchpadX, leftTrackpadX);
                leftCurrState.SetAxisValue(VRModuleRawAxis.TouchpadY, leftTrackpadY);
                leftCurrState.SetAxisValue(VRModuleRawAxis.Trigger, leftTrigger);
                leftCurrState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, leftGrip);
            }

            // remove disconnected nodes
            for (int i = m_prevExistNodeUids.Count - 1; i >= 0; --i)
            {
                var removedIndex = RemoveNodeDeviceIndex(m_prevExistNodeUids[i]);
                if (VRModule.IsValidDeviceIndex(removedIndex))
                {
                    currState[removedIndex].Reset();
                }
            }

            var temp = m_prevExistNodeUids;
            m_prevExistNodeUids = m_currExistNodeUids;
            m_currExistNodeUids = temp;
            m_currExistNodeUids.Clear();

            if (m_rightIndex != rightIndex || m_leftIndex != leftIndex)
            {
                m_rightIndex = rightIndex;
                m_leftIndex = leftIndex;
                InvokeControllerRoleChangedEvent();
            }
        }
#endif
    }
}