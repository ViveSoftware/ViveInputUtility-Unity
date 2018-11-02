//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using UnityEngine;

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
                    transform.localPosition = new Vector3(pos.x, m_height, pos.y);
                    break;
            }
        }
    }
}