//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    [RequireComponent(typeof(Pointer3DRaycaster))]
    public abstract class BaseRaycastMethod : MonoBehaviour, IRaycastMethod
    {
        private Pointer3DRaycaster m_raycaster;
        public Pointer3DRaycaster raycaster { get { return m_raycaster; } }

        protected virtual void Start()
        {
            m_raycaster = GetComponent<Pointer3DRaycaster>();
            if (m_raycaster != null) { m_raycaster.AddRaycastMethod(this); }
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        protected virtual void OnDestroy()
        {
            if (m_raycaster != null) { raycaster.RemoveRaycastMethod(this); }
            m_raycaster = null;
        }

        public abstract void Raycast(Ray ray, float distance, List<RaycastResult> raycastResults);
    }
}