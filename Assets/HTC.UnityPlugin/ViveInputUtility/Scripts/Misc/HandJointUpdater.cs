//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class HandJointUpdater : MonoBehaviour, RenderModelHook.ICustomModel, IViveRoleComponent
    {
        public enum Handed
        {
            Right,
            Left,
        }

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private Handed m_modelHanded;
        [SerializeField]
        private EnumArray<HandJointIndex, Transform> m_joints = new EnumArray<HandJointIndex, Transform>();

        private EnumArray<HandJointIndex, JointPose> m_jointOriginPoses = new EnumArray<HandJointIndex, JointPose>();
        private EnumArray<HandJointIndex, JointPose> m_jointPoses = new EnumArray<HandJointIndex, JointPose>();

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public EnumArray<HandJointIndex, Transform> joints { get { return m_joints; } }

        private void Awake()
        {
            // setup joints

            // findout model front/up axis
            
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