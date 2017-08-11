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
            var earsComp = default(SteamVR_Ears);
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                earsComp = transform.GetChild(i).GetComponentInChildren<SteamVR_Ears>();
                if (earsComp != null) { break; }
            }

            if (earsComp == null)
            {
                var ears = new GameObject("Camera (ears)", typeof(AudioListener), typeof(SteamVR_Ears));
                ears.transform.SetParent(transform, false);
            }

            if (GetComponent<SteamVR_Camera>() == null)
            {
                gameObject.AddComponent<SteamVR_Camera>();
            }

            VRModule.onActiveModuleChanged -= OnModuleActivated;
        }
    }
#endif
}