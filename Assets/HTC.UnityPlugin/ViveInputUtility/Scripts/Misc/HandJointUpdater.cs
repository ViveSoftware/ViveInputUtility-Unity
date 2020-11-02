//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class HandJointUpdater : MonoBehaviour, RenderModelHook.ICustomModel, IViveRoleComponent
    {
        private static readonly string logPrefix = "[" + typeof(HandJointUpdater).Name + "] ";

        public enum Handed
        {
            Right,
            Left,
        }

        [Serializable]
        public class JointTransArray : EnumArray<HandJointIndex, Transform> { }

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private Handed m_modelHanded;
        [SerializeField]
        private JointTransArray m_joints = new JointTransArray();

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public EnumArray<HandJointIndex, Transform> joints { get { return m_joints; } }

        public Handed modelHanded { get { return m_modelHanded; } set { m_modelHanded = value; } }

        private bool isValidModel;


        //private EnumArray<HandJointIndex, JointPose> m_jointOriginPoses = new EnumArray<HandJointIndex, JointPose>();
        //private EnumArray<HandJointIndex, JointPose> m_jointPoses = new EnumArray<HandJointIndex, JointPose>();

        private void Awake()
        {
            // setup joints

            // findout model front/up axis
            var furthest = FindFurthestFingerTip();
            var thinnest = FindMostThinnestFingerTip();
            var thickest = FindMostThickestFingerTip();

            if (furthest == null || thinnest == null || thickest == null || thinnest == thickest)
            {
                Debug.LogError(logPrefix + "Unable to update joints because no valid finger found. furthest:" + (furthest ? furthest.name : "null") + " furthest:" + (thinnest ? thinnest.name : "null") + " furthest:" + (thickest ? thickest.name : "null"));
                return;
            }

            // find forward
            var forward = NormalizeAxis(transform.InverseTransformPoint(furthest.position));
            var vThinnest = transform.InverseTransformPoint(thinnest.position);
            var vThickest = transform.InverseTransformPoint(thickest.position);
            var up = NormalizeAxis(m_modelHanded == Handed.Right ? Vector3.Cross(vThinnest, vThickest) : Vector3.Cross(vThickest, vThinnest));

            if (Vector3.Dot(forward, up) != 0f)
            {
                Debug.LogError(logPrefix + "Unable to find valid model forward/up. forward:" + forward + " up:" + up);
                return;
            }
        }

        private static Vector3 NormalizeAxis(Vector3 v)
        {
            var vAbs = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

            if (vAbs.x > vAbs.y && vAbs.x > vAbs.z)
            {
                return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            }
            else if (vAbs.y > vAbs.x && vAbs.y > vAbs.z)
            {
                return new Vector3(0f, Mathf.Sign(v.y), 0f);
            }
            else
            {
                return new Vector3(0f, 0f, Mathf.Sign(v.z));
            }
        }

        private Transform FindFurthestFingerTip()
        {
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.MiddleTip, HandJointIndex.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.IndexTip, HandJointIndex.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.RingTip, HandJointIndex.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.PinkyTip, HandJointIndex.PinkyMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.ThumbTip, HandJointIndex.ThumbTrapezium)) { if (t != null) { return t; } }
            return null;
        }

        private Transform FindMostThinnestFingerTip()
        {
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.PinkyTip, HandJointIndex.PinkyMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.RingTip, HandJointIndex.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.MiddleTip, HandJointIndex.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.IndexTip, HandJointIndex.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.ThumbTip, HandJointIndex.ThumbTrapezium)) { if (t != null) { return t; } }
            return null;
        }

        private Transform FindMostThickestFingerTip()
        {
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.ThumbTip, HandJointIndex.ThumbTrapezium)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.IndexTip, HandJointIndex.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.MiddleTip, HandJointIndex.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.RingTip, HandJointIndex.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_joints.ElementsFrom(HandJointIndex.PinkyTip, HandJointIndex.PinkyMetacarpal)) { if (t != null) { return t; } }
            return null;
        }

        private void OnEnable()
        {
            VRModule.onNewPoses += UpdatePoses;
        }

        private void OnDisable()
        {
            VRModule.onNewPoses -= UpdatePoses;
        }

        private void UpdatePoses()
        {
            var deviceIndex = m_viveRole.GetDeviceIndex();
            if (!VRModule.IsValidDeviceIndex(deviceIndex)) { return; }

            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);

        }

        public void OnAfterModelCreated(RenderModelHook hook)
        {
            m_viveRole.Set(hook.viveRole);
        }

        public bool OnBeforeModelActivated(RenderModelHook hook) { return true; }

        public bool OnBeforeModelDeactivated(RenderModelHook hook) { return true; }
    }
}