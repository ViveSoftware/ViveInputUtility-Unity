//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [ExecuteInEditMode]
    public class SteamVRCameraHook : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning("SteamVRCameraHook is deprecated. Switch to VRCameraHook automatically.");
            gameObject.AddComponent<VRCameraHook>();
            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }
    }
}