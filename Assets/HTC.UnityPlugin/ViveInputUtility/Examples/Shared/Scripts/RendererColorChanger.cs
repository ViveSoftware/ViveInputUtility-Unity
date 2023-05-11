//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class RendererColorChanger : MonoBehaviour
    {
        public Color initColor = Color.white;
        public List<Renderer> renderers;
#if UNITY_EDITOR
        private void Reset()
        {
            if (renderers == null) { renderers = new List<Renderer>(); }
            GetComponentsInChildren(true, renderers);
        }
#endif
        private void Awake()
        {
            SetColor(initColor);
        }

        public void SetColor(Color color)
        {
            if (renderers != null && renderers.Count > 0)
            {
                for (int i = 0, imax = renderers.Count; i < imax; ++i)
                {
                    var renderer = renderers[i];
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
        }
    }
}