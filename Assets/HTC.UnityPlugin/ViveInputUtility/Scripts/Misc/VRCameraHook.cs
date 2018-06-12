//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// This componenet hooks up custom VR camera required component
    /// </summary>
    [AddComponentMenu("HTC/VIU/Hooks/VR Camera Hook", 10)]
    public class VRCameraHook : MonoBehaviour
    {
        private void Awake()
        {
            if (VRModule.activeModule == VRModuleActiveEnum.Uninitialized)
            {
                VRModule.onActiveModuleChanged += OnModuleActivated;
            }
            else
            {
                OnModuleActivated(VRModule.activeModule);
            }
        }

        private void OnModuleActivated(VRModuleActiveEnum activatedModule)
        {
            switch (activatedModule)
            {
#if VIU_STEAMVR
                case VRModuleActiveEnum.SteamVR:
                    if (GetComponent<SteamVR_Camera>() == null)
                    {
                        gameObject.AddComponent<SteamVR_Camera>();
                    }
                    break;
#endif
#if VIU_WAVEVR
                case VRModuleActiveEnum.WaveVR:
                    if (GetComponent<WaveVR_Render>() == null)
                    {
                        gameObject.AddComponent<WaveVR_Render>();
                    }
                    if (GetComponent<VivePoseTracker>() == null)
                    {
                        gameObject.AddComponent<VivePoseTracker>().viveRole.SetEx(DeviceRole.Hmd);
                    }
                    if (GetComponentsInChildren<AudioListener>().Length > 1)
                    {
                        var listener = GetComponent<AudioListener>();
                        if (listener != null)
                        {
                            Destroy(listener);
                        }
                    }
                    break;
#endif
                default:
                    break;
            }

            if (activatedModule != VRModuleActiveEnum.Uninitialized)
            {
                VRModule.onActiveModuleChanged -= OnModuleActivated;
            }
        }
    }
}