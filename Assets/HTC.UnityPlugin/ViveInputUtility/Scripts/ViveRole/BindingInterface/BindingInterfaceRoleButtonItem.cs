//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

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

        private bool m_disableEventOnce;

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

        public void SetIsOnNoEvent()
        {
            if (!m_toggle.isOn)
            {
                m_disableEventOnce = true;
                m_toggle.isOn = true;
            }
        }

        public void OnValueChanged(bool isOn)
        {
            if (m_disableEventOnce)
            {
                m_disableEventOnce = false;
            }
            else if (isOn)
            {
                if (onValueChanged != null) { onValueChanged(roleValue); }
            }
        }
    }
}