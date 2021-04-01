//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.BindingInterface
{
    public class BindingInterfaceConfigPanelController : MonoBehaviour
    {
        [SerializeField]
        private bool m_closeExCamOnEnable = true;
        [SerializeField]
        private Text m_pathInfo;
        [SerializeField]
        private GameObject m_dirtySymble;

        private bool m_exCamTrunedOff;

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

            m_pathInfo.text = "The changes will be stored in \"" + VIUSettings.bindingConfigFilePath + "\".";
        }

        private void OnDisable()
        {
            if (ExternalCameraHook.Active && !ExternalCameraHook.Instance.enabled && m_exCamTrunedOff)
            {
                ExternalCameraHook.Instance.enabled = true;
            }

            m_exCamTrunedOff = false;
        }

        private void Update()
        {
            if (m_closeExCamOnEnable && ExternalCameraHook.Active && ExternalCameraHook.Instance.enabled)
            {
                ExternalCameraHook.Instance.enabled = false;
                m_exCamTrunedOff = true;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseBindingInterface();
            }
        }

        public void SetDirty()
        {
            m_dirtySymble.SetActive(true);
        }

        public void CloseBindingInterface()
        {
            ViveRoleBindingsHelper.DisableBindingInterface();
        }

        public void ReloadConfig()
        {
            ViveRoleBindingsHelper.LoadBindingConfigFromFile(VIUSettings.bindingConfigFilePath);

            // Unbind all applied bindings
            for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
            {
                var roleType = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i);
                var roleMap = ViveRole.GetMap(roleType);

                roleMap.UnbindAll();
            }

            ViveRoleBindingsHelper.ApplyBindingConfigToRoleMap();

            m_dirtySymble.SetActive(false);
        }

        public void SaveConfig()
        {
            ViveRoleBindingsHelper.LoadBindingConfigFromRoleMap();
            ViveRoleBindingsHelper.SaveBindingConfigToFile(VIUSettings.bindingConfigFilePath);

            m_dirtySymble.SetActive(false);
        }
    }
}