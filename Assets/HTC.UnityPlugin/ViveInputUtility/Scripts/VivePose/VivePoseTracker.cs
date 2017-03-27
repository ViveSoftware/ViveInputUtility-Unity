//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/Vive/Vive Pose Tracker")]
    // Simple component to track Vive devices.
    public class VivePoseTracker : BasePoseTracker, INewPoseListener, IViveRoleComponent
    {
        [Serializable]
        public class UnityEventBool : UnityEvent<bool> { }

        private bool m_isValid;

        public Transform origin;

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New();

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
            SetIsValid(VivePose.IsValidEx(viveRole.roleType, viveRole.roleValue), true);
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
                    viveRole.Set(newRoleType, newRoleValue);
                }
                else
                {
                    roleValueProp.intValue = (int)DeviceRole.Invalid;
                    serializedObject.ApplyModifiedProperties();
                    viveRole.Set(newRoleType, newRoleValue);
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
        }

        public virtual void BeforeNewPoses() { }

        public virtual void OnNewPoses()
        {
            var deviceIndex = viveRole.GetDeviceIndex();

            var valid = VivePose.IsValid(deviceIndex);

            if (valid)
            {
                TrackPose(VivePose.GetPose(deviceIndex, origin));
            }

            SetIsValid(valid);
        }

        public virtual void AfterNewPoses() { }
    }
}