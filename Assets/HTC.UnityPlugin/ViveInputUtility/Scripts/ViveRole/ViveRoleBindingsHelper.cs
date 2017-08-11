//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using HTC.UnityPlugin.Utility;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Vive
{
    public class ViveRoleBindingsHelper : SingletonBehaviour<ViveRoleBindingsHelper>
    {
        [Serializable]
        public struct Binding
        {
            [FormerlySerializedAs("sn")]
            public string device_sn;
            public string role_name;
            [FormerlySerializedAs("sv")]
            public int role_value;
        }

        [Serializable]
        public struct RoleData
        {
            public string type;
            public Binding[] bindings;
        }

        [Serializable]
        public struct BindingConfig
        {
            public bool apply_bindings_on_load;
            public RoleData[] roles;
        }

        public const string AUTO_LOAD_CONFIG_PATH = "viu_binding.cfg";

        private static bool s_isAutoLoaded;
        private static BindingConfig s_bindingConfig;

        public static BindingConfig bindingConfig { get { return s_bindingConfig; } set { s_bindingConfig = value; } }

        [SerializeField]
        private string m_viveRoleBindingsConfigPath = AUTO_LOAD_CONFIG_PATH; // "./vive_role_bindings.cfg"

        [RuntimeInitializeOnLoadMethod]
        private static void AutoLoadConfig()
        {
            if (s_isAutoLoaded) { return; }
            s_isAutoLoaded = true;

            var configPath = AUTO_LOAD_CONFIG_PATH;

            if (Active && string.IsNullOrEmpty(Instance.m_viveRoleBindingsConfigPath))
            {
                configPath = Instance.m_viveRoleBindingsConfigPath;
            }

            if (File.Exists(configPath))
            {
                LoadBindingConfigFromFile(configPath);

                if (s_bindingConfig.apply_bindings_on_load)
                {
                    ApplyBindingConfigToRoleMap();
                }
            }
            else
            {
                s_bindingConfig = new BindingConfig()
                {
                    apply_bindings_on_load = true,
                };
            }
        }

        private void Awake()
        {
            AutoLoadConfig();
        }

        public static void LoadBindingConfigFromRoleMap(params Type[] roleTypeFilter)
        {
            var roleCount = ViveRoleEnum.ValidViveRoleTable.Count;
            var roleDataList = ListPool<RoleData>.Get();
            var filterUsed = roleTypeFilter != null && roleTypeFilter.Length > 0;

            if (filterUsed)
            {
                roleDataList.AddRange(s_bindingConfig.roles);
            }

            for (int i = 0; i < roleCount; ++i)
            {
                var roleType = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i);
                var roleName = ViveRoleEnum.ValidViveRoleTable.GetKeyByIndex(i);
                var roleMap = ViveRole.GetMap(roleType);

                if (filterUsed)
                {
                    // apply filter
                    var filtered = false;
                    foreach (var t in roleTypeFilter) { if (roleType == t) { filtered = true; break; } }
                    if (!filtered) { continue; }
                }

                if (roleMap.BindingCount > 0)
                {
                    var bindingTable = roleMap.BindingTable;

                    var roleData = new RoleData()
                    {
                        type = roleName,
                        bindings = new Binding[bindingTable.Count],
                    };

                    for (int j = 0, jmax = bindingTable.Count; j < jmax; ++j)
                    {
                        var binding = new Binding();
                        binding.device_sn = bindingTable.GetKeyByIndex(j);
                        binding.role_value = bindingTable.GetValueByIndex(j);
                        binding.role_name = roleMap.RoleValueInfo.GetNameByRoleValue(binding.role_value);

                        roleData.bindings[j] = binding;
                    }

                    if (filterUsed)
                    {
                        // merge with existed role data
                        var roleDataIndex = roleDataList.FindIndex((item) => item.type == roleName);
                        if (roleDataIndex >= 0)
                        {
                            roleDataList[roleDataIndex] = roleData;
                        }
                        else
                        {
                            roleDataList.Add(roleData);
                        }
                    }
                    else
                    {
                        roleDataList.Add(roleData);
                    }
                }
                else
                {
                    if (roleDataList.Count > 0)
                    {
                        // don't write to config if no bindings
                        roleDataList.RemoveAll((item) => item.type == roleName);
                    }
                }
            }

            s_bindingConfig.roles = roleDataList.ToArray();

            ListPool<RoleData>.Release(roleDataList);
        }

        public static void ApplyBindingConfigToRoleMap(params Type[] roleTypeFilter)
        {
            var filterUsed = roleTypeFilter != null && roleTypeFilter.Length > 0;

            foreach (var roleData in s_bindingConfig.roles)
            {
                Type roleType;
                if (string.IsNullOrEmpty(roleData.type) || !ViveRoleEnum.ValidViveRoleTable.TryGetValue(roleData.type, out roleType)) { continue; }

                if (filterUsed)
                {
                    // apply filter
                    var filtered = false;
                    foreach (var t in roleTypeFilter) { if (roleType == t) { filtered = true; break; } }
                    if (!filtered) { continue; }
                }

                var roleMap = ViveRole.GetMap(roleType);
                roleMap.UnbindAll();

                foreach (var binding in roleData.bindings)
                {
                    if (roleMap.IsDeviceBound(binding.device_sn)) { continue; } // skip if device already bound

                    // bind device according to role_name first
                    // if role_name is invalid then role_value is used
                    int roleValue;
                    if (string.IsNullOrEmpty(binding.role_name) || !roleMap.RoleValueInfo.TryGetRoleValueByName(binding.role_name, out roleValue))
                    {
                        roleValue = binding.role_value;
                    }

                    roleMap.BindDeviceToRoleValue(binding.device_sn, roleValue);
                }
            }
        }

        public static void SaveBindingConfigToFile(string configPath, bool prettyPrint = true)
        {
            using (var outputFile = new StreamWriter(configPath))
            {
                outputFile.Write(JsonUtility.ToJson(s_bindingConfig, prettyPrint));
            }
        }

        public static void LoadBindingConfigFromFile(string configPath)
        {
            using (var inputFile = new StreamReader(configPath))
            {
                s_bindingConfig = JsonUtility.FromJson<BindingConfig>(inputFile.ReadToEnd());
            }
        }

        public static void BindAllCurrentDeviceClassMappings(VRModuleDeviceClass deviceClass)
        {
            for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
            {
                var roleMap = ViveRole.GetMap(ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i));
                var roleInfo = roleMap.RoleValueInfo;
                for (int rv = roleInfo.MinValidRoleValue, rvmax = roleInfo.MaxValidRoleValue; rv <= rvmax; ++rv)
                {
                    if (!roleInfo.IsValidRoleValue(rv)) { continue; }
                    if (roleMap.IsRoleValueBound(rv)) { continue; }

                    var mappedDevice = roleMap.GetMappedDeviceByRoleValue(rv);
                    var mappedDeviceState = VRModule.GetCurrentDeviceState(mappedDevice);
                    if (mappedDeviceState.deviceClass != deviceClass) { continue; }

                    roleMap.BindDeviceToRoleValue(mappedDeviceState.serialNumber, rv);
                }
            }
        }

        public static void BindAllCurrentMappings()
        {
            for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
            {
                var roleMap = ViveRole.GetMap(ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i));
                roleMap.BindAll();
            }
        }

        public static void UnbindAllCurrentBindings()
        {
            for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
            {
                var roleMap = ViveRole.GetMap(ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i));
                roleMap.UnbindAll();
            }
        }

        public static void SaveBindings(string configPath, bool prettyPrint = true)
        {
            LoadBindingConfigFromRoleMap();
            SaveBindingConfigToFile(configPath, prettyPrint);
        }

        public static void LoadBindings(string configPath)
        {
            LoadBindingConfigFromFile(configPath);
            ApplyBindingConfigToRoleMap();
        }

        [Obsolete("Use SaveBindings instead")]
        public static void SaveRoleBindings(string filePath, bool prettyPrint = false)
        {
            SaveBindings(filePath, prettyPrint);
        }

        [Obsolete("Use LoadBindings instead")]
        public static void LoadRoleBindings(string filePath)
        {
            LoadBindings(filePath);
        }
    }
}