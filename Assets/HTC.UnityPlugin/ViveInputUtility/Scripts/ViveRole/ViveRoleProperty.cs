//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
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
        public static readonly Type DefaultRoleType = typeof(DeviceRole);
        public static readonly int DefaultRoleValue = (int)DeviceRole.Hmd;

        [SerializeField]
        private string m_roleTypeFullName;
        [SerializeField]
        private string m_roleValueName;

        private bool m_isTypeDirty = true;
        private bool m_isValueDirty = true;
        private Type m_roleType;
        private int m_roleValue;

        public event Action Changed;

        public Type roleType
        {
            get
            {
                Update();
                return m_roleType;
            }
            set
            {
                Update();
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
                Update();
                m_isValueDirty |= ChangeProp.Set(ref m_roleValueName, ViveRoleEnum.GetInfo(m_roleType).GetNameByRoleValue(value));
                Update();
            }
        }

        public string roleTypeFullName { get { return m_roleTypeFullName; } }
        public string roleValueName { get { return m_roleValueName; } }

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
            return prop;
        }

        public void SetTypeDirty() { m_isTypeDirty = true; }
        public void SetValueDirty() { m_isValueDirty = true; }

        // update type and value if type string or value string is/are dirty
        private void Update()
        {
            if (!m_isTypeDirty && !m_isValueDirty) { return; }

            var changed = false;

            if (m_isTypeDirty)
            {
                m_isTypeDirty = false;

                Type newType;
                if (string.IsNullOrEmpty(m_roleTypeFullName) || !ViveRoleEnum.ValidViveRoleTable.TryGetValue(m_roleTypeFullName, out newType))
                {
                    newType = DefaultRoleType;
                }

                changed = ChangeProp.Set(ref m_roleType, newType);
            }

            if (m_isValueDirty || changed)
            {
                m_isValueDirty = false;

                int newValue;
                var info = ViveRoleEnum.GetInfo(m_roleType);
                if (string.IsNullOrEmpty(m_roleValueName) || !info.TryGetRoleValueByName(m_roleValueName, out newValue))
                {
                    newValue = info.InvalidRoleValue;
                }

                changed |= ChangeProp.Set(ref m_roleValue, newValue);
            }

            if (changed && Changed != null)
            {
                Changed.Invoke();
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

            return ViveRole.GetMap(m_roleType).GetMappedDeviceByRoleValue(m_roleValue);
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

            var roleInfo = ViveRoleEnum.GetInfo<TRole>();
            if (m_roleType != roleInfo.RoleEnumType) { return false; }

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