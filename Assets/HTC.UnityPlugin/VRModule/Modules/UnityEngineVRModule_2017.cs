//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
#if UNITY_2017_1_OR_NEWER
        private uint m_leftIndex = INVALID_DEVICE_INDEX;
        private uint m_rightIndex = INVALID_DEVICE_INDEX;

        private Dictionary<ulong, uint> m_node2Index = new Dictionary<ulong, uint>();
        private bool[] m_nodeStatesValid = new bool[MAX_DEVICE_COUNT];
        private List<VRNodeState> m_nodeStateList = new List<VRNodeState>();

        private void OnNodeAdded(VRNodeState nodeState)
        {
            //Debug.Log(Time.frameCount + " OnNodeAdded " + nodeState.nodeType + " tracked=" + nodeState.tracked + " id=" + (InputTracking.GetNodeName(nodeState.uniqueID) ?? string.Empty));

            switch (nodeState.nodeType)
            {
                case VRNode.Head:
                case VRNode.RightHand:
                case VRNode.LeftHand:
                case VRNode.GameController:
                case VRNode.HardwareTracker:
                case VRNode.TrackingReference:
                    break;
                default:
                    return;
            }

            if (m_node2Index.ContainsKey(nodeState.uniqueID))
            {
                Debug.LogError("Duplicate node added id:" + nodeState.uniqueID + " name:" + InputTracking.GetNodeName(nodeState.uniqueID));
            }
            else
            {
                var validIndexFound = false;

                if (nodeState.nodeType == VRNode.Head && !m_nodeStatesValid[0])
                {
                    validIndexFound = true;
                    m_nodeStatesValid[0] = true;
                    m_node2Index.Add(nodeState.uniqueID, 0u);
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
                                case VRNode.RightHand: m_rightIndex = i; break;
                                case VRNode.LeftHand: m_leftIndex = i; break;
                            }

                            break;
                        }
                    }
                }

                if (!validIndexFound)
                {
                    Debug.LogWarning("VRNode added, but device index out of range, drop the node");
                    return;
                }
            }
        }

        private void OnNodeRemoved(VRNodeState nodeState)
        {
            //Debug.Log(Time.frameCount + " OnNodeRemoved " + nodeState.nodeType + " tracked=" + nodeState.tracked);

            var deviceIndex = INVALID_DEVICE_INDEX;
            if (m_node2Index.TryGetValue(nodeState.uniqueID, out deviceIndex))
            {
                m_node2Index.Remove(nodeState.uniqueID);
                m_nodeStatesValid[deviceIndex] = false;

                if (deviceIndex == m_rightIndex) { m_rightIndex = INVALID_DEVICE_INDEX; }
                if (deviceIndex == m_leftIndex) { m_leftIndex = INVALID_DEVICE_INDEX; }
            }
        }

        public override uint GetLeftControllerDeviceIndex() { return m_leftIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_rightIndex; }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            InputTracking.GetNodeStates(m_nodeStateList);

            var rightIndex = INVALID_DEVICE_INDEX;
            var leftIndex = INVALID_DEVICE_INDEX;
            for (int i = 0, imax = m_nodeStateList.Count; i < imax; ++i)
            {
                var deviceIndex = INVALID_DEVICE_INDEX;
                if (!m_node2Index.TryGetValue(m_nodeStateList[i].uniqueID, out deviceIndex)) { continue; }

                var prevDeviceState = prevState[deviceIndex];
                var currDeviceState = currState[deviceIndex];

                currDeviceState.isConnected = true;

                switch (m_nodeStateList[i].nodeType)
                {
                    case VRNode.Head:
                        currDeviceState.deviceClass = VRModuleDeviceClass.HMD;
                        break;
                    case VRNode.RightHand:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Controller;
                        rightIndex = deviceIndex;
                        break;
                    case VRNode.LeftHand:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Controller;
                        leftIndex = deviceIndex;
                        break;
                    case VRNode.GameController:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Controller;
                        break;
                    case VRNode.HardwareTracker:
                        currDeviceState.deviceClass = VRModuleDeviceClass.GenericTracker;
                        break;
                    case VRNode.TrackingReference:
                        currDeviceState.deviceClass = VRModuleDeviceClass.TrackingReference;
                        break;
                    default:
                        currDeviceState.deviceClass = VRModuleDeviceClass.Invalid;
                        break;
                }

                if (!prevDeviceState.isConnected)
                {
                    currDeviceState.deviceSerialID = InputTracking.GetNodeName(m_nodeStateList[i].uniqueID) ?? string.Empty;
                    currDeviceState.deviceModelNumber = VRDevice.model + " " + m_nodeStateList[i].nodeType;

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

            // remove disconnected nodes
            for (int i = 0; i < MAX_DEVICE_COUNT; ++i)
            {
                if (!m_nodeStatesValid[i] && currState[i].isConnected)
                {
                    currState[i].Reset();
                }
            }

            if (VRModule.IsValidDeviceIndex(rightIndex))
            {
                currState[rightIndex].SetButtonPress(VRModuleRawButton.PadOrStickPress, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.RightTrackpadPress]));
                currState[rightIndex].SetButtonPress(VRModuleRawButton.PadOrStickTouch, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.RightTrackpadTouch]));
                currState[rightIndex].SetButtonPress(VRModuleRawButton.FunctionKey, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.RightMenuButtonPress]));

                currState[rightIndex].SetAxisValue(VRModuleRawAxis.PadOrStickX, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightTrackpadHorizontal]));
                currState[rightIndex].SetAxisValue(VRModuleRawAxis.PadOrStickY, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightTrackpadVertical]));
                currState[rightIndex].SetAxisValue(VRModuleRawAxis.Trigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightTriggerSqueeze]));
                currState[rightIndex].SetAxisValue(VRModuleRawAxis.GripOrHandTrigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightGripSqueeze]));
            }

            if (VRModule.IsValidDeviceIndex(leftIndex))
            {
                currState[leftIndex].SetButtonPress(VRModuleRawButton.PadOrStickPress, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.LeftTrackpadPress]));
                currState[leftIndex].SetButtonPress(VRModuleRawButton.PadOrStickTouch, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.LeftTrackpadTouch]));
                currState[leftIndex].SetButtonPress(VRModuleRawButton.FunctionKey, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.LeftMenuButtonPress]));

                currState[leftIndex].SetAxisValue(VRModuleRawAxis.PadOrStickX, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftTrackpadHorizontal]));
                currState[leftIndex].SetAxisValue(VRModuleRawAxis.PadOrStickY, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftTrackpadVertical]));
                currState[leftIndex].SetAxisValue(VRModuleRawAxis.Trigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftTriggerSqueeze]));
                currState[leftIndex].SetAxisValue(VRModuleRawAxis.GripOrHandTrigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftGripSqueeze]));
            }

            if (m_rightIndex != rightIndex || m_leftIndex != leftIndex)
            {
                m_rightIndex = rightIndex;
                m_leftIndex = leftIndex;
                VRModule.onControllerRoleChanged.Invoke();
            }
        }
#endif
    }
}