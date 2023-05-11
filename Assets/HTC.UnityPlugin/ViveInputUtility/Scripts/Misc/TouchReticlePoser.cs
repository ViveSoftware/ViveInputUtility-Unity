//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Pointer3D;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Vive
{
    public class TouchReticlePoser : MonoBehaviour
    {
        [Serializable]
        public class UnityEventFloat : UnityEvent<float> { }

        [SerializeField]
        private TouchPointerRaycaster raycaster;
        [SerializeField]
        private Transform reticleRoot;
        [SerializeField]
        private Transform approachReticle;
        [SerializeField]
        private Transform touchedReticle;
        [SerializeField]
        private float approachDistance;
        [SerializeField]
        private float floatingDistance = 0.002f;
        [SerializeField]
        private UnityEventFloat onApproach;

        private bool lastResultIsValid;
        private bool lastApproaching;
        private bool lastTouched;
#if UNITY_EDITOR
        protected virtual void Reset()
        {
            for (var tr = transform; raycaster == null && tr != null; tr = tr.parent)
            {
                raycaster = tr.GetComponentInChildren<TouchPointerRaycaster>(true);
            }
        }
#endif
        private void LateUpdate()
        {
            var result = raycaster.FirstRaycastResult();

            if (!result.isValid)
            {
                if (lastResultIsValid)
                {
                    if (reticleRoot != null) { reticleRoot.gameObject.SetActive(false); }
                    lastResultIsValid = false;
                }
            }
            else
            {
                var touchDist = Mathf.Max(raycaster.MouseButtonLeftRange, raycaster.MouseButtonRightRange, raycaster.MouseButtonMiddleRange);

                if (reticleRoot != null)
                {
                    reticleRoot.rotation = Quaternion.LookRotation(-result.worldNormal, raycaster.transform.up);
                    reticleRoot.position = result.worldPosition + result.worldNormal * floatingDistance;

                    if (!lastResultIsValid)
                    {
                        reticleRoot.gameObject.SetActive(true);
                        lastResultIsValid = true;
                    }
                }

                if (approachReticle != null)
                {
                    var approach = Mathf.InverseLerp(approachDistance, touchDist, result.distance);
                    if (approach <= 0f || approach >= 1f)
                    {
                        if (lastApproaching)
                        {
                            approachReticle.gameObject.SetActive(false);
                            lastApproaching = false;
                        }
                    }
                    else
                    {
                        approachReticle.transform.localScale = Vector3.one * approach;

                        if (!lastApproaching)
                        {
                            approachReticle.gameObject.SetActive(true);
                            lastApproaching = true;
                        }
                    }
                }

                if (touchedReticle != null)
                {
                    if (result.distance <= touchDist)
                    {
                        if (!lastTouched)
                        {
                            touchedReticle.gameObject.SetActive(true);
                            lastTouched = true;
                        }
                    }
                    else
                    {
                        if (lastTouched)
                        {
                            touchedReticle.gameObject.SetActive(false);
                            lastTouched = false;
                        }
                    }
                }
            }
        }
    }
}