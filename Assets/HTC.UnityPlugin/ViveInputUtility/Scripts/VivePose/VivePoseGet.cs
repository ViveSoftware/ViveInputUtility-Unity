//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// To provide static APIs to retrieve devices' tracking status
    /// </summary>
    public partial class VivePose : SingletonBehaviour<VivePose>
    {
        #region origin
        /// <summary>
        /// Returns true if input focus captured by current process
        /// Usually the process losses focus when player switch to deshboard by clicking Steam button
        /// </summary>
        public static bool HasFocus() { return VRModule.HasInputFocus(); }

        /// <summary>
        /// Returns true if the process has focus and the device identified by role is connected / has tracking
        /// </summary>
        public static bool IsValid(HandRole role)
        {
            return IsValid(ViveRole.GetDeviceIndexEx(role));
        }

        /// <summary>
        /// Returns true if the process has focus and the device identified by role is connected / has tracking
        /// </summary>
        public static bool IsValid(DeviceRole role)
        {
            return IsValid(ViveRole.GetDeviceIndexEx(role));
        }

        /// <summary>
        /// Returns true if the device identified by role is connected.
        /// </summary>
        public static bool IsConnected(HandRole role)
        {
            return IsConnected(ViveRole.GetDeviceIndexEx(role));
        }

        /// <summary>
        /// Returns true if the device identified by role is connected.
        /// </summary>
        public static bool IsConnected(DeviceRole role)
        {
            return IsConnected(ViveRole.GetDeviceIndexEx(role));
        }

        /// <summary>
        /// Returns true if tracking data of the device identified by role has valid value.
        /// </summary>
        public static bool HasTracking(HandRole role)
        {
            return HasTracking(ViveRole.GetDeviceIndexEx(role));
        }

        /// <summary>
        /// Returns true if tracking data of the device identified by role has valid value.
        /// </summary>
        public static bool HasTracking(DeviceRole role)
        {
            return HasTracking(ViveRole.GetDeviceIndexEx(role));
        }

        public static bool IsOutOfRange(HandRole role) { return IsOutOfRange(ViveRole.GetDeviceIndexEx(role)); }
        public static bool IsOutOfRange(DeviceRole role) { return IsOutOfRange(ViveRole.GetDeviceIndexEx(role)); }
        public static bool IsCalibrating(HandRole role) { return IsCalibrating(ViveRole.GetDeviceIndexEx(role)); }
        public static bool IsCalibrating(DeviceRole role) { return IsCalibrating(ViveRole.GetDeviceIndexEx(role)); }
        public static bool IsUninitialized(HandRole role) { return IsUninitialized(ViveRole.GetDeviceIndexEx(role)); }
        public static bool IsUninitialized(DeviceRole role) { return IsUninitialized(ViveRole.GetDeviceIndexEx(role)); }
        public static Vector3 GetVelocity(HandRole role, Transform origin = null) { return GetVelocity(ViveRole.GetDeviceIndexEx(role), origin); }
        public static Vector3 GetVelocity(DeviceRole role, Transform origin = null) { return GetVelocity(ViveRole.GetDeviceIndexEx(role), origin); }
        public static Vector3 GetAngularVelocity(HandRole role, Transform origin = null) { return GetAngularVelocity(ViveRole.GetDeviceIndexEx(role), origin); }
        public static Vector3 GetAngularVelocity(DeviceRole role, Transform origin = null) { return GetAngularVelocity(ViveRole.GetDeviceIndexEx(role), origin); }

        /// <summary>
        /// Returns tracking pose of the device identified by role
        /// </summary>
        public static RigidPose GetPose(HandRole role, Transform origin = null)
        {
            return GetPose(ViveRole.GetDeviceIndexEx(role), origin);
        }

        /// <summary>
        /// Returns tracking pose of the device identified by role
        /// </summary>
        public static RigidPose GetPose(DeviceRole role, Transform origin = null)
        {
            return GetPose(ViveRole.GetDeviceIndexEx(role), origin);
        }

        /// <summary>
        /// Set target pose to tracking pose of the device identified by role relative to the origin
        /// </summary>
        public static void SetPose(Transform target, HandRole role, Transform origin = null)
        {
            SetPose(target, ViveRole.GetDeviceIndexEx(role), origin);
        }

        /// <summary>
        /// Set target pose to tracking pose of the device identified by role relative to the origin
        /// </summary>
        public static void SetPose(Transform target, DeviceRole role, Transform origin = null)
        {
            SetPose(target, ViveRole.GetDeviceIndexEx(role), origin);
        }

        public static bool TryGetHandJointPose(HandRole role, HandJointName jointName, out JointPose pose)
        {
            return TryGetHandJointPose(ViveRole.GetDeviceIndexEx(role), jointName, out pose);
        }

        public static JointEnumArray.IReadOnly GetAllHandJoints(HandRole role)
        {
            return GetAllHandJoints(ViveRole.GetDeviceIndexEx(role));
        }

        public static int GetHandJointCount(HandRole role)
        {
            return GetHandJointCount(ViveRole.GetDeviceIndexEx(role));
        }
        #endregion origin

        #region general role property
        /// <summary>
        /// Returns true if the process has focus and the device identified by role is connected / has tracking
        /// </summary>
        public static bool IsValid(ViveRoleProperty role)
        {
            return IsValid(role.GetDeviceIndex());
        }

        /// <summary>
        /// Returns true if the device identified by role is connected.
        /// </summary>
        public static bool IsConnected(ViveRoleProperty role)
        {
            return IsConnected(role.GetDeviceIndex());
        }

        /// <summary>
        /// Returns true if tracking data of the device identified by role has valid value.
        /// </summary>
        public static bool HasTracking(ViveRoleProperty role)
        {
            return HasTracking(role.GetDeviceIndex());
        }

        public static bool IsOutOfRange(ViveRoleProperty role) { return IsOutOfRange(role.GetDeviceIndex()); }
        public static bool IsCalibrating(ViveRoleProperty role) { return IsCalibrating(role.GetDeviceIndex()); }
        public static bool IsUninitialized(ViveRoleProperty role) { return IsUninitialized(role.GetDeviceIndex()); }
        public static Vector3 GetVelocity(ViveRoleProperty role, Transform origin = null) { return GetVelocity(role.GetDeviceIndex(), origin); }
        public static Vector3 GetAngularVelocity(ViveRoleProperty role, Transform origin = null) { return GetAngularVelocity(role.GetDeviceIndex(), origin); }

        /// <summary>
        /// Returns tracking pose of the device identified by role
        /// </summary>
        public static RigidPose GetPose(ViveRoleProperty role, Transform origin = null)
        {
            return GetPose(role.GetDeviceIndex(), origin);
        }

        /// <summary>
        /// Set target pose to tracking pose of the device identified by role relative to the origin
        /// </summary>
        public static void SetPose(Transform target, ViveRoleProperty role, Transform origin = null)
        {
            SetPose(target, role.GetDeviceIndex(), origin);
        }

        public static bool TryGetHandJointPose(ViveRoleProperty role, HandJointName jointName, out JointPose pose)
        {
            return TryGetHandJointPose(role.GetDeviceIndex(), jointName, out pose);
        }

        public static JointEnumArray.IReadOnly GetAllHandJoints(ViveRoleProperty role)
        {
            return GetAllHandJoints(role.GetDeviceIndex());
        }

        public static int GetHandJointCount(ViveRoleProperty role)
        {
            return GetHandJointCount(role.GetDeviceIndex());
        }
        #endregion

        #region extend generic
        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsValidEx<TRole>(TRole role)
        {
            return IsValid(ViveRole.GetDeviceIndexEx(role));
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsConnectedEx<TRole>(TRole role)
        {
            return IsConnected(ViveRole.GetDeviceIndexEx(role));
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool HasTrackingEx<TRole>(TRole role)
        {
            return HasTracking(ViveRole.GetDeviceIndexEx(role));
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsOutOfRangeEx<TRole>(TRole role)
        {
            return IsOutOfRange(ViveRole.GetDeviceIndexEx(role));
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsCalibratingEx<TRole>(TRole role)
        {
            return IsCalibrating(ViveRole.GetDeviceIndexEx(role));
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsUninitializedEx<TRole>(TRole role)
        {
            return IsUninitialized(ViveRole.GetDeviceIndexEx(role));
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector3 GetVelocityEx<TRole>(TRole role, Transform origin = null)
        {
            return GetVelocity(ViveRole.GetDeviceIndexEx(role), origin);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector3 GetAngularVelocityEx<TRole>(TRole role, Transform origin = null)
        {
            return GetAngularVelocity(ViveRole.GetDeviceIndexEx(role), origin);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static RigidPose GetPoseEx<TRole>(TRole role, Transform origin = null)
        {
            return GetPose(ViveRole.GetDeviceIndexEx(role), origin);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void SetPoseEx<TRole>(Transform target, TRole role, Transform origin = null)
        {
            SetPose(target, ViveRole.GetDeviceIndexEx(role), origin);
        }

        public static bool TryGetHandJointPoseEx<TRole>(TRole role, HandJointName jointName, out JointPose pose)
        {
            return TryGetHandJointPose(ViveRole.GetDeviceIndexEx(role), jointName, out pose);
        }

        public static JointEnumArray.IReadOnly GetAllHandJointsEx<TRole>(TRole role)
        {
            return GetAllHandJoints(ViveRole.GetDeviceIndexEx(role));
        }

        public static int GetHandJointCountEx<TRole>(TRole role)
        {
            return GetHandJointCount(ViveRole.GetDeviceIndexEx(role));
        }
        #endregion extend generic

        #region extend property role type & value
        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsValidEx(Type roleType, int roleValue)
        {
            return IsValid(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsConnectedEx(Type roleType, int roleValue)
        {
            return IsConnected(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool HasTrackingEx(Type roleType, int roleValue)
        {
            return HasTracking(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsOutOfRangeEx(Type roleType, int roleValue)
        {
            return IsOutOfRange(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsCalibratingEx(Type roleType, int roleValue)
        {
            return IsCalibrating(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool IsUninitializedEx(Type roleType, int roleValue)
        {
            return IsUninitialized(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector3 GetVelocityEx(Type roleType, int roleValue, Transform origin = null)
        {
            return GetVelocity(ViveRole.GetDeviceIndexEx(roleType, roleValue), origin);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector3 GetAngularVelocityEx(Type roleType, int roleValue, Transform origin = null)
        {
            return GetAngularVelocity(ViveRole.GetDeviceIndexEx(roleType, roleValue), origin);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static RigidPose GetPoseEx(Type roleType, int roleValue, Transform origin = null)
        {
            return GetPose(ViveRole.GetDeviceIndexEx(roleType, roleValue), origin);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void SetPoseEx(Transform target, Type roleType, int roleValue, Transform origin = null)
        {
            SetPose(target, ViveRole.GetDeviceIndexEx(roleType, roleValue), origin);
        }

        public static bool TryGetHandJointPoseEx(Type roleType, int roleValue, HandJointName jointName, out JointPose pose)
        {
            return TryGetHandJointPose(ViveRole.GetDeviceIndexEx(roleType, roleValue), jointName, out pose);
        }

        public static JointEnumArray.IReadOnly GetAllHandJointsEx(Type roleType, int roleValue)
        {
            return GetAllHandJoints(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }

        public static int GetHandJointCountEx(Type roleType, int roleValue)
        {
            return GetHandJointCount(ViveRole.GetDeviceIndexEx(roleType, roleValue));
        }
        #endregion extend general

        #region base
        public static bool IsValid(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).isPoseValid && HasFocus();
        }

        public static bool IsConnected(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).isConnected;
        }

        public static bool HasTracking(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).isPoseValid;
        }

        public static bool IsOutOfRange(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).isOutOfRange;
        }

        public static bool IsCalibrating(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).isCalibrating;
        }

        public static bool IsUninitialized(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).isUninitialized;
        }

        public static Vector3 GetVelocity(uint deviceIndex, Transform origin = null)
        {
            if (!VRModule.IsValidDeviceIndex(deviceIndex))
            {
                return Vector3.zero;
            }
            else if (origin == null)
            {
                return VRModule.GetCurrentDeviceState(deviceIndex).velocity;
            }
            else
            {
                return origin.TransformVector(VRModule.GetCurrentDeviceState(deviceIndex).velocity);
            }
        }

        public static Vector3 GetAngularVelocity(uint deviceIndex, Transform origin = null)
        {
            if (!VRModule.IsValidDeviceIndex(deviceIndex))
            {
                return Vector3.zero;
            }
            else if (origin == null)
            {
                return VRModule.GetCurrentDeviceState(deviceIndex).angularVelocity;
            }
            else
            {
                return origin.TransformVector(VRModule.GetCurrentDeviceState(deviceIndex).angularVelocity);
            }
        }
        
        public static RigidPose GetPose(uint deviceIndex, Transform origin = null)
        {
            var devicePose = VRModule.GetCurrentDeviceState(deviceIndex).pose;

            if (origin == null)
            {
                return devicePose;
            }
            else
            {
                var rawPose = new RigidPose(origin) * devicePose;
                rawPose.pos.Scale(origin.localScale);
                return rawPose;
            }
        }

        public static void SetPose(Transform target, uint deviceIndex, Transform origin = null)
        {
            RigidPose.SetPose(target, GetPose(deviceIndex), origin);
        }

        public static bool TryGetHandJointPose(uint deviceIndex, HandJointName jointName, out JointPose pose)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).TryGetHandJointPose(jointName, out pose);
        }

        public static JointEnumArray.IReadOnly GetAllHandJoints(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).readOnlyHandJoints;
        }

        public static int GetHandJointCount(uint deviceIndex)
        {
            return VRModule.GetCurrentDeviceState(deviceIndex).GetValidHandJointCount();
        }
        #endregion base
    }
}