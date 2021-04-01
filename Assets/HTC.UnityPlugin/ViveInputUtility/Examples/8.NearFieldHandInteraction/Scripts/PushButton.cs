//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class PushButton : MonoBehaviour
    {
        public UnityEvent triggered;

#pragma warning disable 0649

        [SerializeField] private float m_minHeight;
        [SerializeField] private float m_maxHeight;

        [Range(0.01f, 1.0f)]
        [SerializeField] private float m_triggerThresholdHeight = 0.2f;

        [Range(0.0f, 1.0f)]
        [SerializeField] private float m_triggeredHeight = 0.0f;

        [SerializeField] private float m_recoverSpeed = 0.05f;

#pragma warning restore 0649

        private Rigidbody m_rigidbody;
        private bool m_isRecovering;
        private bool m_isTriggerd;

        private bool hasTriggeredAlternativeHeight
        {
            get { return m_triggeredHeight > 0.0f; }
        }

        protected virtual void Start()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            Assert.IsNotNull(m_rigidbody, "Rigidbody is required.");
        }

        protected virtual void FixedUpdate()
        {
            // Check if triggered
            float percentage = Mathf.Clamp01((transform.localPosition.y - m_minHeight) / (m_maxHeight - m_minHeight));
            if (!m_isRecovering && percentage <= m_triggerThresholdHeight)
            {
                InvokeTriggerEvent();
                m_isRecovering = true;
                m_isTriggerd = !m_isTriggerd;
            }

            if (m_isRecovering && percentage > m_triggerThresholdHeight)
            {
                m_isRecovering = false;
            }

            // Recover
            Vector3 position = transform.localPosition;
            position.y = transform.localPosition.y + (m_recoverSpeed * Time.deltaTime);
            transform.localPosition = position;
        }

        protected virtual void LateUpdate()
        {
            // Lock position in local space except for clamped y
            float maxHeight = m_isTriggerd && hasTriggeredAlternativeHeight ? m_maxHeight * m_triggeredHeight : m_maxHeight;
            float clampedY = Mathf.Clamp(transform.localPosition.y, m_minHeight, maxHeight);
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