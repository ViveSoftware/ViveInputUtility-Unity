//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

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
        private static readonly VRModuleDeviceClass[] s_nodeType2DeviceClass;

        private uint m_leftIndex = INVALID_DEVICE_INDEX;
        private uint m_rightIndex = INVALID_DEVICE_INDEX;

        private List<XRNodeState> m_nodeStateList = new List<XRNodeState>();
        private Dictionary<ulong, uint> m_node2Index = new Dictionary<ulong, uint>();
        private ulong[] m_index2nodeID;
        private bool[] m_index2nodeValidity;
        private bool[] m_index2nodeTouched;

        private TrackingSpaceType m_prevTrackingSpace;

        static UnityEngineVRModule()
        {
            s_nodeType2DeviceClass = new VRModuleDeviceClass[EnumUtils.GetMaxValue(typeof(XRNode)) + 1];
            for (int i = 0; i < s_nodeType2DeviceClass.Length; ++i) { s_nodeType2DeviceClass[i] = VRModuleDeviceClass.Invalid; }
            s_nodeType2DeviceClass[(int)XRNode.Head] = VRModuleDeviceClass.HMD;
            s_nodeType2DeviceClass[(int)XRNode.RightHand] = VRModuleDeviceClass.Controller;
            s_nodeType2DeviceClass[(int)XRNode.LeftHand] = VRModuleDeviceClass.Controller;
            s_nodeType2DeviceClass[(int)XRNode.GameController] = VRModuleDeviceClass.Controller;
            s_nodeType2DeviceClass[(int)XRNode.HardwareTracker] = VRModuleDeviceClass.GenericTracker;
            s_nodeType2DeviceClass[(int)XRNode.TrackingReference] = VRModuleDeviceClass.TrackingReference;
        }

        public override void OnActivated()
        {
            m_prevTrackingSpace = XRDevice.GetTrackingSpaceType();
            UpdateTrackingSpaceType();

            EnsureDeviceStateLength(16);
            m_index2nodeID = new ulong[GetDeviceStateLength()];
            m_index2nodeValidity = new bool[GetDeviceStateLength()];
            m_index2nodeTouched = new bool[GetDeviceStateLength()];
        }

        public override void OnDeactivated()
        {
            m_rightIndex = INVALID_DEVICE_INDEX;
            m_leftIndex = INVALID_DEVICE_INDEX;

            RemoveAllValidNodes();
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

        private bool TryGetAndTouchNodeDeviceIndex(XRNodeState nodeState, out uint deviceIndex)
        {
            // only tracking certain type of node (some nodes share same uniqueID)
            if (!IsTrackingDeviceNode(nodeState)) { deviceIndex = INVALID_DEVICE_INDEX; return false; }
            //Debug.Log(Time.frameCount + " TryGetNodeDeviceIndex " + nodeState.nodeType + " tracked=" + nodeState.tracked + " id=" + nodeState.uniqueID + " name=" + (InputTracking.GetNodeName(nodeState.uniqueID) ?? string.Empty));
            if (!m_node2Index.TryGetValue(nodeState.uniqueID, out deviceIndex))
            {
                // FIXME: 0ul is invalid id?
                if (nodeState.uniqueID == 0ul) { return false; }

                var validIndexFound = false;

                if (nodeState.nodeType == XRNode.Head)
                {
                    if (m_index2nodeValidity[0])
                    {
                        //Debug.LogWarning("[" + Time.frameCount + "] Multiple Head node found! drop node id:" + nodeState.uniqueID.ToString("X8") + " type:" + nodeState.nodeType + " name:" + InputTracking.GetNodeName(nodeState.uniqueID) + " tracked=" + nodeState.tracked);
                        deviceIndex = INVALID_DEVICE_INDEX;
                        return false;
                    }

                    validIndexFound = true;
                    m_index2nodeID[0] = nodeState.uniqueID;
                    m_index2nodeValidity[0] = true;
                    m_node2Index.Add(nodeState.uniqueID, 0u);
                    deviceIndex = 0;
                }
                else
                {
                    for (uint i = 1u, imax = (uint)m_index2nodeValidity.Length; i < imax; ++i)
                    {
                        if (!m_index2nodeValidity[i])
                        {
                            validIndexFound = true;
                            m_index2nodeID[i] = nodeState.uniqueID;
                            m_index2nodeValidity[i] = true;
                            m_node2Index.Add(nodeState.uniqueID, i);
                            deviceIndex = i;

                            break;
                        }
                    }
                }

                if (!validIndexFound)
                {
                    Debug.LogWarning("[" + Time.frameCount + "] XRNode added, but device index out of range, drop the node id:" + nodeState.uniqueID.ToString("X8") + " type:" + nodeState.nodeType + " name:" + InputTracking.GetNodeName(nodeState.uniqueID) + " tracked=" + nodeState.tracked);
                    deviceIndex = INVALID_DEVICE_INDEX;
                    return false;
                }

                //Debug.Log("[" + Time.frameCount + "] Add node device index [" + deviceIndex + "] id=" + nodeState.uniqueID.ToString("X8") + " type=" + nodeState.nodeType + " tracked=" + nodeState.tracked);
            }

            m_index2nodeTouched[deviceIndex] = true;
            return true;
        }

        private void TrimUntouchedNodes(System.Action<uint> onTrimmed)
        {
            for (uint i = 0u, imax = (uint)m_index2nodeValidity.Length; i < imax; ++i)
            {
                if (!m_index2nodeTouched[i])
                {
                    if (m_index2nodeValidity[i])
                    {
                        m_node2Index.Remove(m_index2nodeID[i]);
                        //m_index2nodeID[i] = 0;
                        m_index2nodeValidity[i] = false;

                        onTrimmed(i);
                    }
                }
                else
                {
                    Debug.Assert(m_index2nodeValidity[i]);
                    m_index2nodeTouched[i] = false;
                }
            }
        }

        private void RemoveAllValidNodes()
        {
            for (int i = 0, imax = m_index2nodeValidity.Length; i < imax; ++i)
            {
                if (m_index2nodeValidity[i])
                {
                    m_node2Index.Remove(m_index2nodeID[i]);
                    m_index2nodeID[i] = 0;
                    m_index2nodeValidity[i] = false;
                    m_index2nodeTouched[i] = false;
                }
            }
        }

        public override void BeforeRenderUpdate()
        {
            var roleChanged = false;
            var rightIndex = INVALID_DEVICE_INDEX;
            var leftIndex = INVALID_DEVICE_INDEX;

            FlushDeviceState();

            if (XRSettings.isDeviceActive && XRDevice.isPresent)
            {
                InputTracking.GetNodeStates(m_nodeStateList);
            }

            for (int i = 0, imax = m_nodeStateList.Count; i < imax; ++i)
            {
                uint deviceIndex;
                if (!TryGetAndTouchNodeDeviceIndex(m_nodeStateList[i], out deviceIndex)) { continue; }

                switch (m_nodeStateList[i].nodeType)
                {
                    case XRNode.RightHand: rightIndex = deviceIndex; break;
                    case XRNode.LeftHand: leftIndex = deviceIndex; break;
                }

                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                EnsureValidDeviceState(deviceIndex, out prevState, out currState);

                if (m_rightIndex != rightIndex || m_leftIndex != leftIndex)
                {
                    m_rightIndex = rightIndex;
                    m_leftIndex = leftIndex;
                    roleChanged = true;
                }

                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = s_nodeType2DeviceClass[(int)m_nodeStateList[i].nodeType];
                    // FIXME: getting wrong name in Unity 2017.1f1
                    //currDeviceState.serialNumber = InputTracking.GetNodeName(m_nodeStateList[i].uniqueID) ?? string.Empty;
                    //Debug.Log("connected " + InputTracking.GetNodeName(m_nodeStateList[i].uniqueID));
                    currState.serialNumber = XRDevice.model + " " + m_nodeStateList[i].uniqueID.ToString("X8");
                    currState.modelNumber = XRDevice.model + " " + m_nodeStateList[i].nodeType;
                    currState.renderModelName = XRDevice.model + " " + m_nodeStateList[i].nodeType;

                    SetupKnownDeviceModel(currState);
                }

                // update device status
                currState.isPoseValid = m_nodeStateList[i].tracked;

                var velocity = default(Vector3);
                if (m_nodeStateList[i].TryGetVelocity(out velocity)) { currState.velocity = velocity; }

                var position = default(Vector3);
                if (m_nodeStateList[i].TryGetPosition(out position)) { currState.position = position; }

                var rotation = default(Quaternion);
                if (m_nodeStateList[i].TryGetRotation(out rotation)) { currState.rotation = rotation; }
            }

            m_nodeStateList.Clear();

            // update right hand input
            if (VRModule.IsValidDeviceIndex(rightIndex))
            {
                IVRModuleDeviceState rightPrevState;
                IVRModuleDeviceStateRW rightCurrState;
                EnsureValidDeviceState(rightIndex, out rightPrevState, out rightCurrState);
                UpdateRightControllerInput(rightPrevState, rightCurrState);
            }

            //// update left hand input
            if (VRModule.IsValidDeviceIndex(leftIndex))
            {
                IVRModuleDeviceState leftPrevState;
                IVRModuleDeviceStateRW leftCurrState;
                EnsureValidDeviceState(leftIndex, out leftPrevState, out leftCurrState);
                UpdateLeftControllerInput(leftPrevState, leftCurrState);
            }

            TrimUntouchedNodes(trimmedIndex =>
            {
                IVRModuleDeviceState ps;
                IVRModuleDeviceStateRW cs;
                if (TryGetValidDeviceState(trimmedIndex, out ps, out cs))
                {
                    cs.Reset();
                }
            });

            ProcessConnectedDeviceChanged();

            if (roleChanged)
            {
                InvokeControllerRoleChangedEvent();
            }

            ProcessDevicePoseChanged();
            ProcessDeviceInputChanged();
        }
#endif
    }
}