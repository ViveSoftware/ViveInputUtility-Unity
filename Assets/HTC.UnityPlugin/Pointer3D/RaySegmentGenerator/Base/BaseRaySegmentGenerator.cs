//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Pointer3D
{
    [RequireComponent(typeof(Pointer3DRaycaster))]
    public abstract class BaseRaySegmentGenerator : MonoBehaviour, IRaySegmentGenerator
    {
        private Pointer3DRaycaster m_raycaster;
        public Pointer3DRaycaster raycaster { get { return m_raycaster; } }

        protected virtual void Start()
        {
            m_raycaster = GetComponent<Pointer3DRaycaster>();
            if (m_raycaster != null) { m_raycaster.AddGenerator(this); }
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        protected virtual void OnDestroy()
        {
            if (m_raycaster != null) { raycaster.RemoveGenerator(this); }
            m_raycaster = null;
        }

        public abstract void ResetSegments();

        public abstract bool NextSegment(out Vector3 direction, out float distance);


    }
}