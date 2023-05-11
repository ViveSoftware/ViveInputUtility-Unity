//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [Serializable]
    public struct DefaultTooltipRenderData
    {
        public string labelText;
    }

    public class DefaultTooltipRenderer : TooltipRendererBase<DefaultTooltipRenderDataAsset, DefaultTooltipRenderData>
    {
        [SerializeField]
        private Transform m_tooltipParent;
        [SerializeField]
        private GameObject m_tooltipPrefab;

        public GameObject tooltipPrefab { get { return m_tooltipPrefab; } set { m_tooltipPrefab = value; } }

        private EnumArray<ControllerButton, DefaultTooltipPrefab> prefabClones;

        protected override void OnShowTooltip(ControllerButton button, TooltipRig rig, DefaultTooltipRenderData data, bool wasVisible)
        {
            var prefabClone = CloneOrGetPrefab(button);
            if (prefabClone != null)
            {
                prefabClone.gameObject.SetActive(true);
                prefabClone.ShowRenderData(rig, data);
            }
        }

        protected override void OnHideTooltip(ControllerButton button)
        {
            var prefabClone = GetClonedPrefab(button);
            if (prefabClone != null)
            {
                prefabClone.HideRenderData();
                prefabClone.gameObject.SetActive(false);
            }
        }

        public DefaultTooltipPrefab GetClonedPrefab(ControllerButton button)
        {
            return prefabClones == null ? null : prefabClones[(int)button];
        }

        private DefaultTooltipPrefab CloneOrGetPrefab(ControllerButton button)
        {
            var prefabClone = GetClonedPrefab(button);
            if (prefabClone != null) { return prefabClone; }
            if (m_tooltipPrefab == null) { return null; }

            if (prefabClones == null) { prefabClones = new EnumArray<ControllerButton, DefaultTooltipPrefab>(); }
            var obj = Instantiate(m_tooltipPrefab);
            prefabClones[(int)button] = prefabClone = obj.GetComponent<DefaultTooltipPrefab>();
            prefabClone.name = button.ToString();
            prefabClone.transform.SetParent(m_tooltipParent == null ? transform : m_tooltipParent);
            prefabClone.transform.localPosition = Vector3.zero;
            prefabClone.transform.localRotation = Quaternion.identity;
            return prefabClone;
        }

        public void DestroyAllClonedPrefabs()
        {
            if (prefabClones == null) { return; }
            for (int i = EnumArrayBase<ControllerButton>.StaticMinInt, imax = EnumArrayBase<ControllerButton>.StaticMaxInt; i <= imax; ++i)
            {
                var prefabClone = prefabClones[i];
                if (!ReferenceEquals(prefabClone, null))
                {
                    if (prefabClone != null) { Destroy(prefabClone.gameObject); }
                    prefabClones[i] = null;
                }
            }
        }
    }
}