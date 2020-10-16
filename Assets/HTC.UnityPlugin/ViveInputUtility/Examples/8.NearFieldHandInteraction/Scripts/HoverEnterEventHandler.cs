//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using HTC.UnityPlugin.ColliderEvent;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class HoverEnterEventHandler : MonoBehaviour, IColliderEventHoverEnterHandler, IColliderEventHoverExitHandler
    {
        public UnityEvent hovered;

        private Dictionary<ViveRoleProperty, int> m_stayingRoleCounts = new Dictionary<ViveRoleProperty, int>();

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
                    InvokeHoverEvent();
                }
            }
            else
            {
                InvokeHoverEvent();
            }
        }

        public void OnColliderEventHoverExit(ColliderHoverEventData eventData)
        {
            ViveColliderEventCaster caster;
            if (eventData.TryGetEventCaster(out caster))
            {
                Assert.IsTrue(m_stayingRoleCounts.ContainsKey(caster.viveRole), "Exiting role should have entered before.");

                m_stayingRoleCounts[caster.viveRole]--;
            }
        }

        private void InvokeHoverEvent()
        {
            if (hovered != null)
            {
                hovered.Invoke();
            }
        }
    }
}