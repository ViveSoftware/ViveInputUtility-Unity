//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;
using System;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public abstract partial class UnityXRModuleBase : VRModule.ModuleBase
    {
#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
        public static bool HasActiveLoader()
        {
            var instance = XRGeneralSettings.Instance;
            if (instance == null) { return false; }
            var manager = instance.Manager;
            if (manager == null) { return false; }
            return manager.activeLoader != null;
        }

        public static bool HasActiveLoader(string loaderName)
        {
            string activeLoaderName;
            return TryGetActiveLoaderName(out activeLoaderName) && activeLoaderName == loaderName;
        }

        public static bool HasActiveLoader(VRModuleKnownXRLoader knownLoader)
        {
            string activeLoaderName;
            return TryGetActiveLoaderName(out activeLoaderName) && knownLoader == ToKnownXRLoader(activeLoaderName);
        }

        public static VRModuleKnownXRLoader GetKnownActiveLoader()
        {
            var instance = XRGeneralSettings.Instance;
            if (instance == null) { return VRModuleKnownXRLoader.Unknown; }

            var manager = instance.Manager;
            if (manager == null) { return VRModuleKnownXRLoader.Unknown; }

            var loader = manager.activeLoader;
            if (loader == null) { return VRModuleKnownXRLoader.Unknown; }

            var loaderName = loader.name;
            if (string.IsNullOrEmpty(loaderName)) { return VRModuleKnownXRLoader.Unknown; }

            foreach (var profile in loaderProfiles)
            {
                if (string.IsNullOrEmpty(profile.fixedName) || profile.fixedName != loaderName) { continue; }
                if (profile.matchNameRgx == null || !profile.matchNameRgx.IsMatch(loaderName)) { continue; }
                return profile.loader;
            }

            return VRModuleKnownXRLoader.Unknown;
        }

        public static VRModuleKnownXRLoader ToKnownXRLoader(string loaderName)
        {
            foreach (var profile in loaderProfiles)
            {
                if (string.IsNullOrEmpty(profile.fixedName) || profile.fixedName != loaderName) { continue; }
                if (profile.matchNameRgx == null || !profile.matchNameRgx.IsMatch(loaderName)) { continue; }
                return profile.loader;
            }
            return VRModuleKnownXRLoader.Unknown;
        }

        public static bool TryGetActiveLoaderName(out string loaderName)
        {
            loaderName = default(string);
            var instance = XRGeneralSettings.Instance;
            if (instance == null) { return false; }
            var manager = instance.Manager;
            if (manager == null) { return false; }
            var loader = manager.activeLoader;
            if (loader == null) { return false; }
            loaderName = loader.name;
            return true;
        }

        public static VRModuleKnownXRInputSubsystem GetKnownActiveInputSubsystem()
        {
            var activeSubsys = ListPool<XRInputSubsystem>.Get();
            try
            {
                SubsystemManager.GetInstances(activeSubsys);
                if (activeSubsys.Count == 0)
                {
                    Debug.LogWarning("No XRInputSubsystem detected.");
                }
                else
                {
                    foreach (var subsys in activeSubsys)
                    {
                        if (!subsys.running) { continue; }

                        var subsysName = subsys.SubsystemDescriptor.id;
                        if (string.IsNullOrEmpty(subsysName)) { continue; }

                        foreach (var profile in inputSubsystemProfiles)
                        {
                            if (string.IsNullOrEmpty(profile.fixedName) || profile.fixedName != subsysName) { continue; }
                            if (profile.matchNameRgx == null || !profile.matchNameRgx.IsMatch(subsysName)) { continue; }
                            return profile.subsystem;
                        }
                    }
                }

                return VRModuleKnownXRInputSubsystem.Unknown;
            }
            finally
            {
                ListPool<XRInputSubsystem>.Release(activeSubsys);
            }
        }

        public static bool GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<bool> feature, bool defaultValue = default(bool))
        {
            bool value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static uint GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<uint> feature, uint defaultValue = default(uint))
        {
            uint value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static float GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<float> feature, float defaultValue = default(float))
        {
            float value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static Vector2 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector2> feature, Vector2 defaultValue = default(Vector2))
        {
            Vector2 value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static Vector3 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector3> feature, Vector3 defaultValue = default(Vector3))
        {
            Vector3 value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature) { return GetDeviceFeatureValueOrDefault(device, feature, Quaternion.identity); }
        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, Quaternion defaultValue)
        {
            Quaternion value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static Hand GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Hand> feature, Hand defaultValue = default(Hand))
        {
            Hand value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static Bone GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Bone> feature, Bone defaultValue = default(Bone))
        {
            Bone value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

        public static Eyes GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Eyes> feature, Eyes defaultValue = default(Eyes))
        {
            Eyes value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
#if UNITY_EDITOR
            LogWarningFeatureNotFound(device, feature);
#endif
            return defaultValue;
        }

#if UNITY_EDITOR
        private static HashSet<int> warnedFeatures = new HashSet<int>();
        private static void LogWarningFeatureNotFound<T>(InputDevice device, InputFeatureUsage<T> feature)
        {
#if CSHARP_7_OR_LATER
            var hashCode = (device, feature).GetHashCode();
#else
            var hashCode = new { device, feature }.GetHashCode();
#endif
            if (warnedFeatures.Add(hashCode))
            {
                Debug.LogWarningFormat("Device {0} doesn't have {1} feature {2}.", device.name, typeof(T).Name, feature.name);
            }
        }
#endif

#else
        public static bool HasActiveLoader() { return false; }
        public static bool HasActiveLoader(string loaderName) { return false; }
        public static bool HasActiveLoader(VRModuleKnownXRLoader knownLoader) { return false; }
        public static VRModuleKnownXRLoader GetKnownActiveLoader() { return default(VRModuleKnownXRLoader); }
        public static VRModuleKnownXRLoader ToKnownXRLoader(string loaderName) { return default(VRModuleKnownXRLoader); }
        public static bool TryGetActiveLoaderName(out string loaderName) { loaderName = default(string); return false; }
        public static VRModuleKnownXRInputSubsystem GetKnownActiveInputSubsystem() { return default(VRModuleKnownXRInputSubsystem); }
        public static bool GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<bool> feature, bool defaultValue = default(bool)) { return defaultValue; }
        public static uint GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<uint> feature, uint defaultValue = default(uint)) { return defaultValue; }
        public static float GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<float> feature, float defaultValue = default(float)) { return defaultValue; }
        public static Vector2 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector2> feature, Vector2 defaultValue = default(Vector2)) { return defaultValue; }
        public static Vector3 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector3> feature, Vector3 defaultValue = default(Vector3)) { return defaultValue; }
        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature) { return Quaternion.identity; }
        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, Quaternion defaultValue) { return defaultValue; }
        public static Hand GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Hand> feature, Hand defaultValue = default(Hand)) { return defaultValue; }
        public static Bone GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Bone> feature, Bone defaultValue = default(Bone)) { return defaultValue; }
        public static Eyes GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Eyes> feature, Eyes defaultValue = default(Eyes)) { return defaultValue; }
#endif
    }
}