//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

#if UNITY_2017 || UNITY_2017_1_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using HTC.UnityPlugin.Utility;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
#if UNITY_2017 || UNITY_2017_1_OR_NEWER
        private uint m_leftIndex = INVALID_DEVICE_INDEX;
        private uint m_rightIndex = INVALID_DEVICE_INDEX;

        private Dictionary<ulong, uint> m_node2Index = new Dictionary<ulong, uint>();
        private bool[] m_nodeStatesValid = new bool[MAX_DEVICE_COUNT];
        private List<VRNodeState> m_nodeStateList = new List<VRNodeState>();

        private IndexedSet<ulong> m_prevExistNodeUids = new IndexedSet<ulong>();
        private IndexedSet<ulong> m_currExistNodeUids = new IndexedSet<ulong>();

        public override uint GetLeftControllerDeviceIndex() { return m_leftIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_rightIndex; }

        private static bool IsTrackingDeviceNode(VRNode nodeType)
        {
            switch (nodeType)
            {
                case VRNode.Head:
                case VRNode.RightHand:
                case VRNode.LeftHand:
                case VRNode.GameController:
                case VRNode.HardwareTracker:
                case VRNode.TrackingReference:
                    return true;
                default:
                    return false;
            }
        }

        private bool TryGetNodeDeviceIndex(VRNodeState nodeState, out uint deviceIndex)
        {
            // only tracking certain type of node (some nodes share same uniqueID)
            if (!IsTrackingDeviceNode(nodeState.nodeType)) { deviceIndex = 0; return false; }
            //Debug.Log(Time.frameCount + " TryGetNodeDeviceIndex " + nodeState.nodeType + " tracked=" + nodeState.tracked + " id=" + nodeState.uniqueID + " name=" + (InputTracking.GetNodeName(nodeState.uniqueID) ?? string.Empty));
            if (!m_node2Index.TryGetValue(nodeState.uniqueID, out deviceIndex))
            {
                // FIXME: 0ul is invalid id?
                if (nodeState.uniqueID == 0ul) { return false; }

                var validIndexFound = false;

                if (nodeState.nodeType == VRNode.Head)
                {
                    if (m_nodeStatesValid[0])
                    {
                        Debug.LogError("Multiple Head node found! drop node id:" + nodeState.uniqueID.ToString("X8") + " name:" + InputTracking.GetNodeName(nodeState.uniqueID));
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
                                case VRNode.RightHand: m_rightIndex = i; break;
                                case VRNode.LeftHand: m_leftIndex = i; break;
                            }

                            deviceIndex = i;

                            break;
                        }
                    }
                }

                if (!validIndexFound)
                {
                    Debug.LogWarning("VRNode added, but device index out of range, drop the node");
                    return false;
                }

                //Debug.Log("Add node device index [" + deviceIndex + "] id=" + nodeState.uniqueID + " type=" + nodeState.nodeType);
            }

            return true;
        }

        private void RemoveNodeDeviceIndex(ulong uniqueID)
        {
            var deviceIndex = INVALID_DEVICE_INDEX;
            if (m_node2Index.TryGetValue(uniqueID, out deviceIndex))
            {
                m_node2Index.Remove(uniqueID);
                m_nodeStatesValid[deviceIndex] = false;

                if (deviceIndex == m_rightIndex) { m_rightIndex = INVALID_DEVICE_INDEX; }
                if (deviceIndex == m_leftIndex) { m_leftIndex = INVALID_DEVICE_INDEX; }
            }
        }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            InputTracking.GetNodeStates(m_nodeStateList);

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
                    // FIXME: getting wrong name in Unity 2017.1f1
                    //currDeviceState.deviceSerialID = InputTracking.GetNodeName(m_nodeStateList[i].uniqueID) ?? string.Empty;
                    currDeviceState.deviceSerialID = VRDevice.model + " " + m_nodeStateList[i].uniqueID.ToString("X8") + " " + (InputTracking.GetNodeName(m_nodeStateList[i].uniqueID) ?? string.Empty);
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

            // remove disconnected nodes
            for (int i = m_prevExistNodeUids.Count - 1; i >= 0; --i)
            {
                if (currState[i].isConnected) { currState[i].Reset(); }
                RemoveNodeDeviceIndex(m_prevExistNodeUids[i]);
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