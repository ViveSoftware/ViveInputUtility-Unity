//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

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
        private const float MIN_DEVICE_VIEW_SCALE = 0.01f;
        private const float MAX_DEVICE_VIEW_SCALE = 5f;

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
        private float m_deviceViewMargin = 20f;
        [SerializeField]
        private bool m_showDebugBoundRect = false;
        [SerializeField]
        private UnityEventString m_onSelectDevice = new UnityEventString();
        [SerializeField]
        private UnityEventString m_onMouseEnterDevice = new UnityEventString();
        [SerializeField]
        private UnityEventString m_onMouseExitDevice = new UnityEventString();

        private bool m_initialized;
        private bool m_trackingEnabled;
        private ViveRole.IMap m_selectedRoleMap;

        private RectTransform m_deviceView;
        private float m_deviceViewMaskWidth;
        private float m_deviceViewMaskHeight;
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

            m_deviceView = m_deviceItem.transform.parent.GetComponent<RectTransform>();
            var deviceViewMaskRect = m_deviceView.parent.GetComponent<RectTransform>().rect;
            m_deviceViewMaskWidth = deviceViewMaskRect.width;
            m_deviceViewMaskHeight = deviceViewMaskRect.height;
        }

        public void EnableTracking()
        {
            if (m_trackingEnabled) { return; }
            m_trackingEnabled = true;

            Initialize();

            m_inputDeviceSN.text = string.Empty;
            CheckInputDeviceSN(string.Empty);

            for (uint deviceIndex = 0, imax = VRModule.GetDeviceStateCount(); deviceIndex < imax; ++deviceIndex)
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

        private Vector3[] m_itemCorners = new Vector3[4];
        private Image m_bound;
        private void Update()
        {
            if (m_trackingEnabled)
            {
                var boundRect = new Rect()
                {
                    xMin = float.MaxValue,
                    xMax = float.MinValue,
                    yMin = float.MaxValue,
                    yMax = float.MinValue,
                };

                if (m_showDebugBoundRect && m_bound == null)
                {
                    var boundObj = new GameObject();
                    boundObj.transform.SetParent(m_deviceView.transform, false);
                    boundObj.transform.SetAsFirstSibling();
                    m_bound = boundObj.AddComponent<Image>();
                    m_bound.color = new Color(1f, 0f, 0f, 5f);
                }

                for (int i = 0, imax = m_itemTable.Count; i < imax; ++i)
                {
                    var item = m_itemTable.GetValueByIndex(i);

                    if (!item.isDisplayed) { continue; }

                    item.UpdatePosition();

                    item.rectTransform.GetWorldCorners(m_itemCorners);

                    for (int j = 0; j < 4; ++j)
                    {
                        m_itemCorners[j] = m_deviceView.InverseTransformPoint(m_itemCorners[j]);
                    }

                    boundRect.xMin = Mathf.Min(boundRect.xMin, m_itemCorners[0].x, m_itemCorners[1].x, m_itemCorners[2].x, m_itemCorners[3].x);
                    boundRect.xMax = Mathf.Max(boundRect.xMax, m_itemCorners[0].x, m_itemCorners[1].x, m_itemCorners[2].x, m_itemCorners[3].x);
                    boundRect.yMin = Mathf.Min(boundRect.yMin, m_itemCorners[0].y, m_itemCorners[1].y, m_itemCorners[2].y, m_itemCorners[3].y);
                    boundRect.yMax = Mathf.Max(boundRect.yMax, m_itemCorners[0].y, m_itemCorners[1].y, m_itemCorners[2].y, m_itemCorners[3].y);
                }

                // calculate view panel's scale to let view rect includes all devices
                var innerWidth = m_deviceViewMaskWidth - (m_deviceViewMargin * 2f);
                var innerHeight = m_deviceViewMaskHeight - (m_deviceViewMargin * 2f);
                var maxBoundWidth = Mathf.Max(Mathf.Abs(boundRect.xMin), Mathf.Abs(boundRect.xMax)) * 2f;
                var maxBoundHeight = Mathf.Max(Mathf.Abs(boundRect.yMin), Mathf.Abs(boundRect.yMax)) * 2f;

                float scale;
                if (innerWidth / innerHeight >= maxBoundWidth / maxBoundHeight)
                {
                    // if viewRect is wider then boundRect
                    scale = innerHeight / maxBoundHeight;
                }
                else
                {
                    // if boundRect is wider then viewRect
                    scale = innerWidth / maxBoundWidth;
                }

                scale = Mathf.Clamp(scale, MIN_DEVICE_VIEW_SCALE, MAX_DEVICE_VIEW_SCALE);

                if (m_showDebugBoundRect)
                {
                    m_bound.rectTransform.sizeDelta = new Vector2(boundRect.width, boundRect.height);
                    m_bound.rectTransform.localPosition = boundRect.center;
                }

                m_deviceView.localScale = new Vector3(scale, scale, 1f);
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

        public void SetAnimatorSlideLeft()
        {
            if (m_animator.isInitialized)
            {
                m_animator.SetTrigger("SlideDeviceViewLeft");
            }
        }

        public void SetAnimatorSlideRight()
        {
            if (m_animator.isInitialized)
            {
                m_animator.SetTrigger("SlideDeviceViewRight");
            }
        }

        public void SetAnimatorIsEditing(bool value)
        {
            if (m_animator.isInitialized)
            {
                m_animator.SetBool("isEditing", value);
            }
        }
    }
}