//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.Management.Instrumentation;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed class UnityXRPluginManagementVRModule : VRModule.ModuleBase
    {
        public override int moduleIndex { get { return (int)VRModuleActiveEnum.UnityXRPluginManagement; } }

#if VIU_XR_PLUGIN_MANAGEMENT
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance != null && s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
                if (hook.GetComponent<VivePoseTracker>() == null)
                {
                    VivePoseTracker poseTracker = hook.gameObject.AddComponent<VivePoseTracker>();
                    poseTracker.viveRole.SetEx(DeviceRole.Hmd);
                }
            }
        }

        private const uint DEVICE_STATE_LENGTH = 16;
        private static readonly VRModuleDeviceClass[] s_XRNodeToDeviceClasses;
        private static UnityXRPluginManagementVRModule s_moduleInstance;

        public override bool ShouldActiveModule() { return VIUSettings.activateUnityNativeVRModule && XRGeneralSettings.Instance.InitManagerOnStart; }

        private Dictionary<string, uint> m_deviceSerialToIndex = new Dictionary<string, uint>();

        static UnityXRPluginManagementVRModule()
        {
            s_XRNodeToDeviceClasses = new VRModuleDeviceClass[EnumUtils.GetMaxValue(typeof(XRNode)) + 1];
            InitXRNodeToDeviceClasses();
        }

        private static void InitXRNodeToDeviceClasses()
        {
            for (int i = 0; i < s_XRNodeToDeviceClasses.Length; i++)
            {
                s_XRNodeToDeviceClasses[i] = VRModuleDeviceClass.Invalid;
            }

            s_XRNodeToDeviceClasses[(int)XRNode.Head] = VRModuleDeviceClass.HMD;
            s_XRNodeToDeviceClasses[(int)XRNode.RightHand] = VRModuleDeviceClass.Controller;
            s_XRNodeToDeviceClasses[(int)XRNode.LeftHand] = VRModuleDeviceClass.Controller;
            s_XRNodeToDeviceClasses[(int)XRNode.GameController] = VRModuleDeviceClass.Controller;
            s_XRNodeToDeviceClasses[(int)XRNode.HardwareTracker] = VRModuleDeviceClass.GenericTracker;
            s_XRNodeToDeviceClasses[(int)XRNode.TrackingReference] = VRModuleDeviceClass.TrackingReference;
        }

        public override void OnActivated()
        {
            s_moduleInstance = this;
            EnsureDeviceStateLength(DEVICE_STATE_LENGTH);
        }

        public override void OnDeactivated()
        {
            s_moduleInstance = null;
            m_deviceSerialToIndex.Clear();
        }

        public override uint GetLeftControllerDeviceIndex() { return 1; }

        public override uint GetRightControllerDeviceIndex() { return 1; }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {

            }
        }

        public override void Update()
        {
            UpdateLockPhysicsUpdateRate();
        }

        public override void BeforeRenderUpdate()
        {
            List<InputDevice> inputDevices = new List<InputDevice>();
            InputDevices.GetDevices(inputDevices);
            foreach (InputDevice device in inputDevices)
            {
                uint deviceIndex = GetDeviceIndex(device.serialNumber);
                IVRModuleDeviceState prevState;
                IVRModuleDeviceStateRW currState;
                EnsureValidDeviceState(deviceIndex, out prevState, out currState);

                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = s_XRNodeToDeviceClasses[device.];
                }
            }
        }

        private void UpdateLockPhysicsUpdateRate()
        {
            if (VRModule.lockPhysicsUpdateRateToRenderFrequency && Time.timeScale > 0.0f)
            {
                List<XRDisplaySubsystem> displaySystems = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySystems);

                float minRefreshRate = float.MaxValue;
                foreach (XRDisplaySubsystem system in displaySystems)
                {
                    if (system.TryGetDisplayRefreshRate(out float rate))
                    {
                        if (rate < minRefreshRate)
                        {
                            minRefreshRate = rate;
                        }
                    }
                }

                if (minRefreshRate > 0 && minRefreshRate < float.MaxValue)
                {
                    Time.fixedDeltaTime = 1.0f / minRefreshRate;
                }
            }
        }

        private uint GetDeviceIndex(string serial)
        {
            if (m_deviceSerialToIndex.TryGetValue(serial, out uint index))
            {
                return index;
            }
            
            uint newIndex = (uint)m_deviceSerialToIndex.Count;
            m_deviceSerialToIndex.Add(serial, newIndex);

            return newIndex;
        }
#endif
    }
}