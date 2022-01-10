//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    // ViveRoleProperty is a serializable class that preserve vive role using 2 strings.
    // There also has a property drawer so you can use it as a serialized field in your MonoBevaviour.
    // Note that when deserializing, result of type and value is based on the valid role info stored in ViveRoleEnum class
    [Serializable]
    public class ViveRoleProperty
    {
        public delegate void RoleChangedListener();
        public delegate void RoleChangedListenerEx(Type previousRoleType, int previousRoleValue);
        public delegate void DeviceIndexChangedListener(uint deviceIndex);

        public static readonly Type DefaultRoleType = typeof(HandRole);
        public static readonly int DefaultRoleValue = (int)HandRole.RightHand;

        [SerializeField]
        private string m_roleTypeFullName;
        [SerializeField]
        private string m_roleValueName;
        [SerializeField]
        private int m_roleValueInt;

        private bool m_isTypeDirty = true;
        private bool m_isValueDirty = true;
        private bool m_lockUpdate;

        private ViveRole.IMap m_roleMap = null;
        private Type m_roleType = DefaultRoleType;
        private int m_roleValue = DefaultRoleValue;
        private uint m_deviceIndex = VRModule.INVALID_DEVICE_INDEX;

        private Action m_onChanged;
        private RoleChangedListener m_onRoleChanged;
        private RoleChangedListenerEx m_onRoleChangedEx;
        private DeviceIndexChangedListener m_onDeviceIndexChanged;

        public Type roleType
        {
            get
            {
                Update();
                return m_roleType;
            }
            set
            {
                m_isTypeDirty |= ChangeProp.Set(ref m_roleTypeFullName, value.FullName);
                Update();
            }
        }

        public int roleValue
        {
            get
            {
                Update();
                return m_roleValue;
            }
            set
            {
                if (ChangeProp.Set(ref m_roleValueName, m_roleMap.RoleValueInfo.GetNameByRoleValue(value)))
                {
                    m_roleValueInt = value;
                    m_isValueDirty = true;
                }

                Update();
            }
        }

        [Obsolete("Use onRoleChanged instead")]
        public event Action Changed
        {
            add { m_onChanged += value; }
            remove { m_onChanged -= value; }
        }

        public event RoleChangedListener onRoleChanged
        {
            add { m_onRoleChanged += value; }
            remove { m_onRoleChanged -= value; }
        }

        public event RoleChangedListenerEx onRoleChangedEx
        {
            add { m_onRoleChangedEx += value; }
            remove { m_onRoleChangedEx -= value; }
        }

        public event DeviceIndexChangedListener onDeviceIndexChanged
        {
            add
            {
                var wasEmpty = m_onDeviceIndexChanged == null;

                m_onDeviceIndexChanged += value;

                if (wasEmpty && m_onDeviceIndexChanged != null && m_roleMap != null)
                {
                    m_deviceIndex = m_roleMap.GetMappedDeviceByRoleValue(m_roleValue); // update deviceIndex before first time listening to MappingChanged event
                    m_roleMap.onRoleValueMappingChanged += OnMappingChanged;
                }
            }
            remove
            {
                var wasEmpty = m_onDeviceIndexChanged == null;

                m_onDeviceIndexChanged -= value;

                if (!wasEmpty && m_onDeviceIndexChanged == null && m_roleMap != null)
                {
                    m_roleMap.onRoleValueMappingChanged -= OnMappingChanged;
                }
            }
        }

        public static ViveRoleProperty New()
        {
            return New(DefaultRoleType, DefaultRoleValue);
        }

        public static ViveRoleProperty New(Type type, int value)
        {
            return New(type.FullName, ViveRoleEnum.GetInfo(type).GetNameByRoleValue(value));
        }

        public static ViveRoleProperty New<TRole>(TRole role)
        {
            return New(typeof(TRole).FullName, role.ToString());
        }

        public static ViveRoleProperty New(string typeFullName, string valueName)
        {
            var prop = new ViveRoleProperty();
            prop.m_roleTypeFullName = typeFullName;
            prop.m_roleValueName = valueName;

            Type roleType;
            if (ViveRoleEnum.ValidViveRoleTable.TryGetValue(typeFullName, out roleType))
            {
                var roleInfo = ViveRoleEnum.GetInfo(roleType);
                var roleIndex = roleInfo.GetElementIndexByName(valueName);
                if (roleIndex >= 0)
                {
                    prop.m_roleValueInt = roleInfo.GetRoleValueByElementIndex(roleIndex);
                }
                else
                {
                    prop.m_roleValueInt = roleInfo.InvalidRoleValue;
                }
            }

            return prop;
        }

        public void SetTypeDirty() { m_isTypeDirty = true; }

        public void SetValueDirty() { m_isValueDirty = true; }

        private void OnMappingChanged(ViveRole.IMap map, ViveRole.MappingChangedEventArg args)
        {
            if (args.roleValue == m_roleValue)
            {
                m_onDeviceIndexChanged(m_deviceIndex = args.currentDeviceIndex);
            }
        }

        // update type and value changes
        public void Update()
        {
            if (m_lockUpdate && (m_isTypeDirty || m_isValueDirty)) { throw new Exception("Can't change value during onChange event callback"); }

            var oldRoleType = m_roleType;
            var oldRoleValue = m_roleValue;
            var roleTypeChanged = false;
            var roleValueChanged = false;
            var deviceIndexChanged = false;

            if (m_isTypeDirty || m_roleType == null)
            {
                m_isTypeDirty = false;
                
                if (string.IsNullOrEmpty(m_roleTypeFullName) || !ViveRoleEnum.ValidViveRoleTable.TryGetValue(m_roleTypeFullName, out m_roleType))
                {
                    m_roleType = DefaultRoleType;
                }

                roleTypeChanged = oldRoleType != m_roleType;
            }

            // maintain m_roleMap cache
            // m_roleMap could be null on first update
            if (roleTypeChanged || m_roleMap == null)
            {
                if (m_onDeviceIndexChanged != null)
                {
                    if (m_roleMap != null)
                    {
                        m_roleMap.onRoleValueMappingChanged -= OnMappingChanged;
                        m_roleMap = ViveRole.GetMap(m_roleType);
                        m_roleMap.onRoleValueMappingChanged += OnMappingChanged;
                    }
                    else
                    {
                        m_roleMap = ViveRole.GetMap(m_roleType);
                        m_deviceIndex = m_roleMap.GetMappedDeviceByRoleValue(m_roleValue); // update deviceIndex before first time listening to MappingChanged event
                        m_roleMap.onRoleValueMappingChanged += OnMappingChanged;
                    }
                }
                else
                {
                    m_roleMap = ViveRole.GetMap(m_roleType);
                }
            }

            if (m_isValueDirty || roleTypeChanged)
            {
                m_isValueDirty = false;
                
                var info = m_roleMap.RoleValueInfo;
                if (string.IsNullOrEmpty(m_roleValueName) || !info.TryGetRoleValueByName(m_roleValueName, out m_roleValue))
                {
                    m_roleValue = info.IsValidRoleValue(m_roleValueInt) ? m_roleValueInt : info.InvalidRoleValue;
                }

                roleValueChanged = oldRoleValue != m_roleValue;
            }

            if (roleTypeChanged || roleValueChanged)
            {
                if (m_onDeviceIndexChanged != null)
                {
                    var oldDeviceIndex = m_deviceIndex;
                    m_deviceIndex = m_roleMap.GetMappedDeviceByRoleValue(m_roleValue);

                    if (VRModule.IsValidDeviceIndex(oldDeviceIndex) || VRModule.IsValidDeviceIndex(m_deviceIndex))
                    {
                        deviceIndexChanged = oldDeviceIndex != m_deviceIndex;
                    }
                }

                m_lockUpdate = true;

                if (m_onChanged != null) { m_onChanged(); }

                if (m_onRoleChanged != null) { m_onRoleChanged(); }

                if (m_onRoleChangedEx != null) { m_onRoleChangedEx(oldRoleType, oldRoleValue); }

                if (deviceIndexChanged) { m_onDeviceIndexChanged(m_deviceIndex); }

                m_lockUpdate = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TRole">Can be DeviceRole, HandRole or TrackerRole</typeparam>
        /// <param name="role"></param>
        public void SetEx<TRole>(TRole role)
        {
            Set(typeof(TRole).FullName, role.ToString());
        }

        public void Set(Type type, int value)
        {
            Set(type.FullName, ViveRoleEnum.GetInfo(type).GetNameByRoleValue(value));
        }

        public void Set(ViveRoleProperty prop)
        {
            Set(prop.m_roleTypeFullName, prop.m_roleValueName);
        }

        // set by value name to preserve the enum element, since different enum element could have same enum value
        public void Set(string typeFullName, string valueName)
        {
            m_isTypeDirty |= ChangeProp.Set(ref m_roleTypeFullName, typeFullName);
            m_isValueDirty |= ChangeProp.Set(ref m_roleValueName, valueName);

            Update();
        }

        public uint GetDeviceIndex()
        {
            Update();

            if (m_onDeviceIndexChanged == null)
            {
                return m_roleMap.GetMappedDeviceByRoleValue(m_roleValue);
            }
            else
            {
                return m_deviceIndex;
            }
        }

        public TRole ToRole<TRole>()
        {
            Update();

            TRole role;
            var roleInfo = ViveRoleEnum.GetInfo<TRole>();
            if (m_roleType != typeof(TRole) || !roleInfo.TryGetRoleByName(m_roleValueName, out role))
            {
                // return invalid if role type not match or the value name not found in roleInfo
                return roleInfo.InvalidRole;
            }

            return role;
        }

        public bool IsRole(Type type, int value)
        {
            Update();

            return m_roleType == type && m_roleValue == value;
        }

        public bool IsRole<TRole>(TRole role)
        {
            Update();

            if (m_roleType != typeof(TRole)) { return false; }
            var roleInfo = ViveRoleEnum.GetInfo<TRole>();

            return m_roleValue == roleInfo.ToRoleValue(role);
        }

        public static bool operator ==(ViveRoleProperty p1, ViveRoleProperty p2)
        {
            if (ReferenceEquals(p1, p2)) { return true; }
            if (ReferenceEquals(p1, null)) { return false; }
            if (ReferenceEquals(p2, null)) { return false; }
            if (p1.roleType != p2.roleType) { return false; }
            if (p1.roleValue != p2.roleValue) { return false; }
            return true;
        }

        public static bool operator !=(ViveRoleProperty p1, ViveRoleProperty p2)
        {
            return !(p1 == p2);
        }

        public bool Equals(ViveRoleProperty prop)
        {
            return this == prop;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ViveRoleProperty);
        }

        public override int GetHashCode()
        {
            Update();

            var hash = 17;
            hash = hash * 23 + (m_roleType == null ? 0 : m_roleType.GetHashCode());
            hash = hash * 23 + m_roleValue.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            Update();

            return m_roleType.Name + "." + ViveRoleEnum.GetInfo(m_roleType).GetNameByRoleValue(m_roleValue);
        }
    }
}