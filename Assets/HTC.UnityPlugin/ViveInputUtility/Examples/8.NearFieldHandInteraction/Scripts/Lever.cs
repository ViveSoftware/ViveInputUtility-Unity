//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class Lever : MonoBehaviour
    {
        [Serializable] public class MoveEvent : UnityEvent<Transform> {}

        public MoveEvent moved;

#pragma warning disable 0649

        [SerializeField] private float m_minX;
        [SerializeField] private float m_maxX;

#pragma warning restore 0649

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