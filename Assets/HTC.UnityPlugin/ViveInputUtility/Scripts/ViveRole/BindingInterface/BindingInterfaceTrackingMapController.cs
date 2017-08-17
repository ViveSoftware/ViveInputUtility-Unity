//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceTrackingMapController : MonoBehaviour
    , IScrollHandler
    {
        private const float MIN_SCALE = 1f;
        private const float MAX_SCALE = 5f;

        [SerializeField]
        private Vector2 m_defaultOffset = new Vector2(0.0f, 0.0f);
        [SerializeField]
        [Range(MIN_SCALE, MAX_SCALE)]
        private float m_defaultScale = 2f;

        private float m_scale = 1f;

        public void Awake()
        {
            transform.localPosition = m_defaultOffset;

            m_scale = m_defaultScale;
            UpdateScale();
        }

        public void OnScroll(PointerEventData eventData)
        {
            m_scale += eventData.scrollDelta.y * 0.1f;
            UpdateScale();
        }

        private void UpdateScale()
        {
            m_scale = Mathf.Clamp(m_scale, MIN_SCALE, MAX_SCALE);
            transform.localScale = new Vector3(m_scale, m_scale, 1f);
        }
    }
}