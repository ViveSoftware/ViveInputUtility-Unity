//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/VIU/Device Tracker/Vive Pose Tracker (Transform)", 7)]
    // Simple component to track Vive devices.
    public class VivePoseTracker : BasePoseTracker, INewPoseListener, IViveRoleComponent
    {
        [Serializable]
        public class UnityEventBool : UnityEvent<bool> { }

        private bool m_isValid;

        public Transform origin;

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);

        public UnityEventBool onIsValidChanged;

        [HideInInspector]
        [Obsolete("Use VivePoseTracker.viveRole instead")]
        public DeviceRole role = DeviceRole.Invalid;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public bool isPoseValid { get { return m_isValid; } }

        protected void SetIsValid(bool value, bool forceSet = false)
        {
            if (ChangeProp.Set(ref m_isValid, value) || forceSet)
            {
                if (onIsValidChanged != null)
                {
                    onIsValidChanged.Invoke(value);
                }
            }
        }

        protected virtual void Start()
        {
            SetIsValid(VivePose.IsValid(m_viveRole), true);
        }
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // change old DeviceRole value to viveRole value
            var serializedObject = new UnityEditor.SerializedObject(this);

            var roleValueProp = serializedObject.FindProperty("role");
            var oldRoleValue = roleValueProp.intValue;

            if (oldRoleValue != (int)DeviceRole.Invalid)
            {
                Type newRoleType;
                int newRoleValue;

                if (oldRoleValue == -1)
                {
                    newRoleType = typeof(DeviceRole);
                    newRoleValue = (int)DeviceRole.Hmd;
                }
                else
                {
                    newRoleType = typeof(HandRole);
                    newRoleValue = oldRoleValue;
                }

                if (Application.isPlaying)
                {
                    roleValueProp.intValue = (int)DeviceRole.Invalid;
                    m_viveRole.Set(newRoleType, newRoleValue);
                }
                else
                {
                    roleValueProp.intValue = (int)DeviceRole.Invalid;
                    serializedObject.ApplyModifiedProperties();
                    m_viveRole.Set(newRoleType, newRoleValue);
                    serializedObject.Update();
                }
            }
            serializedObject.Dispose();
        }
#endif
        protected virtual void OnEnable()
        {
            VivePose.AddNewPosesListener(this);
        }

        protected virtual void OnDisable()
        {
            VivePose.RemoveNewPosesListener(this);

            SetIsValid(false);
        }

        public virtual void BeforeNewPoses() { }

        public virtual void OnNewPoses()
        {
            var deviceIndex = m_viveRole.GetDeviceIndex();
            var isValid = VivePose.IsValid(deviceIndex);

            if (isValid)
            {
                TrackPose(VivePose.GetPose(deviceIndex), origin);
            }

            SetIsValid(isValid);
        }

        public virtual void AfterNewPoses() { }
    }
}