//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [Serializable]
    public struct TooltipRig
    {
        public Vector3 buttonPosition;
        public Vector3 buttonNormal;
        public Vector3 labelPosition;
        public Vector3 labelNormal;
        public Vector3 labelUp;
        public TextAnchor labelAnchor;
    }

    //[CreateAssetMenu(menuName = "HTC/TooltipRigSetAsset", fileName = "TooltipRigSetAsset")]
    public class TooltipRigAsset : ScriptableObject
    {
        [Serializable]
        public struct TooltipRigEntry
        {
            public ControllerButton button;
            public TooltipRig tooltipRig;
        }

        [SerializeField]
        private List<TooltipRigEntry> m_rigEntries = new List<TooltipRigEntry>();

        public List<TooltipRigEntry> rigEntries { get { return m_rigEntries; } }

        private struct LoadedRigSet
        {
            public bool haveLoaded;
            public TooltipRigAsset asset;
        }

        private static EnumArray<VRModuleDeviceModel, LoadedRigSet> s_defaultTooltipRigSets;

        public static bool TryGetDefaultAsset(VRModuleDeviceModel model, out TooltipRigAsset rigAsset)
        {
            if (!EnumArrayBase<VRModuleDeviceModel>.StaticIsValidIndex((int)model)) { rigAsset = null; return false; }

            if (s_defaultTooltipRigSets == null) { s_defaultTooltipRigSets = new EnumArray<VRModuleDeviceModel, LoadedRigSet>(); }

            var loadedRigSet = s_defaultTooltipRigSets[(int)model];
            if (!loadedRigSet.haveLoaded)
            {
                TooltipRigAsset asset = null;
                int modelNameIndex;
                var info = EnumUtils.GetDisplayInfo(typeof(VRModuleDeviceModel));
                if (info.value2displayedIndex.TryGetValue((int)model, out modelNameIndex))
                {
                    asset = Resources.Load<TooltipRigAsset>("TooltipRig/VIUTooltipRig" + info.displayedNames[modelNameIndex]);
                }

                s_defaultTooltipRigSets[(int)model] = loadedRigSet = new LoadedRigSet()
                {
                    haveLoaded = true,
                    asset = asset,
                };
            }

            if (loadedRigSet.asset == null) { rigAsset = null; return false; }

            rigAsset = loadedRigSet.asset;
            return rigAsset != null;
        }

        public static void ClearDefaultAssetCache()
        {
            if (s_defaultTooltipRigSets != null)
            {
                s_defaultTooltipRigSets.Clear();
            }
        }
    }
}