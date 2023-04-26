//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class HandJointUpdater : MonoBehaviour, RenderModelHook.ICustomModel, IViveRoleComponent
    {
        private static readonly string logPrefix = "[" + typeof(HandJointUpdater).Name + "] ";

        private static JointEnumArray s_lastJointPoses = new JointEnumArray();

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
        public class JointTransArray : EnumArray<HandJointName, Transform> { }

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private RigMode m_rigMode;
        [SerializeField]
        private Handed m_modelHanded;
        //[SerializeField]
        private float m_stabilizerAngleThreshold;
        //[SerializeField]
        private float m_stabilizerSlerpSpeedCoef;
        [SerializeField]
        private GameObject m_debugJoint;
        [SerializeField]
        private JointTransArray m_modelJoints = new JointTransArray();

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public RigMode rigMode { get { return m_rigMode; } set { m_rigMode = value; } }

        public JointTransArray modelJoints { get { return m_modelJoints; } }

        public Handed modelHanded { get { return m_modelHanded; } set { m_modelHanded = value; } }

        public bool isModelValid { get { return m_isValidModel; } }

        private bool m_isValidModel;
        private RigidPose m_modelOffset;
        private RigidPose m_modelOffsetInverse;
        private float m_modelLength;
        private GameObject m_debugJointRoot;
        private JointTransArray m_debugJoints;

        private void Awake()
        {
            CalculateModelSpaceAndLength();
        }

        public bool CalculateModelSpaceAndLength()
        {
            // findout model front/up axis
            var wrist = m_modelJoints[HandJointName.Wrist];
            var furthest = FindFurthestFingerTip();
            var thinnest = FindMostThinnestFingerTip();
            var thickest = FindMostThickestFingerTip();

            if (wrist == null || furthest == null || thinnest == null || thickest == null || thinnest == thickest)
            {
                Debug.LogError(logPrefix + "Unable to fine model space because no valid finger found. furthest:" + (furthest ? furthest.name : "null") + " furthest:" + (thinnest ? thinnest.name : "null") + " furthest:" + (thickest ? thickest.name : "null"));
                m_isValidModel = false;
                return false;
            }

            // find forward
            var forward = NormalizeAxis(wrist.InverseTransformPoint(furthest.position));
            var vThinnest = wrist.InverseTransformPoint(thinnest.position);
            var vThickest = wrist.InverseTransformPoint(thickest.position);
            var up = NormalizeAxis(m_modelHanded == Handed.Right ? Vector3.Cross(vThickest, vThinnest) : Vector3.Cross(vThinnest, vThickest));

            if (Vector3.Dot(forward, up) != 0f)
            {
                Debug.LogError(logPrefix + "Unable to find valid model forward/up. forward:" + forward + " up:" + up);
                m_isValidModel = false;
                return false;
            }

            m_isValidModel = true;
            m_modelOffset = new RigidPose(Vector3.zero, Quaternion.LookRotation(forward, up));
            m_modelOffsetInverse = m_modelOffset.GetInverse();
            m_modelLength = CalculateModelLength();
            return true;
        }

        private float CalculateModelLength()
        {
            var len = 0f;
            var wrist = m_modelJoints[HandJointName.Wrist];
            var lastPos = Vector3.zero;
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.MiddleMetacarpal, HandJointName.MiddleTip))
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
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.MiddleTip, HandJointName.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.IndexTip, HandJointName.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.RingTip, HandJointName.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.PinkyTip, HandJointName.PinkyMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.ThumbTip, HandJointName.ThumbTrapezium)) { if (t != null) { return t; } }
            return null;
        }

        private Transform FindMostThinnestFingerTip()
        {
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.PinkyTip, HandJointName.PinkyMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.RingTip, HandJointName.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.MiddleTip, HandJointName.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.IndexTip, HandJointName.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.ThumbTip, HandJointName.ThumbTrapezium)) { if (t != null) { return t; } }
            return null;
        }

        private Transform FindMostThickestFingerTip()
        {
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.ThumbTip, HandJointName.ThumbTrapezium)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.IndexTip, HandJointName.IndexMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.MiddleTip, HandJointName.MiddleMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.RingTip, HandJointName.RingMetacarpal)) { if (t != null) { return t; } }
            foreach (var t in m_modelJoints.ValuesFrom(HandJointName.PinkyTip, HandJointName.PinkyMetacarpal)) { if (t != null) { return t; } }
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

        private void UpdateFingerJoints(JointEnumArray.IReadOnly roomSpaceJoints, Transform parentTransform, RigidPose roomSpaceParentPoseInverse, HandJointName startJoint, HandJointName endJoint)
        {
            foreach (var index in EnumArrayBase<HandJointName>.StaticEnumsFrom(startJoint, endJoint))
            {
                var jointTrans = m_modelJoints[index];
                if (jointTrans == null) { continue; }

                var data = roomSpaceJoints[index];
                if (!data.isValid) { continue; }

                var parentSpaceJointPose = roomSpaceParentPoseInverse * data.pose;
                UpdateJointTransformLocal(parentTransform, jointTrans, parentSpaceJointPose);
                parentTransform = jointTrans;
                roomSpaceParentPoseInverse = data.pose.GetInverse();
            }
        }

        private void UpdateJointTransformLocal(Transform parentTransform, Transform targetTransform, RigidPose parentSpacePose)
        {
            RigidPose localSpacePose;
            if (m_modelOffset.rot != Quaternion.identity)
            {
                // FIXME: should calculate a fixed matrix to trasform coordinating system?
                float angle; Vector3 axis;
                parentSpacePose.rot.ToAngleAxis(out angle, out axis);
                axis = m_modelOffset.rot * axis;
                localSpacePose.rot = Quaternion.AngleAxis(angle, axis);
                localSpacePose.pos = m_modelOffset.rot * parentSpacePose.pos;
            }
            else
            {
                localSpacePose = parentSpacePose;
            }

            if (targetTransform.parent != parentTransform && targetTransform.IsChildOf(parentTransform))
            {
                for (var t = targetTransform.parent; t != parentTransform; t = t.parent)
                {
                    localSpacePose = new RigidPose(t, true).GetInverse() * localSpacePose;
                }
            }

            switch (m_rigMode)
            {
                case RigMode.RotateOnly:
                case RigMode.RotateAndScale:
                    targetTransform.localRotation = localSpacePose.rot;
                    break;
                case RigMode.RotateAndTranslate:
                    targetTransform.localPosition = localSpacePose.pos;
                    targetTransform.localRotation = localSpacePose.rot;
                    break;
            }
        }

        private void UpdatePoses()
        {
            if (!m_isValidModel) { return; }

            var deviceIndex = m_viveRole.GetDeviceIndex();
            if (!VRModule.IsValidDeviceIndex(deviceIndex)) { return; }

            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);
            if (deviceState.GetValidHandJointCount() <= 0) { return; }

            // Store last pose
            foreach (var pair in m_modelJoints.EnumValues)
            {
                if (pair.Value)
                {
                    s_lastJointPoses[pair.Key] = new JointPose(pair.Value.position, pair.Value.rotation);
                }
                else
                {
                    s_lastJointPoses[pair.Key] = default(JointPose);
                }
            }

            var roomSpaceJoints = deviceState.readOnlyHandJoints;
            var roomSpaceWristPose = roomSpaceJoints[HandJointName.Wrist].pose;
            var roomSpaceWristPoseInverse = roomSpaceWristPose.GetInverse();
            var roomSpaceHandPoseInverse = deviceState.pose.GetInverse();
            var wristTransform = m_modelJoints[HandJointName.Wrist];
            wristTransform.localScale = Vector3.one;
            
            RigidPose wristLocalPose = roomSpaceHandPoseInverse * roomSpaceWristPose * m_modelOffsetInverse;
            wristTransform.localPosition = wristLocalPose.pos;
            wristTransform.localRotation = wristLocalPose.rot;

            var palmTransform = m_modelJoints[HandJointName.Palm];
            if (palmTransform != null)
            {
                var data = roomSpaceJoints[HandJointName.Palm];
                if (data.isValid)
                {
                    UpdateJointTransformLocal(wristTransform, palmTransform, roomSpaceWristPoseInverse * data.pose);
                }
            }

            UpdateFingerJoints(roomSpaceJoints, wristTransform, roomSpaceWristPoseInverse, HandJointName.ThumbTrapezium, HandJointName.ThumbTip);
            UpdateFingerJoints(roomSpaceJoints, wristTransform, roomSpaceWristPoseInverse, HandJointName.IndexMetacarpal, HandJointName.IndexTip);
            UpdateFingerJoints(roomSpaceJoints, wristTransform, roomSpaceWristPoseInverse, HandJointName.MiddleMetacarpal, HandJointName.MiddleTip);
            UpdateFingerJoints(roomSpaceJoints, wristTransform, roomSpaceWristPoseInverse, HandJointName.RingMetacarpal, HandJointName.RingTip);
            UpdateFingerJoints(roomSpaceJoints, wristTransform, roomSpaceWristPoseInverse, HandJointName.PinkyMetacarpal, HandJointName.PinkyTip);

            if (m_rigMode == RigMode.RotateAndScale)
            {
                wristTransform.localScale = Vector3.one * (CalculateJointLength(deviceState.readOnlyHandJoints) / m_modelLength);
            }

            // Stabilize
            if (m_stabilizerAngleThreshold > 0)
            {
                foreach (var pair in m_modelJoints.EnumValues)
                {
                    if (!pair.Value)
                    {
                        continue;
                    }

                    Quaternion lastRotation = s_lastJointPoses[pair.Key].pose.rot;
                    Quaternion currentRotation = pair.Value.rotation;
                    float diffAngle = Quaternion.Angle(lastRotation, currentRotation);
                    if (diffAngle < m_stabilizerAngleThreshold)
                    {
                        if (m_stabilizerSlerpSpeedCoef > 0)
                        {
                            pair.Value.rotation = Quaternion.Slerp(lastRotation, currentRotation, m_stabilizerSlerpSpeedCoef * Time.deltaTime);
                        }
                        else
                        {
                            pair.Value.rotation = lastRotation;
                        }
                    }
                    else
                    {
                        pair.Value.rotation = Quaternion.RotateTowards(currentRotation, lastRotation, m_stabilizerAngleThreshold);
                    }
                }
            }

            if (m_debugJoint != null)
            {
                foreach (var joint in deviceState.readOnlyHandJoints.EnumValues)
                {
                    var index = joint.Key;
                    var poseData = joint.Value;

                    if (poseData.isValid && TryInitDebugJoints())
                    {
                        var debugJointTransform = m_debugJoints[index];
                        if (debugJointTransform == null)
                        {
                            var obj = Instantiate(m_debugJoint);
                            obj.name = index.ToString();
                            obj.transform.SetParent(m_debugJointRoot.transform, false);
                            m_debugJoints[index] = debugJointTransform = obj.transform;
                        }

                        RigidPose.SetPose(debugJointTransform, roomSpaceHandPoseInverse * poseData.pose);
                    }
                }
            }
        }

        private static float CalculateJointLength(JointEnumArray.IReadOnly joints)
        {
            var len = 0f;
            var lastPos = joints[HandJointName.Wrist].pose.pos;
            foreach (var jointPose in joints.ValuesFrom(HandJointName.MiddleMetacarpal, HandJointName.MiddleTip))
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