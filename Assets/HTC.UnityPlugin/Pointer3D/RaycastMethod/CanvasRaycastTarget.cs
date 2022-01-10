//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public interface ICanvasRaycastTarget
    {
        Canvas canvas { get; }
        bool enabled { get; }
        bool ignoreReversedGraphics { get; }
    }

    [AddComponentMenu("VIU/UI Pointer/Canvas Raycast Target", 6)]
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class CanvasRaycastTarget : UIBehaviour, ICanvasRaycastTarget
    {
        private Canvas m_canvas;
        [SerializeField]
        private bool m_IgnoreReversedGraphics = true;

        public virtual Canvas canvas { get { return m_canvas ?? (m_canvas = GetComponent<Canvas>()); } }

        public bool ignoreReversedGraphics { get { return m_IgnoreReversedGraphics; } set { m_IgnoreReversedGraphics = value; } }

        protected override void OnEnable()
        {
            base.OnEnable();
            CanvasRaycastMethod.AddTarget(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CanvasRaycastMethod.RemoveTarget(this);
        }
    }
}