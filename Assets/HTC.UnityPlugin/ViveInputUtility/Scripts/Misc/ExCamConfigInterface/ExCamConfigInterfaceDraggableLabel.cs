//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.ExCamConfigInterface
{
    public sealed class ExCamConfigInterfaceDraggableLabel : MonoBehaviour
        , IPointerEnterHandler
        , IPointerExitHandler
        , IInitializePotentialDragHandler
        , IBeginDragHandler
        , IDragHandler
        , IEndDragHandler
    {
        [Serializable]
        public class UnityEventFloat : UnityEvent<float> { }

        [SerializeField]
        private Text m_text;
        [SerializeField]
        private InputField m_field;
        [SerializeField]
        private float m_fieldValue;
        [SerializeField]
        private string m_label;
        [SerializeField, Range(0, 10)]
        private int m_dragPrecision = 2;
        [SerializeField]
        private float m_slopePow = 1.5f;
        [SerializeField]
        private bool m_clampValue;
        [SerializeField]
        private float m_clampMin;
        [SerializeField]
        private float m_clampMax;
        [SerializeField]
        private UnityEventFloat m_onEndEdit = new UnityEventFloat();

        private HashSet<PointerEventData> m_pointerEnter;
        private PointerEventData m_pointerDrag;
        private Vector2 m_lastDragPos;
        private Vector2 m_dragDelta;
        private bool m_changingFieldText;

        public float fieldValue
        {
            get { return m_fieldValue; }
            set
            {
                if (m_clampValue)
                {
                    value = Mathf.Clamp(value, m_clampMin, m_clampMax);
                }

                if (m_fieldValue != value)
                {
                    m_fieldValue = value;
                    m_field.text = m_fieldValue.ToString("r");
                }
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            m_text = GetComponent<Text>();
        }

        private void OnValidate()
        {
            if (m_text != null)
            {
                CombineLabelToText(0.5f, 0.5f);
            }
        }
#endif

        private void Awake()
        {
            if (m_clampValue)
            {
                m_fieldValue = Mathf.Clamp(m_fieldValue, m_clampMin, m_clampMax);
            }

            m_field.text = m_fieldValue.ToString("r");

            UpdateLabelText();
            ((Text)m_field.placeholder).text = "";
            m_field.onEndEdit.AddListener(OnFieldEndEdit);
        }

        private void OnFieldEndEdit(string fieldStr)
        {
            float fv;
            if (string.IsNullOrEmpty(fieldStr))
            {
                fieldValue = 0f;
            }
            else if (float.TryParse(fieldStr, out fv))
            {
                fieldValue = fv;
            }

            SubmitFieldValue();
        }

        public void SubmitFieldValue()
        {
            m_onEndEdit.Invoke(fieldValue);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (m_pointerEnter == null) { m_pointerEnter = new HashSet<PointerEventData>(); }

            if (m_pointerEnter.Add(eventData) && m_pointerEnter.Count == 1)
            {
                UpdateLabelText();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (m_pointerEnter.Remove(eventData) && m_pointerEnter.Count == 0)
            {
                UpdateLabelText();
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {

        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (m_pointerDrag == null)
            {
                m_pointerDrag = eventData;
                m_lastDragPos = m_pointerDrag.position;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_pointerDrag == eventData)
            {
                m_dragDelta = m_pointerDrag.position - m_lastDragPos;
                m_lastDragPos = m_pointerDrag.position;
                UpdateLabelText();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_pointerDrag == eventData)
            {
                m_pointerDrag = null;
                m_dragDelta = Vector2.zero;
                UpdateLabelText();
            }
        }

        private void UpdateLabelText()
        {
            if (m_pointerDrag != null)
            {
                if (m_dragDelta.x > 0f)
                {
                    CombineLabelToText(0.5f, 1.0f);
                }
                else if (m_dragDelta.x < 0f)
                {
                    CombineLabelToText(1.0f, 0.5f);
                }

                fieldValue += Mathf.Sign(m_dragDelta.x) * Mathf.Pow(Mathf.Abs(m_dragDelta.x), m_slopePow) * Mathf.Pow(0.1f, m_dragPrecision);
                SubmitFieldValue();
            }
            else if (m_pointerEnter != null && m_pointerEnter.Count > 0)
            {
                CombineLabelToText(0.5f, 0.5f);
            }
            else
            {
                CombineLabelToText(0f, 0f);
            }
        }

        private void CombineLabelToText(float leftAlphaFactor, float rightAlphaFactor)
        {
            var color = m_text.color;
            var leftColor = (Color32)new Color(color.r, color.g, color.b, color.a * leftAlphaFactor);
            var rightColor = (Color32)new Color(color.r, color.g, color.b, color.a * rightAlphaFactor);

            m_text.text = "<color=#" + Color32ToHexStr(leftColor) + "><</color>" + m_label + "<color=#" + Color32ToHexStr(rightColor) + ">></color>";
        }

        private string Color32ToHexStr(Color32 color)
        {
            return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
        }
    }
}