//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceDeviceItem : MonoBehaviour
    , IPointerEnterHandler
    , IPointerExitHandler
    {
        [SerializeField]
        private Image m_imageModel;
        [SerializeField]
        private Button m_button;

        public RectTransform rectTransform { get { return m_imageModel.rectTransform; } }

        public bool isDisplayed { get { return m_imageModel.enabled; } }

        public bool isBound { get; set; }

        public uint deviceIndex { get; set; }

        public event Action<string> onClick;
        public event Action<uint> onEnter;
        public event Action<uint> onExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (onEnter != null) { onEnter(deviceIndex); }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (onEnter != null) { onExit(deviceIndex); }
        }

        public void OnClick()
        {
            if (onClick != null) { onClick(VRModule.GetCurrentDeviceState(deviceIndex).serialNumber); }
        }

        public void UpdateModel()
        {
            BindingInterfaceSpriteManager.SetupTrackingDeviceIcon(m_imageModel, VRModule.GetCurrentDeviceState(deviceIndex), isBound);
        }

        public void UpdatePosition()
        {
            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);
            var devicePose = deviceState.pose;

            transform.localPosition = new Vector3(devicePose.pos.x, devicePose.pos.z, 0f) * 100f;
            transform.localRotation = Quaternion.Euler(0f, 0f, -devicePose.rot.eulerAngles.y);
        }
    }
}