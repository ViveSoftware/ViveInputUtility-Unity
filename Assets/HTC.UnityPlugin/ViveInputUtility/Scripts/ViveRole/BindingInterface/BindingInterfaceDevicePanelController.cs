//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceDevicePanelController : MonoBehaviour
    {
        [Serializable]
        public class UnityEventString : UnityEvent<string> { }

        [SerializeField]
        private Animator m_animator;
        [SerializeField]
        private InputField m_inputDeviceSN;
        [SerializeField]
        private Image m_modelIcon;
        [SerializeField]
        private Button m_buttonCheck;
        [SerializeField]
        private BindingInterfaceDeviceItem m_deviceItem;
        [SerializeField]
        private UnityEventString m_onSelectDevice = new UnityEventString();
        [SerializeField]
        private UnityEventString m_onMouseEnterDevice = new UnityEventString();
        [SerializeField]
        private UnityEventString m_onMouseExitDevice = new UnityEventString();

        private bool m_initialized;
        private bool m_trackingEnabled;
        private ViveRole.IMap m_selectedRoleMap;

        private IndexedTable<uint, BindingInterfaceDeviceItem> m_itemTable = new IndexedTable<uint, BindingInterfaceDeviceItem>();
        private List<BindingInterfaceDeviceItem> m_itemPool = new List<BindingInterfaceDeviceItem>();

        private void Initialize()
        {
            if (m_initialized) { return; }
            m_initialized = true;

            m_itemPool.Add(m_deviceItem);

            m_deviceItem.onClick += m_onSelectDevice.Invoke;
            m_deviceItem.onEnter += OnEnterDevice;
            m_deviceItem.onExit += OnExitDevice;
            m_deviceItem.gameObject.SetActive(false);
        }

        public void EnableTracking()
        {
            if (m_trackingEnabled) { return; }
            m_trackingEnabled = true;

            Initialize();

            m_inputDeviceSN.text = string.Empty;
            CheckInputDeviceSN(string.Empty);

            for (uint deviceIndex = 0; deviceIndex < VRModule.MAX_DEVICE_COUNT; ++deviceIndex)
            {
                if (VRModule.GetCurrentDeviceState(deviceIndex).isConnected)
                {
                    OnDeviceConnected(deviceIndex, true);
                }
            }

            VRModule.onDeviceConnected += OnDeviceConnected;
        }

        public void DisableTracking()
        {
            if (!m_trackingEnabled) { return; }
            m_trackingEnabled = false;

            VRModule.onDeviceConnected -= OnDeviceConnected;

            for (int i = 0, imax = m_itemTable.Count; i < imax; ++i)
            {
                var item = m_itemTable.GetValueByIndex(i);
                m_itemPool.Add(m_itemTable.GetValueByIndex(i));
                item.gameObject.SetActive(false);
            }
            m_itemTable.Clear();
        }

        private void OnDisable()
        {
            DisableTracking();
        }

        private void Update()
        {
            if (m_trackingEnabled)
            {
                for (int i = 0, imax = m_itemTable.Count; i < imax; ++i)
                {
                    m_itemTable.GetValueByIndex(i).UpdatePosition();
                }
            }
        }

        private void OnDeviceConnected(uint deviceIndex, bool connected)
        {
            BindingInterfaceDeviceItem item;

            if (connected)
            {
                if (m_itemPool.Count == 0)
                {
                    var itemObj = Instantiate(m_deviceItem.gameObject);
                    itemObj.transform.SetParent(m_deviceItem.transform.parent, false);
                    item = itemObj.GetComponent<BindingInterfaceDeviceItem>();

                    item.onClick += m_onSelectDevice.Invoke;
                    item.onEnter += OnEnterDevice;
                    item.onExit += OnExitDevice;
                }
                else
                {
                    item = m_itemPool[m_itemPool.Count - 1];
                    m_itemPool.RemoveAt(m_itemPool.Count - 1); // remove last
                }

                m_itemTable.Add(deviceIndex, item);
                item.deviceIndex = deviceIndex;
                item.isBound = m_selectedRoleMap.IsDeviceConnectedAndBound(deviceIndex);
                item.UpdateModel();
            }
            else
            {
                item = m_itemTable[deviceIndex];
                m_itemTable.Remove(deviceIndex);

                m_itemPool.Add(item);
            }

            item.gameObject.SetActive(connected);
        }

        private void OnEnterDevice(uint deviceIndex)
        {
            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);
            var deviceSN = deviceState.serialNumber;

            m_inputDeviceSN.text = deviceSN;
            CheckInputDeviceSN(deviceSN);

            m_modelIcon.gameObject.SetActive(true);
            BindingInterfaceSpriteManager.SetupDeviceIcon(m_modelIcon, deviceState.deviceModel, true);

            if (m_onMouseEnterDevice != null)
            {
                m_onMouseEnterDevice.Invoke(deviceSN);
            }
        }

        private void OnExitDevice(uint deviceIndex)
        {
            var deviceSN = VRModule.GetCurrentDeviceState(deviceIndex).serialNumber;

            m_inputDeviceSN.text = string.Empty;
            CheckInputDeviceSN(string.Empty);

            m_modelIcon.gameObject.SetActive(false);

            if (m_onMouseExitDevice != null)
            {
                m_onMouseExitDevice.Invoke(deviceSN);
            }
        }

        public void CheckInputDeviceSN(string inputStr)
        {
            if (string.IsNullOrEmpty(inputStr))
            {
                m_buttonCheck.interactable = false;
                m_modelIcon.gameObject.SetActive(false);
            }
            else
            {
                m_buttonCheck.interactable = true;
                m_modelIcon.gameObject.SetActive(true);
                uint deviceIndex;
                if (VRModule.TryGetConnectedDeviceIndex(inputStr, out deviceIndex))
                {
                    BindingInterfaceSpriteManager.SetupDeviceIcon(m_modelIcon, VRModule.GetCurrentDeviceState(deviceIndex).deviceModel, true);
                }
                else
                {
                    BindingInterfaceSpriteManager.SetupDeviceIcon(m_modelIcon, ViveRoleBindingsHelper.GetDeviceModelHint(inputStr), false);
                }
            }
        }

        public void UpdateForBindingChanged()
        {
            for (int i = 0, imax = m_itemTable.Count; i < imax; ++i)
            {
                var deviceIndex = m_itemTable.GetKeyByIndex(i);
                var item = m_itemTable.GetValueByIndex(i);

                item.isBound = m_selectedRoleMap.IsDeviceConnectedAndBound(deviceIndex);
                item.UpdateModel();
            }
        }

        public void ConfirmInputDeviceSN()
        {
            if (m_onSelectDevice != null)
            {
                m_onSelectDevice.Invoke(m_inputDeviceSN.text);
            }
        }

        public void DeselectCheckButton()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void SelecRoleSet(ViveRole.IMap roleMap)
        {
            m_selectedRoleMap = roleMap;
        }

        public void SetAnimatorIsEditing(bool value)
        {
            m_animator.SetBool("isEditing", value);
        }
    }
}