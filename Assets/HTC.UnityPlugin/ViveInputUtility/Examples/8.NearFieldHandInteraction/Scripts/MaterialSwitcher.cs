//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace HTC.UnityPlugin.Vive
{
    public class MaterialSwitcher : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] private List<Material> m_materials;

#pragma warning restore 0649

        private Renderer m_renderer;
        private int m_materialIndex;
        
        public void SwitchNextMaterial()
        {
            if (!m_renderer || m_materials.Count == 0)
            {
                return;
            }

            m_materialIndex = (m_materialIndex + 1) % m_materials.Count;
            m_renderer.material = m_materials[m_materialIndex];
        }

        protected virtual void Start()
        {
            Init();
        }

        protected void Init()
        {
            m_renderer = GetComponent<Renderer>();
            Assert.IsNotNull(m_renderer, "Renderer is required.");

            if (m_renderer && m_materials.Count > 0)
            {
                m_renderer.material = m_materials[m_materialIndex];
            }
        }

        protected virtual void OnValidate()
        {
            Init();
        }
    }
}