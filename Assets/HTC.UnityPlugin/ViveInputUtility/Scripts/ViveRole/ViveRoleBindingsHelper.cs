//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using HTC.UnityPlugin.Utility;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace HTC.UnityPlugin.Vive
{
    public class ViveRoleBindingsHelper : SingletonBehaviour<ViveRoleBindingsHelper>
    {
        [Serializable]
        public struct Binding
        {
            [FormerlySerializedAs("sn")]
            public string device_sn;
            public VRModuleDeviceModel device_model;
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
        public class BindingConfig
        {
            [NonSerialized]
            [Obsolete("This field is always true now. Use VIUSettings.autoLoadBindingConfigOnStart to control if the project will auto load config.")]
            public bool apply_bindings_on_load = true;
            [NonSerialized]
            [Obsolete("Use VIUSettings.bindingInterfaceSwitchKey instead.")]
            public string toggle_interface_key_code = string.Empty;
            [NonSerialized]
            [Obsolete("Use VIUSettings.bindingInterfaceSwitchKeyModifier instead.")]
            public string toggle_interface_modifier = string.Empty;
            [NonSerialized]
            [Obsolete("Use VIUSettings.bindingInterfaceObject instead.")]
            public string interface_prefab = DEFAULT_INTERFACE_PREFAB;

            public RoleData[] roles = new RoleData[0];
        }

        [Obsolete("Use VIUSettings.BINDING_INTERFACE_CONFIG_FILE_PATH_DEFAULT_VALUE instead.")]
        public const string AUTO_LOAD_CONFIG_PATH = "vive_role_bindings.cfg";
        [Obsolete("Use VIUSettings.BINDING_INTERFACE_PREFAB_DEFAULT_RESOURCE_PATH instead.")]
        public const string DEFAULT_INTERFACE_PREFAB = "VIUBindingInterface";
        
        private static BindingConfig s_bindingConfig = new BindingConfig();

        private static GameObject s_interfaceObj;
        private static Dictionary<string, VRModuleDeviceModel> s_modelHintTable = new Dictionary<string, VRModuleDeviceModel>();

        public static BindingConfig bindingConfig { get { return s_bindingConfig; } }

        public static bool isBindingInterfaceEnabled { get { return s_interfaceObj != null && s_interfaceObj.activeSelf; } }

        static ViveRoleBindingsHelper()
        {
            SetDefaultInitGameObjectGetter(VRModule.GetInstanceGameObject);
        }

        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (VRModule.Active && VRModule.activeModule != VRModuleActiveEnum.Uninitialized)
            {
                TryInitializeOnLoad();
            }
            else
            {
                VRModule.onActiveModuleChanged += OnActiveModuleChanged;
            }
        }

        private static void OnActiveModuleChanged(VRModuleActiveEnum activatedModule)
        {
            if (activatedModule != VRModuleActiveEnum.Uninitialized)
            {
                VRModule.onActiveModuleChanged -= OnActiveModuleChanged;

                TryInitializeOnLoad();
            }
        }

        private static void TryInitializeOnLoad()
        {
            if (VIUSettings.autoLoadBindingConfigOnStart)
            {
                if (LoadBindingConfigFromFile(VIUSettings.bindingConfigFilePath))
                {
                    var appliedCount = ApplyBindingConfigToRoleMap();

                    Debug.Log("ViveRoleBindingsHelper: " + appliedCount + " bindings applied from " + VIUSettings.bindingConfigFilePath);
                }
            }

            if (!Active && VIUSettings.enableBindingInterfaceSwitch)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!IsInstance) { return; }

            if (VIUSettings.enableBindingInterfaceSwitch)
            {
                if (Input.GetKeyDown(VIUSettings.bindingInterfaceSwitchKey) && (VIUSettings.bindingInterfaceSwitchKeyModifier == KeyCode.None || Input.GetKey(VIUSettings.bindingInterfaceSwitchKeyModifier)))
                {
                    ToggleBindingInterface();
                }
            }
        }

        public static VRModuleDeviceModel GetDeviceModelHint(string deviceSN)
        {
            var deviceIndex = VRModule.GetConnectedDeviceIndex(deviceSN);
            if (VRModule.IsValidDeviceIndex(deviceIndex))
            {
                return VRModule.GetCurrentDeviceState(deviceIndex).deviceModel;
            }

            VRModuleDeviceModel deviceModel;
            if (s_modelHintTable.TryGetValue(deviceSN, out deviceModel))
            {
                return deviceModel;
            }

            return VRModuleDeviceModel.Unknown;
        }

        public static void EnableBindingInterface()
        {
            if (s_interfaceObj == null)
            {
                if (VIUSettings.bindingInterfaceObjectSource == null)
                {
                    Debug.LogWarning("VIUSettings.bindingInterfaceObjectSource is null.");
                    return;
                }

                s_interfaceObj = Instantiate(VIUSettings.bindingInterfaceObjectSource);
            }
            else
            {
                s_interfaceObj.SetActive(true);
            }
        }

        public static void DisableBindingInterface()
        {
            if (s_interfaceObj != null)
            {
                s_interfaceObj.SetActive(false);
            }
        }

        public static void ToggleBindingInterface()
        {
            if (!isBindingInterfaceEnabled)
            {
                EnableBindingInterface();
            }
            else
            {
                DisableBindingInterface();
            }
        }

        public static void LoadBindingConfigFromRoleMap(params Type[] roleTypeFilter)
        {
            var roleDataList = ListPool<RoleData>.Get();
            var filterUsed = roleTypeFilter != null && roleTypeFilter.Length > 0;

            if (filterUsed)
            {
                roleDataList.AddRange(s_bindingConfig.roles);
            }

            for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
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

                        // save the device_model for better recognition of the device
                        if (VRModule.IsDeviceConnected(binding.device_sn))
                        {
                            binding.device_model = VRModule.GetCurrentDeviceState(VRModule.GetConnectedDeviceIndex(binding.device_sn)).deviceModel;
                            s_modelHintTable[binding.device_sn] = binding.device_model;
                        }
                        else if (!s_modelHintTable.TryGetValue(binding.device_sn, out binding.device_model))
                        {
                            binding.device_model = VRModuleDeviceModel.Unknown;
                        }

                        roleData.bindings[j] = binding;
                    }

                    if (filterUsed)
                    {
                        // merge with existing role data
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

        public static int ApplyBindingConfigToRoleMap(params Type[] roleTypeFilter)
        {
            var appliedCount = 0;
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
                    // bind device according to role_name first
                    // if role_name is invalid then role_value is used
                    int roleValue;
                    if (string.IsNullOrEmpty(binding.role_name) || !roleMap.RoleValueInfo.TryGetRoleValueByName(binding.role_name, out roleValue))
                    {
                        roleValue = binding.role_value;
                    }

                    roleMap.BindDeviceToRoleValue(binding.device_sn, roleValue);
                    ++appliedCount;
                }
            }

            return appliedCount;
        }

        public static void SaveBindingConfigToFile(string configPath, bool prettyPrint = true)
        {
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(configPath))
            {
                Directory.CreateDirectory(dir);
            }

            using (var outputFile = new StreamWriter(configPath))
            {
                outputFile.Write(JsonUtility.ToJson(s_bindingConfig, prettyPrint));
            }
        }

        public static bool LoadBindingConfigFromFile(string configPath)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            {
                return false;
            }

            using (var inputFile = new StreamReader(configPath))
            {
                s_bindingConfig = JsonUtility.FromJson<BindingConfig>(inputFile.ReadToEnd());

                foreach (var roleData in s_bindingConfig.roles)
                {
                    foreach (var binding in roleData.bindings)
                    {
                        if (VRModule.IsDeviceConnected(binding.device_sn))
                        {
                            s_modelHintTable[binding.device_sn] = VRModule.GetCurrentDeviceState(VRModule.GetConnectedDeviceIndex(binding.device_sn)).deviceModel;
                        }
                        else
                        {
                            s_modelHintTable[binding.device_sn] = binding.device_model;
                        }
                    }
                }

                return true;
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