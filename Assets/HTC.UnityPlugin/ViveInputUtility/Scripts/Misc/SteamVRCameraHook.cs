//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using System.Collections;
using HTC.UnityPlugin.VRModuleManagement;

public class SteamVRCameraHook : MonoBehaviour
{
    IEnumerator Start()
    {
#if VIU_STEAMVR
        while (VRModule.activeModule == SupportedVRModule.Uninitialized)
        {
            yield return null;
        }

        if (VRModule.activeModule == SupportedVRModule.SteamVR)
        {
            if (SteamVR_Render.Top() != null)
            {
                // SteamVR_Camera already exist in the scene
            }
            else
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
            }
        }
#endif
        yield break;
    }
}