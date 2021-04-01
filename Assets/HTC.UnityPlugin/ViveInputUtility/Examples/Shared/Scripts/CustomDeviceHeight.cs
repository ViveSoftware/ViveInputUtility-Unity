//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace HTC.UnityPlugin.Vive
{
    // This script set custom device height depends on loaded VR device,
    // Daydream need additional height for device so
    // we can control camera-rig like using room-scale VR devices
    public class CustomDeviceHeight : MonoBehaviour
    {
        [SerializeField]
        private float m_height = 1.3f;

        public float height
        {
            get { return m_height; }
            set { if (ChangeProp.Set(ref m_height, value)) { UpdateHeight(); } }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled && VRModule.Active)
            {
                UpdateHeight();
            }
        }
#endif

        private void OnEnable()
        {
            VRModule.onActiveModuleChanged += OnActiveModuleChanged;
            VRModule.Initialize();

            UpdateHeight();
        }

        private void OnDisable()
        {
            VRModule.onActiveModuleChanged -= OnActiveModuleChanged;
        }

        private void OnActiveModuleChanged(VRModuleActiveEnum activeModule)
        {
            UpdateHeight();
        }

        public void UpdateHeight()
        {
            var pos = transform.localPosition;

            switch (VRModule.activeModule)
            {
                case VRModuleActiveEnum.DayDream:
                    transform.localPosition = new Vector3(pos.x, m_height, pos.z);
                    break;
#if VIU_OCULUSVR && !VIU_OCULUSVR_19_0_OR_NEWER
                case VRModuleActiveEnum.OculusVR:
                    if (OVRPlugin.GetSystemHeadsetType().Equals(OVRPlugin.SystemHeadset.Oculus_Go))
                    {
                        transform.localPosition = new Vector3(pos.x, m_height, pos.z);
                    }
                    break;
#endif
#if UNITY_2019_2_OR_NEWER && !UNITY_2019_3_OR_NEWER
                case VRModuleActiveEnum.UnityNativeVR:
                    if (XRDevice.model.Equals("Oculus Go"))
                    {
                        transform.localPosition = new Vector3(pos.x, m_height, pos.z);
                    }
                    break;
#endif
            }
        }
    }
}