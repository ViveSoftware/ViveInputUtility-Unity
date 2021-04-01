//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceRoleSetBindingItem : MonoBehaviour
    {
        [SerializeField]
        private Image m_modelIcon;
        [SerializeField]
        private Text m_deviceSN;
        [SerializeField]
        private Text m_roleName;
        [SerializeField]
        private Button m_editButton;
        [SerializeField]
        private Image m_heighLight;

        public string deviceSN { get; set; }
        public bool isHeighLight { get { return m_heighLight.enabled; } set { m_heighLight.enabled = value; } }
        public bool isEditing { get { return m_editButton.interactable; } set { m_editButton.interactable = !value; } }
        public event Action<string> onEditPress;
        public event Action<string> onRemovePress;

        public void RefreshDisplayInfo(ViveRole.IMap roleMap)
        {
            var roleInfo = roleMap.RoleValueInfo;
            var roleValue = roleMap.GetBoundRoleValueByDevice(deviceSN);
            var deviceModel = ViveRoleBindingsHelper.GetDeviceModelHint(deviceSN);

            m_deviceSN.text = deviceSN;
            m_roleName.text = roleInfo.GetNameByRoleValue(roleValue);

            BindingInterfaceSpriteManager.SetupDeviceIcon(m_modelIcon, deviceModel, VRModule.IsDeviceConnected(deviceSN));
        }

        public void OnEdit()
        {
            if (onEditPress != null) { onEditPress(deviceSN); }
        }

        public void OnRemove()
        {
            if (onRemovePress != null) { onRemovePress(deviceSN); }
        }
    }
}