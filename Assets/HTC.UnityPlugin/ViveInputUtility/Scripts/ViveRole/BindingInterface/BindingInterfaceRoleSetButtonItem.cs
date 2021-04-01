//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceRoleSetButtonItem : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_toggle;
        [SerializeField]
        private Text m_textName;

        private ViveRole.IMap m_roleMap;

        public event Action<int> onSelected;

        public bool interactable { get { return m_toggle.interactable; } set { m_toggle.interactable = value; } }
        public int index { get; set; }

        public ViveRole.IMap roleMap
        {
            get { return m_roleMap; }
            set
            {
                m_roleMap = value;

                if (m_roleMap.BindingCount > 0)
                {
                    m_textName.text = value.RoleValueInfo.RoleEnumType.Name + "(" + value.BindingCount + ")";
                }
                else
                {
                    m_textName.text = value.RoleValueInfo.RoleEnumType.Name;
                }
            }
        }

        public void SetIsOn()
        {
            if (!m_toggle.isOn)
            {
                m_toggle.isOn = true;
                m_toggle.group.RegisterToggle(m_toggle);
                m_toggle.group.NotifyToggleOn(m_toggle);
            }
        }

        public void OnValueChanged(bool isOn)
        {
            if (isOn)
            {
                if (onSelected != null) { onSelected(index); }
            }
        }
    }
}