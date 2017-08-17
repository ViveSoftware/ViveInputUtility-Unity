//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceConfigPanelController : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_toggleApplyOnStart;
        [SerializeField]
        private GameObject m_dirtySymble;

        private void Awake()
        {
            if (EventSystem.current == null)
            {
                new GameObject("[EventSystem]", typeof(EventSystem)).AddComponent<StandaloneInputModule>();
            }
            else if (EventSystem.current.GetComponent<StandaloneInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            }

            ViveRoleBindingsHelper.AutoLoadConfig();

            m_toggleApplyOnStart.isOn = ViveRoleBindingsHelper.bindingConfig.apply_bindings_on_load;
        }

        public void SetDirty()
        {
            m_dirtySymble.SetActive(true);
        }

        public void ToggleBindingInterface()
        {
            ViveRoleBindingsHelper.ToggleBindingInterface();
        }

        public void ReloadConfig()
        {
            ViveRoleBindingsHelper.LoadBindingConfigFromFile(ViveRoleBindingsHelper.AUTO_LOAD_CONFIG_PATH);
            ViveRoleBindingsHelper.ApplyBindingConfigToRoleMap();

            m_toggleApplyOnStart.isOn = ViveRoleBindingsHelper.bindingConfig.apply_bindings_on_load;

            m_dirtySymble.SetActive(false);
        }

        public void SaveConfig()
        {
            ViveRoleBindingsHelper.bindingConfig.apply_bindings_on_load = m_toggleApplyOnStart.isOn;

            ViveRoleBindingsHelper.LoadBindingConfigFromRoleMap();
            ViveRoleBindingsHelper.SaveBindingConfigToFile(ViveRoleBindingsHelper.AUTO_LOAD_CONFIG_PATH);

            m_dirtySymble.SetActive(false);
        }
    }
}