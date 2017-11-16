//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using UnityEngine;

public class SteamVRCameraHook : MonoBehaviour
{
#if VIU_STEAMVR
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
        if (activatedModule == VRModuleActiveEnum.SteamVR)
        {
            if (GetComponent<SteamVR_Camera>() == null)
            {
                gameObject.AddComponent<SteamVR_Camera>();
            }

            VRModule.onActiveModuleChanged -= OnModuleActivated;
        }
    }
#endif
}