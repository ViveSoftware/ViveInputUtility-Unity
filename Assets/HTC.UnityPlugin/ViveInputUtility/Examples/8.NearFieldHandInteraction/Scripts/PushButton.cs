//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class PushButton : MonoBehaviour
    {
        public UnityEvent triggered;

        [SerializeField] private float m_minHeight;
        [SerializeField] private float m_maxHeight;

        [Range(0.01f, 1.0f)]
        [SerializeField] private float m_triggerThreshold = 0.8f;

        [SerializeField] private float m_recoverSpeed = 0.05f;

        private Rigidbody m_rigidbody;
        private bool m_isTriggered;

        protected virtual void Start()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            Assert.IsNotNull(m_rigidbody, "Rigidbody is required.");
        }

        protected virtual void FixedUpdate()
        {
            // Check if triggered
            float percentage = Mathf.Clamp01((transform.localPosition.y - m_minHeight) / (m_maxHeight - m_minHeight));
            if (!m_isTriggered && percentage <= (1 - m_triggerThreshold))
            {
                InvokeTriggerEvent();
                m_isTriggered = true;
            }

            if (m_isTriggered && percentage > (1 - m_triggerThreshold))
            {
                m_isTriggered = false;
            }

            // Recover
            Vector3 position = transform.localPosition;
            position.y = transform.localPosition.y + (m_recoverSpeed * Time.deltaTime);
            transform.localPosition = position;
        }

        protected virtual void LateUpdate()
        {
            // Lock position in local space except for clamped y
            float clampedY = Mathf.Clamp(transform.localPosition.y, m_minHeight, m_maxHeight);
            transform.localPosition = new Vector3(0.0f, clampedY, 0.0f);

            // Lock velocity
            if (m_rigidbody)
            {
                m_rigidbody.velocity = Vector3.zero;
            }
        }

        protected void InvokeTriggerEvent()
        {
            if (triggered != null)
            {
                triggered.Invoke();
            }
        }
    }
}