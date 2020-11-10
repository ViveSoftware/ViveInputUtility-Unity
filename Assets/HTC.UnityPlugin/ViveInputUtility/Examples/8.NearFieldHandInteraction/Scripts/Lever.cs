//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class Lever : MonoBehaviour
    {
        public UnityEvent<Transform> moved;

        [SerializeField] private float m_minX;
        [SerializeField] private float m_maxX;

        private Rigidbody m_rigidbody;

        protected virtual void Start()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            Assert.IsNotNull(m_rigidbody, "Rigidbody is required.");
        }

        protected virtual void FixedUpdate()
        {
            InvokeMoveEvent(transform);
        }

        protected virtual void LateUpdate()
        {
            // Lock local position
            float clampedX = Mathf.Clamp(transform.localPosition.x, m_minX, m_maxX);
            transform.localPosition = new Vector3(clampedX, 0.0f, 0.0f);

            // Lock velocity
            if (m_rigidbody)
            {
                m_rigidbody.velocity = Vector3.zero;
            }
        }

        private void InvokeMoveEvent(Transform trans)
        {
            if (moved != null)
            {
                moved.Invoke(trans);
            }
        }
    }
}