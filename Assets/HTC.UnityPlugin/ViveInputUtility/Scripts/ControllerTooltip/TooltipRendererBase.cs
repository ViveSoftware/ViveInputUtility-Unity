//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [Serializable]
    public class DeviceTooltipRigSet : EnumArray<VRModuleDeviceModel, TooltipRigAsset> { }

    public class TooltipRendererBase : MonoBehaviour, IViveRoleComponent
    {
        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private DeviceTooltipRigSet m_customTooltipRigSet;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public bool TryGetValidTooltipRig(ControllerButton button, out TooltipRig rig)
        {
            rig = default(TooltipRig);
            if (!EnumArrayBase<ControllerButton>.StaticIsDefined((int)button)) { return false; }

            var model = VRModule.GetDeviceState(m_viveRole.GetDeviceIndex()).deviceModel;

            TooltipRigAsset rigSetAsset = null;
            if (m_customTooltipRigSet != null)
            {
                rigSetAsset = m_customTooltipRigSet[(int)model];
            }

            if (rigSetAsset != null || TooltipRigAsset.TryGetDefaultAsset(model, out rigSetAsset))
            {
                var entries = rigSetAsset.rigEntries;
                if (entries != null && entries.Count > 0)
                {
                    for (int i = 0, imax = entries.Count; i < imax; ++i)
                    {
                        var entry = entries[i];
                        if (entry.button == button)
                        {
                            rig = entry.tooltipRig;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    public abstract class TooltipRendererBase<TRenderData> : TooltipRendererBase
    {
        private enum State
        {
            Invalid,
            Hidden,
            Visible,
        }

        private struct DataState
        {
            public State state;
            public TRenderData data;
            public bool isVisible { get { return state == State.Visible; } }
            public bool isValid { get { return state != State.Invalid; } }
        }

        private EnumArray<ControllerButton, DataState> dataStateArray;
        private static EnumArray<ControllerButton, bool> btnVisibleTmp;

        protected virtual void Awake()
        {
            viveRole.onDeviceIndexChanged += OnDeviceIndexChanged;
        }

        protected virtual void OnDestroy()
        {
            viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;
        }

        private void OnDeviceIndexChanged(uint deviceIndex)
        {
            if (dataStateArray == null) { return; }

            for (ControllerButton i = EnumArrayBase<ControllerButton>.StaticMin, imax = EnumArrayBase<ControllerButton>.StaticMax; i <= imax; ++i)
            {
                var state = dataStateArray[(int)i];
                if (!state.isValid) { continue; }

                TooltipRig rig;
                var shouldShow = TryGetValidTooltipRig(i, out rig);
                var wasVisible = state.isVisible;

                if (wasVisible)
                {
                    if (shouldShow)
                    {
                        OnShowTooltip(i, rig, state.data, true);
                    }
                    else
                    {
                        dataStateArray[(int)i] = new DataState()
                        {
                            state = State.Hidden,
                            data = state.data,
                        };
                        OnHideTooltip(i);
                    }
                }
                else
                {
                    if (shouldShow)
                    {
                        dataStateArray[(int)i] = new DataState()
                        {
                            state = State.Visible,
                            data = state.data,
                        };
                        OnShowTooltip(i, rig, state.data, false);
                    }
                }
            }
        }

        public bool TryGetValidTooltipData(ControllerButton button, out TRenderData data)
        {
            data = default(TRenderData);
            if (dataStateArray == null) { return false; }
            if (!EnumArrayBase<ControllerButton>.StaticIsDefined(button)) { return false; }
            if (!dataStateArray[(int)button].isValid) { return false; }
            data = dataStateArray[(int)button].data;
            return true;
        }

        public bool IsTooltipVisible(ControllerButton button) { return dataStateArray == null ? false : dataStateArray[(int)button].isVisible; }

        public void SetTooltipData(IEnumerable<KeyValuePair<ControllerButton, TRenderData>> dataEnumerable)
        {
            SetTooltipData(dataEnumerable.GetEnumerator());
        }

        public void SetTooltipData(IEnumerator<KeyValuePair<ControllerButton, TRenderData>> dataEnumerator)
        {
            if (dataEnumerator == null)
            {
                ClearTooltipData();
                return;
            }

            if (btnVisibleTmp == null) { btnVisibleTmp = new EnumArray<ControllerButton, bool>(); }

            while (dataEnumerator.MoveNext())
            {
                var entry = dataEnumerator.Current;
                if (EnumArrayBase<ControllerButton>.StaticIsDefined((int)entry.Key))
                {
                    btnVisibleTmp[(int)entry.Key] = true;
                    SetTooltipData(entry.Key, entry.Value);
                }
            }

            for (ControllerButton i = EnumArrayBase<ControllerButton>.StaticMin, imax = EnumArrayBase<ControllerButton>.StaticMax; i <= imax; ++i)
            {
                if (btnVisibleTmp[(int)i])
                {
                    btnVisibleTmp[(int)i] = false;
                }
                else
                {
                    ResetTooltipData(i);
                }
            }
        }

        public void SetTooltipData(ControllerButton button, TRenderData data)
        {
            if (dataStateArray == null) { dataStateArray = new EnumArray<ControllerButton, DataState>(); }
            var wasVisible = dataStateArray[(int)button].isVisible;

            TooltipRig rig;
            var shouldShow = TryGetValidTooltipRig(button, out rig);

            dataStateArray[(int)button] = new DataState()
            {
                state = shouldShow ? State.Visible : State.Hidden,
                data = data,
            };

            if (shouldShow)
            {
                OnShowTooltip(button, rig, data, wasVisible);
            }
        }

        public void ResetTooltipData(ControllerButton button)
        {
            if (dataStateArray != null)
            {
                var wasVisible = dataStateArray[(int)button].isVisible;
                dataStateArray[(int)button] = default(DataState);
                if (wasVisible)
                {
                    OnHideTooltip(button);
                }
            }
        }

        public void ClearTooltipData()
        {
            for (ControllerButton i = EnumArrayBase<ControllerButton>.StaticMin, imax = EnumArrayBase<ControllerButton>.StaticMax; i <= imax; ++i)
            {
                ResetTooltipData(i);
            }
        }

        protected abstract void OnShowTooltip(ControllerButton button, TooltipRig rig, TRenderData data, bool wasVisible);

        protected virtual void OnHideTooltip(ControllerButton button) { }
    }

    public abstract class TooltipRendererBase<TRenderDataAsset, TRenderData> : TooltipRendererBase<TRenderData> where TRenderDataAsset : TooltipRenderDataAssetBase<TRenderData>
    {
        [SerializeField]
        private TRenderDataAsset m_defaultRenderData;

        protected override void Awake()
        {
            base.Awake();

            if (m_defaultRenderData != null)
            {
                ShowTooltipsByAsset(m_defaultRenderData);
            }
        }

        public void ShowTooltipsByAsset(TRenderDataAsset dataSetAsset)
        {
            SetTooltipData(dataSetAsset);
        }
    }
}