//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using System;
using UnityEngine;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// To provide static APIs to retrieve devices' tracking status
    /// </summary>
    public static partial class VivePose
    {
        #region origin
        /// <summary>
        /// Returns true if input focus captured by current process
        /// Usually the process losses focus when player switch to deshboard by clicking Steam button
        /// </summary>
        public static bool HasFocus() { return s_hasFocus; }

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
        public static Pose GetPose(HandRole role, Transform origin = null)
        {
            return GetPose(ViveRole.GetDeviceIndexEx(role), origin);
        }

        /// <summary>
        /// Returns tracking pose of the device identified by role
        /// </summary>
        public static Pose GetPose(DeviceRole role, Transform origin = null)
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
        #endregion origin

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
        public static Pose GetPoseEx<TRole>(TRole role, Transform origin = null)
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
        #endregion extend generic

        #region extend general
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
        public static Pose GetPoseEx(Type roleType, int roleValue, Transform origin = null)
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
        #endregion extend general

        #region base
        public static bool IsValid(uint deviceIndex)
        {
            return deviceIndex < s_rawPoses.Length && s_rawPoses[deviceIndex].bDeviceIsConnected && s_rawPoses[deviceIndex].bPoseIsValid && s_hasFocus; ;
        }

        public static bool IsConnected(uint deviceIndex)
        {
            return deviceIndex < s_rawPoses.Length && s_rawPoses[deviceIndex].bDeviceIsConnected;
        }

        public static bool HasTracking(uint deviceIndex)
        {
            return deviceIndex < s_rawPoses.Length && s_rawPoses[deviceIndex].bPoseIsValid;
        }

        public static bool IsOutOfRange(uint deviceIndex)
        {
            return deviceIndex < s_rawPoses.Length && (s_rawPoses[deviceIndex].eTrackingResult == ETrackingResult.Running_OutOfRange || s_rawPoses[deviceIndex].eTrackingResult == ETrackingResult.Calibrating_OutOfRange);
        }

        public static bool IsCalibrating(uint deviceIndex)
        {
            return deviceIndex < s_rawPoses.Length && (s_rawPoses[deviceIndex].eTrackingResult == ETrackingResult.Calibrating_InProgress || s_rawPoses[deviceIndex].eTrackingResult == ETrackingResult.Calibrating_OutOfRange);
        }

        public static bool IsUninitialized(uint deviceIndex)
        {
            return deviceIndex < s_rawPoses.Length && s_rawPoses[deviceIndex].eTrackingResult == ETrackingResult.Uninitialized;
        }

        public static Vector3 GetVelocity(uint deviceIndex, Transform origin = null)
        {
            var rawValue = Vector3.zero;
            if (deviceIndex < s_rawPoses.Length)
            {
                rawValue = new Vector3(s_rawPoses[deviceIndex].vVelocity.v0, s_rawPoses[deviceIndex].vVelocity.v1, -s_rawPoses[deviceIndex].vVelocity.v2);
            }
            return origin == null ? rawValue : origin.TransformVector(rawValue);
        }

        public static Vector3 GetAngularVelocity(uint deviceIndex, Transform origin = null)
        {
            var rawValue = Vector3.zero;
            if (deviceIndex < s_rawPoses.Length)
            {
                rawValue = new Vector3(-s_rawPoses[deviceIndex].vAngularVelocity.v0, -s_rawPoses[deviceIndex].vAngularVelocity.v1, s_rawPoses[deviceIndex].vAngularVelocity.v2);
            }
            return origin == null ? rawValue : origin.TransformVector(rawValue);
        }

        public static Pose GetPose(uint deviceIndex, Transform origin = null)
        {
            var rawPose = new Pose();
            if (deviceIndex < s_poses.Length) { rawPose = s_poses[deviceIndex]; }
            if (origin != null)
            {
                rawPose = new Pose(origin) * rawPose;
                rawPose.pos.Scale(origin.localScale);
            }
            return rawPose;
        }

        public static void SetPose(Transform target, uint deviceIndex, Transform origin = null)
        {
            Pose.SetPose(target, GetPose(deviceIndex), origin);
        }
        #endregion base
    }
}