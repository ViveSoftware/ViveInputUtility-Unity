//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public abstract class BaseFallbackCamRaycaster : BaseMultiMethodRaycaster
    {
        [NonSerialized]
        private bool isDestroying = false;
        [NonSerialized]
        private Camera fallbackCam;

        [SerializeField]
        private float nearDistance = 0f;
        [SerializeField]
        private float farDistance = 20f;

        public float NearDistance
        {
            get { return nearDistance; }
            set
            {
                nearDistance = Mathf.Max(0f, value);
                if (eventCamera != null)
                {
                    eventCamera.nearClipPlane = nearDistance;
                }
            }
        }

        public float FarDistance
        {
            get { return farDistance; }
            set
            {
                farDistance = Mathf.Max(0f, nearDistance, value);
                if (eventCamera != null)
                {
                    eventCamera.farClipPlane = farDistance;
                }
            }
        }

        public override Camera eventCamera
        {
            get
            {
                if (isDestroying)
                {
                    return null;
                }

                if (fallbackCam == null)
                {
                    var go = new GameObject(name + " FallbackCamera");
                    go.SetActive(false);
                    // place fallback camera at root to preserve world position
                    go.transform.SetParent(EventSystem.current.transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    fallbackCam = go.AddComponent<Camera>();
                    fallbackCam.clearFlags = CameraClearFlags.Nothing;
                    fallbackCam.cullingMask = 0;
                    fallbackCam.orthographic = true;
                    fallbackCam.orthographicSize = 1;
                    fallbackCam.useOcclusionCulling = false;
#if !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
                    fallbackCam.stereoTargetEye = StereoTargetEyeMask.None;
#endif
                    fallbackCam.nearClipPlane = nearDistance;
                    fallbackCam.farClipPlane = farDistance;
                }

                return fallbackCam;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            isDestroying = true;

            if (fallbackCam != null)
            {
                Destroy(fallbackCam);
                fallbackCam = null;
            }
        }
    }
}