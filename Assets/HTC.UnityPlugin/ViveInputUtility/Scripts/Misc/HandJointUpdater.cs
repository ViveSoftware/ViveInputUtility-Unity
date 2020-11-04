//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
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

        public enum RigMode
        {
            RotateOnly,
            RotateAndScale,
            RotateAndTranslate,
        }

        [Serializable]
        public class JointTransArray : EnumArray<HandJointIndex, Transform> { }

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private RigMode m_rigMode;
        [SerializeField]
        private Handed m_modelHanded;
        [SerializeField]
        private GameObject m_debugJoint;
        [SerializeField]
        private JointTransArray m_modelJoints = new JointTransArray();

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public RigMode rigMode { get { return m_rigMode; } set { m_rigMode = value; } }

        public JointTransArray modelJoints { get { return m_modelJoints; } }

        public Handed modelHanded { get { return m_modelHanded; } set { m_modelHanded = value; } }

        private bool m_isValidModel;
        private RigidPose m_modelOffset;
        private float m_modelLength;
        private GameObject m_debugJointRoot;
        private JointTransArray m_debugJoints;

        private void Awake()
        {
            // setup joints

            // findout model front/up axis
            var wrist = m_modelJoints[HandJointIndex.Wrist];
            var furthest = FindFurthestFingerTip();
            var thinnest = FindMostThinnestFingerTip();
            var thickest = FindMostThickestFingerTip();

            if (wrist == null || furthest == null || thinnest == null || thickest == null || thinnest == thickest)
            {
                Debug.LogError(logPrefix + "Unable to update joints because no valid finger found. furthest:" + (furthest ? furthest.name : "null") + " furthest:" + (thinnest ? thinnest.name : "null") + " furthest:" + (thickest ? thickest.name : "null"));
                return;
            }

            // find forward
            var forward = NormalizeAxis(wrist.InverseTransformPoint(furthest.position));
            var vThinnest = wrist.InverseTransformPoint(thinnest.position);
            var vThickest = wrist.InverseTransformPoint(thickest.position);
            var up = NormalizeAxis(m_modelHanded == Handed.Right ? Vector3.Cross(vThickest, vThinnest) : Vector3.Cross(vThinnest, vThickest));

            if (Vector3.Dot(forward, up) != 0f)
            {
                Debug.LogError(logPrefix + "Unable to find valid model forward/up. forward:" + forward + " up:" + up);
                return;
            }

            m_isValidModel = true;
            m_modelOffset = new RigidPose(Vector3.zero, Quaternion.Inverse(Quaternion.LookRotation(forward, up)));
            m_modelLength = CalculateModelLength();
        }

        private float CalculateModelLength()
        {
            var len = 0f;
            var wrist = m_modelJoints[HandJointIndex.Wrist];
            var lastPos = Vector3.zero;
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.MiddleMetacarpal, HandJointIndex.MiddleTip))
            {
                if (t != null)
                {
                    var pos = wrist.InverseTransformPoint(t.position);
                    len += (pos - lastPos).magnitude;
                    lastPos = pos;
                }
            }
            return len;
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
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.MiddleTip, HandJointIndex.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.IndexTip, HandJointIndex.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.RingTip, HandJointIndex.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.PinkyTip, HandJointIndex.PinkyMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.ThumbTip, HandJointIndex.ThumbTrapezium)) { if (t != null) { return t; } }
            return null;
        }

        private Transform FindMostThinnestFingerTip()
        {
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.PinkyTip, HandJointIndex.PinkyMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.RingTip, HandJointIndex.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.MiddleTip, HandJointIndex.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.IndexTip, HandJointIndex.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.ThumbTip, HandJointIndex.ThumbTrapezium)) { if (t != null) { return t; } }
            return null;
        }

        private Transform FindMostThickestFingerTip()
        {
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.ThumbTip, HandJointIndex.ThumbTrapezium)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.IndexTip, HandJointIndex.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.MiddleTip, HandJointIndex.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.RingTip, HandJointIndex.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ElementsFrom(HandJointIndex.PinkyTip, HandJointIndex.PinkyMetacarpal)) { if (t != null) { return t; } }
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
            if (!m_isValidModel) { return; }

            var deviceIndex = m_viveRole.GetDeviceIndex();
            if (!VRModule.IsValidDeviceIndex(deviceIndex)) { return; }

            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);
            if (deviceState.GetValidHandJointCount() <= 0) { return; }

            var wristTransform = m_modelJoints[HandJointIndex.Wrist];
            wristTransform.localScale = Vector3.one;

            var roomSpaceWristPoseInverse = deviceState._handJoints[HandJointIndex.Wrist].pose.GetInverse();
            foreach (var p in m_modelJoints)
            {
                var jointIndex = p.Key;
                var jointTrans = p.Value;
                var jointPoseData = deviceState._handJoints[jointIndex];

                if (jointPoseData.isValid)
                {
                    var roomSpaceJointPose = jointPoseData.pose;
                    var wristSpaceJointPose = roomSpaceWristPoseInverse * roomSpaceJointPose;

                    if (jointTrans != null)
                    {
                        switch (m_rigMode)
                        {
                            case RigMode.RotateOnly:
                            case RigMode.RotateAndScale:
                                jointTrans.rotation = transform.rotation * wristSpaceJointPose.rot * m_modelOffset.rot;
                                break;
                            case RigMode.RotateAndTranslate:
                                jointTrans.position = transform.TransformPoint((wristSpaceJointPose * m_modelOffset).pos);
                                jointTrans.rotation = transform.rotation * wristSpaceJointPose.rot * m_modelOffset.rot;
                                break;
                        }
                    }

                    if (TryInitDebugJoints())
                    {
                        var debugJointTransform = m_debugJoints[jointIndex];
                        if (debugJointTransform == null)
                        {
                            var obj = Instantiate(m_debugJoint);
                            obj.name = jointIndex.ToString();
                            obj.transform.SetParent(m_debugJointRoot.transform, false);
                            m_debugJoints[jointIndex] = debugJointTransform = obj.transform;
                        }

                        RigidPose.SetPose(debugJointTransform, wristSpaceJointPose);
                    }
                }
            }

            if (m_rigMode == RigMode.RotateAndScale)
            {
                wristTransform.localScale = Vector3.one * (CalculateJointLength(deviceState._handJoints) / m_modelLength);
            }
        }

        private static float CalculateJointLength(JointEnumArray.IReadOnly joints)
        {
            var len = 0f;
            var lastPos = joints[HandJointIndex.Wrist].pose.pos;
            foreach (var jointPose in joints.ElementsFrom(HandJointIndex.MiddleMetacarpal, HandJointIndex.MiddleTip))
            {
                if (jointPose.isValid)
                {
                    var pos = jointPose.pose.pos;
                    len += (pos - lastPos).magnitude;
                    lastPos = pos;
                }
            }
            return len;
        }

        private bool TryInitDebugJoints()
        {
            if (m_debugJoint == null) { return false; }
            if (m_debugJoints == null) { m_debugJoints = new JointTransArray(); }
            if (m_debugJointRoot == null)
            {
                m_debugJointRoot = new GameObject("DebugJoints"); m_debugJointRoot.transform.SetParent(transform, false);
            }
            return true;
        }

        public void OnAfterModelCreated(RenderModelHook hook)
        {
            m_viveRole.Set(hook.viveRole);
        }

        public bool OnBeforeModelActivated(RenderModelHook hook) { return true; }

        public bool OnBeforeModelDeactivated(RenderModelHook hook) { return true; }
    }
}