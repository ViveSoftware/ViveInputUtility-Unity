//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public class CubeSlider : MonoBehaviour
    {
        [Serializable] public class ValueChangeEvent : UnityEvent<float> {}

        public ValueChangeEvent valueChanged;

#pragma warning disable 0649

        [SerializeField] private Transform m_baseTransform;
        [SerializeField] private Transform m_barTransform;
        [SerializeField] private Vector2 m_barPadding;
        [SerializeField] private float m_barHeightOffset = 0.001f;

        [Range(0.0f, 1.0f)]
        [SerializeField] private float m_value = 1.0f;

        [SerializeField] private float m_maxLength;

#pragma warning restore 0649

        public void SetValue(float value)
        {
            m_value = Mathf.Clamp01(value);
            UpdateBarTransform();
            InvokeValueChangeEvent();
        }

        public void OnHoverStaying(Transform trans)
        {
            if (!trans)
            {
                return;
            }

            Vector3 barStartPos = GetBarStartLocalPosition();
            Vector3 collisionPos = m_baseTransform.parent.InverseTransformPoint(trans.position);
            float value = Mathf.Clamp01((collisionPos.x - barStartPos.x) / GetBarLength());

            SetValue(value);
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(m_baseTransform);
            Assert.IsNotNull(m_barTransform);
            Assert.IsTrue(m_barPadding.x >= 0);
            Assert.IsTrue(m_barPadding.y >= 0);
        }

        private void UpdateBarTransform()
        {
            Vector3 barScale = GetBarMaxScale();
            float offset = barScale.x * (1.0f - m_value);
            barScale.x -= offset;

            Vector3 basePos = m_baseTransform.localPosition;
            Vector3 barPos = new Vector3(basePos.x - (offset / 2.0f), basePos.y + m_barHeightOffset, basePos.z);

            m_barTransform.localPosition = barPos;
            m_barTransform.localScale = barScale;
        }

        private Vector3 GetBarMaxScale()
        {
            float x = m_baseTransform.localScale.x - (m_barPadding.x * 2);
            float y = m_baseTransform.localScale.y;
            float z = m_baseTransform.localScale.z - (m_barPadding.y * 2);

            return new Vector3(x, y, z);
        }

        private Vector3 GetBarStartLocalPosition()
        {
            Vector3 basePos = m_baseTransform.localPosition;
            float x = basePos.x - (m_baseTransform.localScale.x / 2.0f) + m_barPadding.x;

            return new Vector3(x, basePos.y, basePos.z);
        }

        private float GetBarLength()
        {
            return m_baseTransform.localScale.x - (m_barPadding.x * 2);
        }

        private void InvokeValueChangeEvent()
        {
            if (valueChanged != null)
            {
                valueChanged.Invoke(m_value);
            }
        }

        private void OnValidate()
        {
            UpdateBarTransform();
            InvokeValueChangeEvent();
        }
    }
}