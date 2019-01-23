//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceRolePanelController : MonoBehaviour
    {
        [SerializeField]
        private BindingInterfaceRoleButtonItem m_roleButtonItem;
        [SerializeField]
        private UnityEvent m_onBoundDevcieToRole;

        private List<BindingInterfaceRoleButtonItem> m_roleButtonList = new List<BindingInterfaceRoleButtonItem>();
        private ViveRole.IMap m_selectedRoleMap;
        private string m_editingDeviceSN;

        public void SelectRole(int roleValue)
        {
            if (m_selectedRoleMap == null || string.IsNullOrEmpty(m_editingDeviceSN)) { return; }

            m_selectedRoleMap.BindDeviceToRoleValue(m_editingDeviceSN, roleValue);

            if (m_onBoundDevcieToRole != null)
            {
                m_onBoundDevcieToRole.Invoke();
            }
        }

        public void SelecRoleSet(ViveRole.IMap roleMap)
        {
            if (m_roleButtonList.Count == 0)
            {
                m_roleButtonList.Add(m_roleButtonItem);
                m_roleButtonItem.onValueChanged += SelectRole;
            }

            var roleInfo = roleMap.RoleValueInfo;

            // update buttons
            if (m_selectedRoleMap != roleMap)
            {
                m_selectedRoleMap = roleMap;

                m_roleButtonList[0].roleValue = roleInfo.InvalidRoleValue;
                m_roleButtonList[0].roleName = roleInfo.GetNameByRoleValue(roleInfo.InvalidRoleValue);

                var buttonIndex = 1;
                for (int roleValue = roleInfo.MinValidRoleValue, max = roleInfo.MaxValidRoleValue; roleValue <= max; ++roleValue)
                {
                    if (!roleInfo.IsValidRoleValue(roleValue)) { continue; }

                    BindingInterfaceRoleButtonItem item;
                    if (buttonIndex >= m_roleButtonList.Count)
                    {
                        var itemObj = Instantiate(m_roleButtonItem.gameObject);
                        itemObj.transform.SetParent(m_roleButtonItem.transform.parent, false);

                        m_roleButtonList.Add(item = itemObj.GetComponent<BindingInterfaceRoleButtonItem>());
                        item.onValueChanged += SelectRole;
                    }
                    else
                    {
                        item = m_roleButtonList[buttonIndex];
                    }

                    item.gameObject.SetActive(true);
                    item.roleValue = roleValue;
                    item.roleName = roleInfo.GetNameByRoleValue(roleValue);

                    ++buttonIndex;
                }

                for (int max = m_roleButtonList.Count; buttonIndex < max; ++buttonIndex)
                {
                    m_roleButtonList[buttonIndex].gameObject.SetActive(false);
                }
            }
        }

        public void SelectBindingDevice(string deviceSN)
        {
            // update selected role
            m_editingDeviceSN = deviceSN;
            if (m_selectedRoleMap.IsDeviceBound(deviceSN))
            {
                var validRoleFound = false;
                var boundRoleValue = m_selectedRoleMap.GetBoundRoleValueByDevice(deviceSN);
                for (int i = 0, imax = m_roleButtonList.Count; i < imax && m_roleButtonList[i].gameObject.activeSelf; ++i)
                {
                    if (m_roleButtonList[i].roleValue == boundRoleValue)
                    {
                        m_roleButtonList[i].SetIsOnNoEvent();
                        validRoleFound = true;
                        break;
                    }
                }

                if (!validRoleFound)
                {
                    m_roleButtonList[0].SetIsOnNoEvent();
                }
            }
            else
            {
                m_roleButtonList[0].SetIsOnNoEvent();
            }
        }
    }
}