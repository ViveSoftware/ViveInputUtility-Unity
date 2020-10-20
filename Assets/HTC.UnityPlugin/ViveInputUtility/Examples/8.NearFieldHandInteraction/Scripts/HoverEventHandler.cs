//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using HTC.UnityPlugin.ColliderEvent;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class HoverEventHandler : MonoBehaviour, IColliderEventHoverEnterHandler, IColliderEventHoverExitHandler
    {
        public UnityEvent<ColliderHoverEventData> hoverEntered;
        public UnityEvent<Transform> hoverStaying;
        public UnityEvent<ColliderHoverEventData> hoverExited;

        private Dictionary<ViveRoleProperty, int> m_stayingRoleCounts = new Dictionary<ViveRoleProperty, int>();
        private Transform m_stayingTransform;

        public void OnColliderEventHoverEnter(ColliderHoverEventData eventData)
        {
            ViveColliderEventCaster caster;
            if (eventData.TryGetEventCaster(out caster))
            {
                int count;
                if (m_stayingRoleCounts.TryGetValue(caster.viveRole, out count))
                {
                    m_stayingRoleCounts[caster.viveRole]++;
                }
                else
                {
                    m_stayingRoleCounts.Add(caster.viveRole, 1);
                }

                if (m_stayingRoleCounts[caster.viveRole] == 1)
                {
                    InvokeEnterEvent(eventData);
                    m_stayingTransform = caster.transform;
                }
            }
            else
            {
                InvokeEnterEvent(eventData);
            }
        }

        public void OnColliderEventHoverExit(ColliderHoverEventData eventData)
        {
            ViveColliderEventCaster caster;
            if (eventData.TryGetEventCaster(out caster))
            {
                Assert.IsTrue(m_stayingRoleCounts.ContainsKey(caster.viveRole), "Exiting role should have entered before.");

                m_stayingRoleCounts[caster.viveRole]--;
                if (m_stayingRoleCounts[caster.viveRole] == 0)
                {
                    InvokeExitEvent(eventData);
                }

                if (m_stayingTransform == caster.transform)
                {
                    m_stayingTransform = null;
                }
            }
            else
            {
                InvokeExitEvent(eventData);
            }
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

        private void InvokeStayEvent(Transform transform)
        {
            if (hoverEntered != null)
            {
                hoverStaying.Invoke(transform);
            }
        }

        private void InvokeExitEvent(ColliderHoverEventData eventData)
        {
            if (hoverEntered != null)
            {
                hoverExited.Invoke(eventData);
            }
        }
    }
}