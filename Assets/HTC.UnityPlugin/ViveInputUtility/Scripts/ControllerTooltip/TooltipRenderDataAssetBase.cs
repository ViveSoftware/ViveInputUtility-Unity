//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public abstract class TooltipRenderDataAssetBase : ScriptableObject { }

    public abstract class TooltipRenderDataAssetBase<TRenderData> : TooltipRenderDataAssetBase, IEnumerable<KeyValuePair<ControllerButton, TRenderData>>
    {
        [Serializable]
        public struct DataEntry
        {
            public ControllerButton button;
            public TRenderData data;
        }

        [SerializeField]
        private List<ControllerButton> m_buttonList = new List<ControllerButton>();
        [SerializeField]
        private List<TRenderData> m_dataList = new List<TRenderData>();

        public virtual IEnumerator<KeyValuePair<ControllerButton, TRenderData>> GetEnumerator()
        {
            var i = 0;
            while (i < m_buttonList.Count && i < m_dataList.Count)
            {
                yield return new KeyValuePair<ControllerButton, TRenderData>(m_buttonList[i], m_dataList[i]);
                ++i;
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}