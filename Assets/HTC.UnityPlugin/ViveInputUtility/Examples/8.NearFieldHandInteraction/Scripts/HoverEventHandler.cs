//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using HTC.UnityPlugin.ColliderEvent;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class HoverEventHandler : MonoBehaviour, IColliderEventHoverEnterHandler, IColliderEventHoverExitHandler
    {
        [Serializable] public class HoverEnterEvent : UnityEvent<ColliderHoverEventData> {}
        [Serializable] public class EveryHoverEnterEvent : UnityEvent<ColliderHoverEventData> {}
        [Serializable] public class HoverStayEvent : UnityEvent<Transform> {}
        [Serializable] public class HoverExitEvent : UnityEvent<ColliderHoverEventData> {}
        [Serializable] public class EveryHoverExitEvent : UnityEvent<ColliderHoverEventData> {}

        public HoverEnterEvent hoverEntered;
        public EveryHoverEnterEvent everyHoverEntered;
        public HoverStayEvent hoverStaying;
        public HoverExitEvent hoverExited;
        public EveryHoverExitEvent everyHoverExited;

        private HashSet<ColliderHoverEventData> m_stayingEventDataSet = new HashSet<ColliderHoverEventData>();
        private Transform m_stayingTransform;

        public void OnColliderEventHoverEnter(ColliderHoverEventData eventData)
        {
            m_stayingEventDataSet.Add(eventData);

            if (m_stayingEventDataSet.Count == 1)
            {
                InvokeEnterEvent(eventData);

                ViveColliderEventCaster caster;
                if (eventData.TryGetEventCaster(out caster))
                {
                    m_stayingTransform = caster.transform;
                }
            }

            InvokeEveryEnterEvent(eventData);
        }

        public void OnColliderEventHoverExit(ColliderHoverEventData eventData)
        {
            m_stayingEventDataSet.Remove(eventData);

            ViveColliderEventCaster caster;
            if (eventData.TryGetEventCaster(out caster))
            {
                if (m_stayingTransform == caster.transform)
                {
                    m_stayingTransform = null;
                }
            }

            if (m_stayingEventDataSet.Count == 0)
            {
                InvokeExitEvent(eventData);
            }

            InvokeEveryExitEvent(eventData);
        }

        protected virtual void Update()
        {
            if (m_stayingTransform)
            {
                InvokeStayEvent(m_stayingTransform);
            }
        }

        private void InvokeEnterEvent(ColliderHoverEventData eventData)
        {
            if (hoverEntered != null)
            {
                hoverEntered.Invoke(eventData);
            }
        }

        private void InvokeEveryEnterEvent(ColliderHoverEventData eventData)
        {
            if (everyHoverEntered != null)
            {
                everyHoverEntered.Invoke(eventData);
            }
        }

        private void InvokeStayEvent(Transform transform)
        {
            if (hoverStaying != null)
            {
                hoverStaying.Invoke(transform);
            }
        }

        private void InvokeExitEvent(ColliderHoverEventData eventData)
        {
            if (hoverExited != null)
            {
                hoverExited.Invoke(eventData);
            }
        }

        private void InvokeEveryExitEvent(ColliderHoverEventData eventData)
        {
            if (everyHoverExited != null)
            {
                everyHoverExited.Invoke(eventData);
            }
        }
    }
}