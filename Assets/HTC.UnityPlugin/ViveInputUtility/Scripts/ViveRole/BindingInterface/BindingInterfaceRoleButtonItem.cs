//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceRoleButtonItem : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_toggle;
        [SerializeField]
        private Text m_textRoleName;

        public string roleName { get { return m_textRoleName.text; } set { m_textRoleName.text = value; } }
        public int roleValue { get; set; }
        public event Action<int> onValueChanged;

        public void SetIsOn()
        {
            if (!m_toggle.isOn)
            {
                m_toggle.isOn = true;
                m_toggle.group.NotifyToggleOn(m_toggle);
            }
        }

        public void OnValueChanged(bool isOn)
        {
            if (isOn)
            {
                if (onValueChanged != null) { onValueChanged(roleValue); }
            }
        }
    }
}