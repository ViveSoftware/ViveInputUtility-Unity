//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
using System;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public abstract partial class UnityXRModuleBase : VRModule.ModuleBase
    {
#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
        private class IndexMap
        {
            private Dictionary<int, uint> hashID2index = new Dictionary<int, uint>();
            private InputDevice[] index2Device = new InputDevice[VRModule.MAX_DEVICE_COUNT];
            private int[] index2DeviceHashID = new int[VRModule.MAX_DEVICE_COUNT];
            public IndexMap() { Clear(); }

            private bool IsValidDevice(InputDevice device) { return device != default(InputDevice); }
            private bool IsValidIndex(uint index) { return index < index2Device.Length; }

            public uint Device2Index(InputDevice device)
            {
                if (IsValidDevice(device))
                {
                    uint index;
                    if (hashID2index.TryGetValue(HashID(device), out index))
                    {
                        return index;
                    }
                }
                return VRModule.INVALID_DEVICE_INDEX;
            }

            public bool TryGetIndex(InputDevice device, out uint index)
            {
                index = Device2Index(device);
                return IsValidIndex(index);
            }

            public InputDevice Index2Device(uint index)
            {
                return IsValidIndex(index) ? index2Device[index] : default(InputDevice);
            }

            public bool TryGetDevice(uint index, out InputDevice device)
            {
                device = Index2Device(index);
                return IsValidDevice(device);
            }

            public bool IsMapped(int hashID)
            {
                uint index;
                return hashID2index.TryGetValue(hashID, out index) && IsValidIndex(index);
            }

            public bool IsMapped(uint index)
            {
                return IsValidIndex(index) && index2DeviceHashID[index] != 0;
            }

            public static bool IsHMD(InputDevice device)
            {
                return (device.characteristics & InputDeviceCharacteristics.HeadMounted) > 0u;
            }

            public bool TryMapAsHMD(InputDevice device)
            {
                if (!IsValidDevice(device)) { throw new ArgumentException("Invalid device", "device"); }

                var hashID = HashID(device);
                if (IsMapped(hashID)) { throw new Exception("device(" + device.ToString() + ") already mapped"); }

                if (!IsHMD(device)) { return false; }

                hashID2index[hashID] = VRModule.HMD_DEVICE_INDEX;
                index2Device[VRModule.HMD_DEVICE_INDEX] = device;
                index2DeviceHashID[VRModule.HMD_DEVICE_INDEX] = hashID;
                return true;
            }

            public void MapNonHMD(InputDevice device, uint index)
            {
                if (!IsValidDevice(device)) { throw new ArgumentException("Invalid device", "device"); }
                if (!IsValidIndex(index)) { throw new ArgumentException("index larger then VRModule.MAX_DEVICE_COUNT(" + VRModule.MAX_DEVICE_COUNT + ")", "index"); }

                var hashID = HashID(device);
                if (IsMapped(hashID)) { throw new Exception("device(" + device.ToString() + ") already mapped"); }
                if (IsMapped(index)) { throw new Exception("index(" + index + ") already mapped"); }

                if (IsHMD(device)) { throw new Exception("device(" + device.ToString() + ") is hmd"); }
                if (index == VRModule.HMD_DEVICE_INDEX) { throw new Exception("index cannot be VRModule.HMD_DEVICE_INDEX(" + VRModule.HMD_DEVICE_INDEX + ")"); }

                hashID2index[hashID] = index;
                index2Device[index] = device;
                index2DeviceHashID[index] = hashID;
            }

            public void UnmapByDevice(InputDevice device)
            {
                if (!IsValidDevice(device)) { throw new ArgumentException("Invalid device", "device"); }

                var hashID = HashID(device);
                uint index;
                if (!hashID2index.TryGetValue(hashID, out index)) { return; }

                hashID2index.Remove(hashID);
                index2Device[index] = default(InputDevice);
                index2DeviceHashID[index] = 0;
            }

            public void UnmapByIndex(uint index)
            {
                if (!IsValidIndex(index)) { throw new ArgumentException("Invalid index", "index"); }

                hashID2index.Remove(index2DeviceHashID[index]);
                index2Device[index] = default(InputDevice);
                index2DeviceHashID[index] = 0;
            }

            public void Clear()
            {
                hashID2index.Clear();
                for (int i = index2Device.Length - 1; i >= 0; --i) { index2Device[i] = default(InputDevice); }
            }

            public static int HashID(InputDevice device)
            {
#if CSHARP_7_OR_LATER
                return (device, device.name, device.characteristics).GetHashCode();
#else
                return new { device, device.name, device.characteristics }.GetHashCode();
#endif
            }
        }

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

        protected void LogDeviceFeatureUsages(InputDevice device)
        {
            List<InputFeatureUsage> usages = new List<InputFeatureUsage>();
            if (device.TryGetFeatureUsages(usages))
            {
                string strUsages = "";
                foreach (var usage in usages)
                {
                    strUsages += "[" + usage.type.Name + "] " + usage.name + "\n";
                }

                Debug.Log(device.name + " feature usages:\n\n" + strUsages);
            }
        }

        protected static string CharacteristicsToString(InputDeviceCharacteristics ch)
        {
            if (ch == 0u) { return " No Characteristic"; }
            var chu = (uint)ch;
            var str = string.Empty;
            for (var i = 1u; chu > 0u; i <<= 1)
            {
                if ((chu & i) == 0u) { continue; }
                str += " " + (InputDeviceCharacteristics)i;
                chu &= ~i;
            }
            return str;
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
                            if (!string.IsNullOrEmpty(profile.fixedName) && profile.fixedName != subsysName) { continue; }
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

        public static InputFeatureUsage<Vector3> pointerPositionFeature = new InputFeatureUsage<Vector3>("PointerPosition");
        public static InputFeatureUsage<Quaternion> pointerRotationFeature = new InputFeatureUsage<Quaternion>("PointerRotation");
        public static InputFeatureUsage<Vector3> pointerVelocityFeature = new InputFeatureUsage<Vector3>("PointerVelocity");
        public static InputFeatureUsage<Vector3> pointerAngularVelocityFeature = new InputFeatureUsage<Vector3>("PointerAngularVelocity");

#if UNITY_EDITOR
        public static bool GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<bool> feature, bool defaultValue = default(bool))
        {
            bool value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static uint GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<uint> feature, uint defaultValue = default(uint))
        {
            uint value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static float GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<float> feature, float defaultValue = default(float))
        {
            float value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static Vector2 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector2> feature, Vector2 defaultValue = default(Vector2))
        {
            Vector2 value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static Vector3 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector3> feature, Vector3 defaultValue = default(Vector3))
        {
            Vector3 value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static Vector3 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector3> feature, InputFeatureUsage<Vector3> fallbackFeature, Vector3 defaultValue = default(Vector3))
        {
            Vector3 value;
            if (device.TryGetFeatureValue(feature, out value)) { return value; }
            if (device.TryGetFeatureValue(fallbackFeature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            LogWarningFeatureNotFound(device, fallbackFeature);
            return defaultValue;
        }

        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature) { return GetDeviceFeatureValueOrDefault(device, feature, Quaternion.identity); }
        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, Quaternion defaultValue)
        {
            Quaternion value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, InputFeatureUsage<Quaternion> fallbackFeature) { return GetDeviceFeatureValueOrDefault(device, feature, fallbackFeature, Quaternion.identity); }
        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, InputFeatureUsage<Quaternion> fallbackFeature, Quaternion defaultValue)
        {
            Quaternion value;
            if (device.TryGetFeatureValue(feature, out value)) { return value; }
            if (device.TryGetFeatureValue(fallbackFeature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            LogWarningFeatureNotFound(device, fallbackFeature);
            return defaultValue;
        }

        public static UnityEngine.XR.Hand GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<UnityEngine.XR.Hand> feature, UnityEngine.XR.Hand defaultValue = default(UnityEngine.XR.Hand))
        {
            UnityEngine.XR.Hand value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static Bone GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Bone> feature, Bone defaultValue = default(Bone))
        {
            Bone value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

        public static Eyes GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Eyes> feature, Eyes defaultValue = default(Eyes))
        {
            Eyes value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            LogWarningFeatureNotFound(device, feature);
            return defaultValue;
        }

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
#else
        public static bool GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<bool> feature, bool defaultValue = default(bool))
        {
            bool value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static uint GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<uint> feature, uint defaultValue = default(uint))
        {
            uint value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static float GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<float> feature, float defaultValue = default(float))
        {
            float value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static Vector2 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector2> feature, Vector2 defaultValue = default(Vector2))
        {
            Vector2 value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static Vector3 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector3> feature, Vector3 defaultValue = default(Vector3))
        {
            Vector3 value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static Vector3 GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Vector3> feature, InputFeatureUsage<Vector3> fallbackFeature, Vector3 defaultValue = default(Vector3))
        {
            Vector3 value; 
            if (device.TryGetFeatureValue(feature, out value)) { return value; }
            if (device.TryGetFeatureValue(fallbackFeature, out value)) { return value; }
            return defaultValue;
        }

        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature) { return GetDeviceFeatureValueOrDefault(device, feature, Quaternion.identity); }
        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, Quaternion defaultValue)
        {
            Quaternion value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, InputFeatureUsage<Quaternion> fallbackFeature) { return GetDeviceFeatureValueOrDefault(device, feature, fallbackFeature, Quaternion.identity); }
        public static Quaternion GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Quaternion> feature, InputFeatureUsage<Quaternion> fallbackFeature, Quaternion defaultValue)
        {
            Quaternion value;
            if (device.TryGetFeatureValue(feature, out value)) { return value; }
            if (device.TryGetFeatureValue(fallbackFeature, out value)) { return value; }
            return defaultValue;
        }

        public static Hand GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Hand> feature, Hand defaultValue = default(Hand))
        {
            Hand value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static Bone GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Bone> feature, Bone defaultValue = default(Bone))
        {
            Bone value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }

        public static Eyes GetDeviceFeatureValueOrDefault(InputDevice device, InputFeatureUsage<Eyes> feature, Eyes defaultValue = default(Eyes))
        {
            Eyes value; if (device.TryGetFeatureValue(feature, out value)) { return value; }
            return defaultValue;
        }
#endif

#else

        public static bool HasActiveLoader() { return false; }

        public static bool HasActiveLoader(string loaderName) { return false; }

        public static bool HasActiveLoader(VRModuleKnownXRLoader knownLoader) { return false; }

        public static VRModuleKnownXRLoader GetKnownActiveLoader() { return VRModuleKnownXRLoader.Unknown; }

        public static VRModuleKnownXRLoader ToKnownXRLoader(string loaderName) { return VRModuleKnownXRLoader.Unknown; }

        public static bool TryGetActiveLoaderName(out string loaderName) { loaderName = default(string); return false; }

        public static VRModuleKnownXRInputSubsystem GetKnownActiveInputSubsystem() { return VRModuleKnownXRInputSubsystem.Unknown; }

#endif
    }
}