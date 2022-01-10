//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceRoleSetPanelController : MonoBehaviour
    {
        public Type DEFAULT_SELECTED_ROLE = typeof(BodyRole);

        [Serializable]
        public class UnityEventSelectRole : UnityEvent<ViveRole.IMap> { }

        [Serializable]
        public class UnityEventString : UnityEvent<string> { }

        [SerializeField]
        private Animator m_animator;
        [SerializeField]
        private BindingInterfaceRoleSetButtonItem m_roleSetButtonItem;
        [SerializeField]
        private BindingInterfaceRoleSetBindingItem m_bindingItem;
        [SerializeField]
        private UnityEventSelectRole m_onSelectRoleSet;
        [SerializeField]
        private UnityEventString m_onEditBinding;
        [SerializeField]
        private UnityEvent m_onFinishEditBinding;

        private int m_maxBindingCount;
        private List<BindingInterfaceRoleSetButtonItem> m_roleSetButtonList = new List<BindingInterfaceRoleSetButtonItem>();
        private int m_selectedRoleIndex = -1;

        private List<BindingInterfaceRoleSetBindingItem> m_bindingList = new List<BindingInterfaceRoleSetBindingItem>();
        private string m_editingDevice = string.Empty;
        private string m_heighLightDevice = string.Empty;
        private OrderedIndexedSet<string> m_boundDevices = new OrderedIndexedSet<string>();

        public ViveRole.IMap selectedRoleMap { get { return m_roleSetButtonList[m_selectedRoleIndex].roleMap; } }
        public bool isEditing { get { return !string.IsNullOrEmpty(m_editingDevice); } }
        public bool hasHeightLight { get { return !string.IsNullOrEmpty(m_heighLightDevice); } }

        private void Awake()
        {
            RefreshRoleSelection();

            // select the role that have largest binding count
            for (int i = 0, imax = m_roleSetButtonList.Count; i < imax; ++i)
            {
                if (!m_roleSetButtonList[i].roleMap.Handler.BlockBindings && m_roleSetButtonList[i].roleMap.BindingCount == m_maxBindingCount)
                {
                    SelectRoleSet(i);
                    break;
                }
            }
        }

        private void OnEnable()
        {
            VRModule.onDeviceConnected += OnDeviceConnected;
        }

        private void OnDisable()
        {
            VRModule.onDeviceConnected -= OnDeviceConnected;

            FinishEditBinding();
        }

        private void OnDeviceConnected(uint deviceIndex, bool connected)
        {
            RefreshSelectedRoleBindings();
            RefreshRoleSelection();
        }

        public void SetAnimatorSlideLeft()
        {
            if (m_animator.isInitialized)
            {
                m_animator.SetTrigger("SlideRoleSetViewLeft");
            }
        }

        public void SetAnimatorSlideRight()
        {
            if (m_animator.isInitialized)
            {
                m_animator.SetTrigger("SlideRoleSetViewRight");
            }
        }

        public void EnableSelection()
        {
            for (int i = 0, imax = m_roleSetButtonList.Count; i < imax; ++i)
            {
                m_roleSetButtonList[i].interactable = true;
            }
        }

        public void DisableSelection()
        {
            for (int i = 0, imax = m_roleSetButtonList.Count; i < imax; ++i)
            {
                m_roleSetButtonList[i].interactable = false;
            }
        }

        public void SelectRoleSet(int index)
        {
            m_roleSetButtonList[index].SetIsOn();

            if (m_selectedRoleIndex == index) { return; }

            m_selectedRoleIndex = index;

            m_boundDevices.Clear();

            RefreshSelectedRoleBindings();

            if (m_onSelectRoleSet != null)
            {
                m_onSelectRoleSet.Invoke(selectedRoleMap);
            }
        }

        public void StartEditBinding(string deviceSN)
        {
            if (m_editingDevice == deviceSN) { return; }

            FinishEditBindingNoEvent();

            var bindingItemIndex = m_boundDevices.IndexOf(deviceSN);
            if (bindingItemIndex < 0)
            {
                selectedRoleMap.BindDeviceToRoleValue(deviceSN, selectedRoleMap.RoleValueInfo.InvalidRoleValue);

                m_editingDevice = deviceSN;
                RefreshSelectedRoleBindings();
                RefreshRoleSelection();
            }
            else
            {
                m_editingDevice = deviceSN;
                m_bindingList[m_boundDevices.IndexOf(deviceSN)].isEditing = true;
            }

            EnableHeightLightBinding(deviceSN);

            if (m_onEditBinding != null)
            {
                m_onEditBinding.Invoke(m_editingDevice);
            }
        }

        public void FinishEditBindingNoEvent()
        {
            if (isEditing)
            {
                m_bindingList[m_boundDevices.IndexOf(m_editingDevice)].isEditing = false;
            }

            m_editingDevice = string.Empty;
        }

        public void FinishEditBinding()
        {
            FinishEditBindingNoEvent();

            if (m_onFinishEditBinding != null)
            {
                m_onFinishEditBinding.Invoke();
            }
        }

        public void EnableHeightLightBinding(string deviceSN)
        {
            if (m_heighLightDevice == deviceSN) { return; }

            DisableHeightLightBinding();

            if (string.IsNullOrEmpty(deviceSN)) { return; }

            var itemIndex = m_boundDevices.IndexOf(deviceSN);
            if (itemIndex < 0) { return; }

            m_heighLightDevice = deviceSN;
            m_bindingList[itemIndex].isHeighLight = true;
        }

        public void DisableHeightLightBinding()
        {
            if (hasHeightLight)
            {
                m_bindingList[m_boundDevices.IndexOf(m_heighLightDevice)].isHeighLight = false;
                m_heighLightDevice = string.Empty;
            }
        }

        public void RemoveBinding(string deviceSN)
        {
            if (isEditing && m_editingDevice == deviceSN)
            {
                FinishEditBinding();
            }

            selectedRoleMap.UnbindDevice(deviceSN);
            RefreshRoleSelection();
            RefreshSelectedRoleBindings();
        }

        public void RefreshRoleSelection()
        {
            ViveRole.Initialize();

            if (m_roleSetButtonList.Count == 0)
            {
                m_roleSetButtonList.Add(m_roleSetButtonItem);
                m_roleSetButtonItem.index = 0;
                m_roleSetButtonItem.onSelected += SelectRoleSet;
            }

            m_maxBindingCount = 0;
            var buttonIndex = 0;
            for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
            {
                var roleType = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i);
                var roleMap = ViveRole.GetMap(roleType);

                if (roleMap.Handler.BlockBindings) { continue; }

                BindingInterfaceRoleSetButtonItem item;
                if (buttonIndex >= m_roleSetButtonList.Count)
                {
                    var itemObj = Instantiate(m_roleSetButtonItem.gameObject);
                    itemObj.transform.SetParent(m_roleSetButtonItem.transform.parent, false);

                    m_roleSetButtonList.Add(item = itemObj.GetComponent<BindingInterfaceRoleSetButtonItem>());
                    item.index = buttonIndex;
                    item.onSelected += SelectRoleSet;
                }
                else
                {
                    item = m_roleSetButtonList[buttonIndex];
                }

                m_maxBindingCount = Mathf.Max(m_maxBindingCount, roleMap.BindingCount);
                item.roleMap = roleMap;

                ++buttonIndex;
            }
        }

        private IndexedSet<string> m_tempDevices = new OrderedIndexedSet<string>();
        public void RefreshSelectedRoleBindings()
        {
            var roleMap = m_roleSetButtonList[m_selectedRoleIndex].roleMap;
            var bindingTable = roleMap.BindingTable;

            // update bound device list and keep the original order
            for (int i = 0, imax = m_boundDevices.Count; i < imax; ++i) { m_tempDevices.Add(m_boundDevices[i]); }
            for (int i = 0, imax = bindingTable.Count; i < imax; ++i)
            {
                var boundDevice = bindingTable.GetKeyByIndex(i);
                if (!m_tempDevices.Remove(boundDevice))
                {
                    m_boundDevices.Add(boundDevice);
                }
            }
            for (int i = 0, imax = m_tempDevices.Count; i < imax; ++i) { m_boundDevices.Remove(m_tempDevices[i]); }
            m_tempDevices.Clear();

            if (m_bindingList.Count == 0)
            {
                m_bindingList.Add(m_bindingItem);
                m_bindingItem.onEditPress += StartEditBinding;
                m_bindingItem.onRemovePress += RemoveBinding;
            }

            var bindingIndex = 0;
            for (int max = m_boundDevices.Count; bindingIndex < max; ++bindingIndex)
            {
                BindingInterfaceRoleSetBindingItem item;
                if (bindingIndex >= m_bindingList.Count)
                {
                    var itemObj = Instantiate(m_bindingItem.gameObject);
                    itemObj.transform.SetParent(m_bindingItem.transform.parent, false);

                    // set child index to secnd last, last index is for add item
                    itemObj.transform.SetSiblingIndex(itemObj.transform.parent.childCount - 2);

                    m_bindingList.Add(item = itemObj.GetComponent<BindingInterfaceRoleSetBindingItem>());
                    item.onEditPress += StartEditBinding;
                    item.onRemovePress += RemoveBinding;
                }
                else
                {
                    item = m_bindingList[bindingIndex];
                }

                item.gameObject.SetActive(true);
                item.deviceSN = m_boundDevices[bindingIndex];
                item.isEditing = isEditing && item.deviceSN == m_editingDevice;
                item.isHeighLight = hasHeightLight && item.deviceSN == m_heighLightDevice;
                item.RefreshDisplayInfo(roleMap);
            }

            // FIXME: issue in 2017.2.0b2, item won't refresh at the first time, force refresh
            m_bindingItem.transform.parent.gameObject.SetActive(false);
            m_bindingItem.transform.parent.gameObject.SetActive(true);

            for (int max = m_bindingList.Count; bindingIndex < max; ++bindingIndex)
            {
                m_bindingList[bindingIndex].gameObject.SetActive(false);
            }
        }
    }
}