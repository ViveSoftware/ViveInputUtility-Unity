//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;

namespace HTC.UnityPlugin.Vive
{
    public enum ViveRoleEnumValidateResult
    {
        Valid,
        IsNotEnumType,
        ViveRoleEnumAttributeNotFound,
        InvalidRoleNotFound,
        ValidRoleNotFound,
    }

    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
    public class ViveRoleEnumAttribute : Attribute
    {
        public int InvalidRoleValue { get; private set; }
        public ViveRoleEnumAttribute(int invalidEnumValue) { InvalidRoleValue = invalidEnumValue; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class HideMamberAttribute : Attribute { }

    public static class ViveRoleEnum
    {
        public interface IInfo
        {
            Type RoleEnumType { get; }
            string[] RoleValueNames { get; }
            int[] RoleValues { get; }
            int ElementCount { get; }
            int ValidRoleLength { get; }
            int InvalidRoleValue { get; }
            int InvalidRoleValueIndex { get; }
            int MinValidRoleValue { get; }
            int MaxValidRoleValue { get; }

            int ToRoleOffset(int roleValue);
            string GetNameByRoleValue(int roleValue);
            string GetNameByElementIndex(int elementIndex);
            int GetRoleValueByElementIndex(int elementIndex);
            int GetElementIndexByName(string name);
            bool TryGetRoleValueByName(string name, out int roleValue);
            bool IsValidRoleValue(int roleValue);
        }

        private sealed class Info : IInfo
        {
            private struct EnumValues
            {
                public string name;
                public int value;
            }

            private readonly Type m_roleEnumType;

            private readonly IndexedSet<string> m_roleNameSet;
            private readonly IndexedTable<int, string> m_roleNameTable;
            private readonly string[] m_roleValueNames;
            private readonly int[] m_roleValues;
            private readonly bool[] m_roleValid;

            private readonly int m_invalidRoleValue;
            private readonly int m_invalidRoleValueIndex;
            private readonly int m_minRoleValue;
            private readonly int m_maxRoleValue;
            private readonly int m_validRoleLength;

            public Info(Type roleEnumType)
            {
                m_roleEnumType = roleEnumType;

                var attrs = roleEnumType.GetCustomAttributes(typeof(ViveRoleEnumAttribute), false) as ViveRoleEnumAttribute[];
                m_invalidRoleValue = attrs[0].InvalidRoleValue;

                m_roleValueNames = Enum.GetNames(roleEnumType);
                m_roleValues = Enum.GetValues(roleEnumType) as int[];
                // remove name that shares same value with others
                var tempValues = new List<EnumValues>();
                for (int i = 0, imax = m_roleValues.Length; i < imax; ++i)
                {
                    // filter out obsolete member
                    var memInfo = m_roleEnumType.GetMember(m_roleValueNames[i]);
                    var attributes = memInfo[0].GetCustomAttributes(typeof(HideMamberAttribute), false);
                    if (attributes != null && attributes.Length > 0) { continue; }

                    tempValues.Add(new EnumValues()
                    {
                        value = m_roleValues[i],
                        name = m_roleValueNames[i],
                    });
                }
                // sort by value
                tempValues.Sort((e1, e2) =>
                {
                    if (e1.value < e2.value) { return -1; }
                    if (e1.value > e2.value) { return 1; }
                    return string.Compare(e1.name, e2.name);
                });

                m_roleNameSet = new IndexedSet<string>(m_roleValues.Length);
                m_roleNameTable = new IndexedTable<int, string>();
                m_roleValueNames = new string[tempValues.Count];
                m_roleValues = new int[tempValues.Count];
                for (int i = 0, imax = tempValues.Count; i < imax; ++i)
                {
                    m_roleValueNames[i] = tempValues[i].name;
                    m_roleValues[i] = tempValues[i].value;

                    m_roleNameSet.Add(tempValues[i].name);
                    m_roleNameTable.AddUniqueKey(tempValues[i].value, tempValues[i].name);
                }

                m_minRoleValue = int.MaxValue;
                m_maxRoleValue = int.MinValue;
                // find invalid role & valid role length
                for (int i = 0; i < m_roleValues.Length; ++i)
                {
                    if (m_roleValues[i] == m_invalidRoleValue)
                    {
                        m_invalidRoleValueIndex = i;
                        continue;
                    }

                    if (m_roleValues[i] < m_minRoleValue) { m_minRoleValue = m_roleValues[i]; }

                    if (m_roleValues[i] > m_maxRoleValue) { m_maxRoleValue = m_roleValues[i]; }
                }

                m_validRoleLength = m_maxRoleValue - m_minRoleValue + 1;

                // initialize role valid array, in case that the sequence of value of the enum type is not continuous
                m_roleValid = new bool[m_validRoleLength];
                for (int i = 0; i < m_roleValues.Length; ++i)
                {
                    if (m_roleValues[i] == m_invalidRoleValue) { continue; }

                    m_roleValid[ToRoleOffset(m_roleValues[i])] = true;
                }
            }

            public Type RoleEnumType { get { return m_roleEnumType; } }
            public string[] RoleValueNames { get { return m_roleValueNames; } }
            public int[] RoleValues { get { return m_roleValues; } }
            public int ElementCount { get { return m_roleValues.Length; } }
            public int ValidRoleLength { get { return m_validRoleLength; } }
            public int InvalidRoleValue { get { return m_invalidRoleValue; } }
            public int InvalidRoleValueIndex { get { return m_invalidRoleValueIndex; } }
            public int MinValidRoleValue { get { return m_minRoleValue; } }
            public int MaxValidRoleValue { get { return m_maxRoleValue; } }

            public int ToRoleOffset(int roleValue) { return roleValue - m_minRoleValue; }
            public string GetNameByRoleValue(int roleValue) { string name; return m_roleNameTable.TryGetValue(roleValue, out name) ? name : roleValue.ToString(); }
            public int GetElementIndexByName(string name) { return m_roleNameSet.IndexOf(name); }
            public string GetNameByElementIndex(int elementIndex) { return m_roleNameSet[elementIndex]; }
            public int GetRoleValueByElementIndex(int elementIndex) { return m_roleValues[elementIndex]; }

            public bool TryGetRoleValueByName(string name, out int roleValue)
            {
                int index = GetElementIndexByName(name);
                if (index >= 0)
                {
                    roleValue = GetRoleValueByElementIndex(index);
                    return true;
                }
                else
                {
                    roleValue = default(int);
                    return false;
                }
            }

            public bool IsValidRoleValue(int roleValue)
            {
                if (roleValue == m_invalidRoleValue) { return false; }

                var roleOffset = ToRoleOffset(roleValue);
                if (roleOffset < 0 || roleOffset >= m_roleValid.Length) { return false; }

                return m_roleValid[roleOffset];
            }
        }

        public interface IInfo<TRole> : IInfo
        {
            TRole InvalidRole { get; }
            TRole MinValidRole { get; }
            TRole MaxValidRole { get; }

            bool RoleEquals(TRole role1, TRole role2);
            int ToRoleValue(TRole role);
            int ToRoleOffsetFromRole(TRole role);
            TRole ToRole(int roleValue);
            TRole GetRoleByElementIndex(int elementIndex);
            bool TryGetRoleByName(string name, out TRole role);
            bool IsValidRole(TRole role);
        }

        private sealed class GenericInfo<TRole> : IInfo<TRole>
        {
            public static GenericInfo<TRole> s_instance;

            private readonly IInfo m_info;
            private readonly IndexedTable<string, TRole> m_nameTable;
            private readonly TRole[] m_roles;

            private readonly TRole m_invalidRole;
            private readonly TRole m_minValidRole;
            private readonly TRole m_maxValidRole;

            public GenericInfo()
            {
                m_info = GetInfo(typeof(TRole));
                var roleEnums = m_info.RoleValues as TRole[];

                m_nameTable = new IndexedTable<string, TRole>(roleEnums.Length);
                m_roles = new TRole[ValidRoleLength];

                for (int i = 0; i < m_roles.Length; ++i)
                {
                    m_roles[i] = InvalidRole;
                }

                for (int i = 0; i < roleEnums.Length; ++i)
                {
                    var roleValue = ToRoleValue(roleEnums[i]);

                    m_nameTable.Add(GetNameByElementIndex(i), roleEnums[i]);

                    if (roleValue == InvalidRoleValue)
                    {
                        m_invalidRole = roleEnums[i];
                    }
                    else
                    {
                        var offset = ToRoleOffset(roleValue);
                        m_roles[offset] = roleEnums[i];
                    }
                }

                m_minValidRole = ToRole(m_info.MinValidRoleValue);
                m_maxValidRole = ToRole(m_info.MaxValidRoleValue);

                if (s_instance == null)
                {
                    s_instance = this;
                }
                else
                {
                    UnityEngine.Debug.Log("duplicated instance for RoleInfo<" + typeof(TRole).Name + ">");
                }
            }

            public Type RoleEnumType { get { return m_info.RoleEnumType; } }
            public string[] RoleValueNames { get { return m_info.RoleValueNames; } }
            public int[] RoleValues { get { return m_info.RoleValues; } }
            public int ElementCount { get { return m_info.ElementCount; } }
            public int ValidRoleLength { get { return m_info.ValidRoleLength; } }
            public int InvalidRoleValue { get { return m_info.InvalidRoleValue; } }
            public int InvalidRoleValueIndex { get { return m_info.InvalidRoleValueIndex; } }
            public int MinValidRoleValue { get { return m_info.MinValidRoleValue; } }
            public int MaxValidRoleValue { get { return m_info.MaxValidRoleValue; } }
            public TRole MinValidRole { get { return m_minValidRole; } }
            public TRole MaxValidRole { get { return m_maxValidRole; } }

            public TRole InvalidRole { get { return m_invalidRole; } }

            public int ToRoleOffset(int roleValue) { return m_info.ToRoleOffset(roleValue); }
            public string GetNameByRoleValue(int roleValue) { return m_info.GetNameByRoleValue(roleValue); }
            public int GetElementIndexByName(string name) { return m_info.GetElementIndexByName(name); }
            public string GetNameByElementIndex(int elementIndex) { return m_info.GetNameByElementIndex(elementIndex); }
            public int GetRoleValueByElementIndex(int elementIndex) { return m_info.GetRoleValueByElementIndex(elementIndex); }
            public bool TryGetRoleValueByName(string name, out int roleValue) { return m_info.TryGetRoleValueByName(name, out roleValue); }
            public bool IsValidRoleValue(int roleValue) { return m_info.IsValidRoleValue(roleValue); }

            public bool RoleEquals(TRole role1, TRole role2) { return EqualityComparer<TRole>.Default.Equals(role1, role2); }
            public int ToRoleValue(TRole role) { return EqualityComparer<TRole>.Default.GetHashCode(role); }
            public int ToRoleOffsetFromRole(TRole role) { return ToRoleOffset(ToRoleValue(role)); }
            public TRole ToRole(int roleValue) { return IsValidRoleValue(roleValue) ? m_roles[ToRoleOffset(roleValue)] : InvalidRole; }
            public TRole GetRoleByElementIndex(int elementIndex) { return m_nameTable.GetValueByIndex(elementIndex); }
            public bool TryGetRoleByName(string name, out TRole role) { return m_nameTable.TryGetValue(name, out role); }
            public bool IsValidRole(TRole role) { return IsValidRoleValue(ToRoleValue(role)); }
        }

        private static IndexedTable<Type, IInfo> s_infoTable;
        private static readonly IndexedTable<string, Type> s_validViveRoleTable = new IndexedTable<string, Type>();

        static ViveRoleEnum()
        {
            // find all valid ViveRole enum type in current assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (ValidateViveRoleEnum(type) == ViveRoleEnumValidateResult.Valid)
                    {
                        s_validViveRoleTable.Add(type.FullName, type);
                    }
                }
            }
        }

        public static IIndexedTableReadOnly<string, Type> ValidViveRoleTable { get { return s_validViveRoleTable.ReadOnly; } }

        public static IInfo GetInfo(Type roleEnumType)
        {
            if (s_infoTable == null)
            {
                s_infoTable = new IndexedTable<Type, IInfo>();
            }

            IInfo info;
            if (!s_infoTable.TryGetValue(roleEnumType, out info))
            {
                var validateResult = ValidateViveRoleEnum(roleEnumType);
                if (validateResult != ViveRoleEnumValidateResult.Valid)
                {
                    UnityEngine.Debug.LogWarning(roleEnumType.Name + " is not valid ViveRole. " + validateResult);
                    return null;
                }

                info = new Info(roleEnumType);
                s_infoTable.Add(roleEnumType, info);
            }

            return info;
        }

        public static IInfo<TRole> GetInfo<TRole>()
        {
            var roleEnumType = typeof(TRole);
            if (GenericInfo<TRole>.s_instance == null)
            {
                var validateResult = ValidateViveRoleEnum(roleEnumType);
                if (validateResult != ViveRoleEnumValidateResult.Valid)
                {
                    UnityEngine.Debug.LogWarning(roleEnumType.Name + " is not valid ViveRole. " + validateResult);
                    return null;
                }

                new GenericInfo<TRole>();
            }

            return GenericInfo<TRole>.s_instance;
        }

        public static ViveRoleEnumValidateResult ValidateViveRoleEnum(Type roleEnumType)
        {
            if (!roleEnumType.IsEnum)
            {
                return ViveRoleEnumValidateResult.IsNotEnumType;
            }

            var attrs = roleEnumType.GetCustomAttributes(typeof(ViveRoleEnumAttribute), false) as ViveRoleEnumAttribute[];
            if (attrs.Length <= 0)
            {
                return ViveRoleEnumValidateResult.ViveRoleEnumAttributeNotFound;
            }

            var invalidRoleValue = attrs[0].InvalidRoleValue;
            var values = Enum.GetValues(roleEnumType) as int[];
            var invalidRoleFound = false;
            foreach (var value in values)
            {
                if (value == invalidRoleValue)
                {
                    invalidRoleFound = true;
                    break;
                }
            }

            if (!invalidRoleFound)
            {
                return ViveRoleEnumValidateResult.InvalidRoleNotFound;
            }

            if (values.Length < 2)
            {
                return ViveRoleEnumValidateResult.ValidRoleNotFound;
            }

            return ViveRoleEnumValidateResult.Valid;
        }
    }
}