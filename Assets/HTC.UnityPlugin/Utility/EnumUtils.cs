﻿//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    public static class EnumUtils
    {
        public const int UINT_MASK_FIELD_LENGTH = sizeof(int) * 8;
        public const int ULONG_MASK_FIELD_LENGTH = sizeof(long) * 8;

        // this class pares and stored the enum's names and values in different orders
        // 
        // Example:
        //
        // public enum SomeEnum
        // {
        //     Invalid = -1,
        //     AAA,
        //     BBB,
        //     zzz = -2,
        //     CCC = 35,
        //     Default = 0,
        //     EEE,
        //     FFF,
        //     GGG = 65,
        // }
        // 
        // EnumDisplayInfo for typeof(SomeEnum) will be:
        //
        // rawNames | rawValues
        // ---------------------
        // AAA      | 0
        // Default  | 0
        // EEE      | 1
        // BBB      | 1
        // FFF      | 2
        // CCC      | 35
        // GGG      | 65
        // zzz      | -2
        // Invalid  | -1
        // 
        // displayedNames | displayedRawNames | displayedValues
        // -----------------------------------------------------
        // Invalid        | Invalid           | -1
        // AAA            | AAA               | 0
        // BBB            | BBB               | 1
        // zzz            | zzz               | -2
        // CCC            | CCC               | 35
        // Default (AAA)  | Default           | 0
        // EEE (BBB)      | EEE               | 1
        // FFF            | FFF               | 2
        // GGG            | GGG               | 65
        // 
        // displayedMaskNames | displayedMaskRawNames | displayedMaskValues | realMaskField
        // ---------------------------------------------------------------------------------
        // AAA                | AAA                   | 0                   | 1ul << 0
        // BBB                | BBB                   | 1                   | 1ul << 1
        // CCC                | CCC                   | 35                  | 1ul << 35
        // Default (AAA)      | Default               | 0                   | 1ul << 0
        // EEE (BBB)          | EEE                   | 1                   | 1ul << 1
        // FFF                | FFF                   | 2                   | 1ul << 2

        public class EnumDisplayInfo
        {
            public Type enumType { get; private set; }

            public int minValue { get; private set; }
            public int maxValue { get; private set; }

            public string[] rawNames { get; private set; }
            public int[] rawValues { get; private set; }
            public Dictionary<int, int> rawValue2index { get; private set; }
            public Dictionary<string, int> rawName2index { get; private set; }

            public int displayedLength { get { return displayedRawNames.Length; } }
            public string[] displayedRawNames { get; private set; } // without parenthesis
            public string[] displayedNames { get; private set; }
            public int[] displayedValues { get; private set; }
            public Dictionary<int, int> value2displayedIndex { get; private set; }
            public Dictionary<string, int> name2displayedIndex { get; private set; }

            public int displayedMaskLength { get { return displayedMaskRawNames.Length; } }
            public string[] displayedMaskRawNames { get; private set; } // without parenthesis
            public string[] displayedMaskNames { get; private set; }
            public int[] displayedMaskValues { get; private set; }
            public ulong[] displayedMaskRealMaskField { get; private set; }
            public Dictionary<int, int> value2displayedMaskIndex { get; private set; }
            public Dictionary<string, int> name2displayedMaskIndex { get; private set; }

            public EnumDisplayInfo(Type type)
            {
                if (type == null) { throw new ArgumentNullException("type"); }
                if (!type.IsEnum) { throw new ArgumentException("Must be enum type", "type"); }

                enumType = type;
                rawNames = Enum.GetNames(type);
                rawValues = Enum.GetValues(type) as int[];
                rawValue2index = new Dictionary<int, int>();
                rawName2index = new Dictionary<string, int>();
                minValue = int.MaxValue;
                maxValue = int.MinValue;

                {
                    var index = 0;
                    foreach (var value in rawValues)
                    {
                        minValue = Mathf.Min(minValue, value);
                        maxValue = Mathf.Max(maxValue, value);

                        rawName2index[rawNames[index]] = index;

                        if (!rawValue2index.ContainsKey(value)) { rawValue2index[value] = index; }

                        ++index;
                    }
                }

                var displayedRawNamesList = new List<string>();
                var displayedNamesList = new List<string>();
                var displayedValuesList = new List<int>();
                value2displayedIndex = new Dictionary<int, int>();
                name2displayedIndex = new Dictionary<string, int>();

                var displayedMaskRawNamesList = new List<string>();
                var displayedMaskNamesList = new List<string>();
                var displayedMaskValuesList = new List<int>();
                var displayedMaskRealMaskFieldList = new List<ulong>();
                value2displayedMaskIndex = new Dictionary<int, int>();
                name2displayedMaskIndex = new Dictionary<string, int>();


                foreach (FieldInfo fi in type.GetFields()
                                             .Where(fi => fi.IsStatic && fi.GetCustomAttributes(typeof(HideInInspector), true).Length == 0)
                                             .OrderBy(fi => fi.MetadataToken))
                {
                    int index;
                    int priorIndex;
                    var name = fi.Name;
                    var value = (int)fi.GetValue(null);

                    displayedRawNamesList.Add(name);
                    displayedNamesList.Add(name);
                    displayedValuesList.Add(value);
                    index = displayedNamesList.Count - 1;

                    name2displayedIndex[name] = index;

                    if (!value2displayedIndex.TryGetValue(value, out priorIndex))
                    {
                        value2displayedIndex[value] = index;
                    }
                    else
                    {
                        displayedNamesList[index] += " (" + displayedNamesList[priorIndex] + ")";
                        name2displayedIndex[displayedNamesList[index]] = index;
                    }

                    if (value < 0 || value >= ULONG_MASK_FIELD_LENGTH) { continue; }

                    displayedMaskRawNamesList.Add(name);
                    displayedMaskNamesList.Add(name);
                    displayedMaskValuesList.Add(value);
                    displayedMaskRealMaskFieldList.Add(1ul << value);
                    index = displayedMaskNamesList.Count - 1;

                    name2displayedMaskIndex[name] = index;

                    if (!value2displayedMaskIndex.TryGetValue(value, out priorIndex))
                    {
                        value2displayedMaskIndex.Add(value, index);
                    }
                    else
                    {
                        displayedMaskNamesList[index] += " (" + displayedMaskNamesList[priorIndex] + ")";
                        name2displayedMaskIndex[displayedMaskNamesList[index]] = index;
                    }

                }

                displayedRawNames = displayedRawNamesList.ToArray();
                displayedNames = displayedNamesList.ToArray();
                displayedValues = displayedValuesList.ToArray();

                displayedMaskRawNames = displayedMaskRawNamesList.ToArray();
                displayedMaskNames = displayedMaskNamesList.ToArray();
                displayedMaskValues = displayedMaskValuesList.ToArray();
                displayedMaskRealMaskField = displayedMaskRealMaskFieldList.ToArray();
            }

            [Obsolete]
            public int RealToDisplayedMaskField(int realMask)
            {
                var displayedMask = 0u;

                for (int i = 0; i < UINT_MASK_FIELD_LENGTH && realMask != 0; ++i)
                {
                    if (GetFlag((uint)realMask, i))
                    {
                        UnsetFlag((uint)realMask, i);
                        if (value2displayedMaskIndex[i] < UINT_MASK_FIELD_LENGTH)
                        {
                            displayedMask |= 1u << value2displayedMaskIndex[i];
                        }
                    }
                }

                return (int)displayedMask;
            }

            [Obsolete]
            public int DisplayedToRealMaskField(int displayedMask, bool fillUp = true)
            {
                var realMask = 0u;

                for (int i = 0; i < UINT_MASK_FIELD_LENGTH && displayedMask != 0; ++i)
                {
                    if (GetFlag((uint)displayedMask, i))
                    {
                        UnsetFlag((uint)displayedMask, i);
                        if (i < UINT_MASK_FIELD_LENGTH)
                        {
                            realMask |= (uint)displayedMaskRealMaskField[i];
                        }
                    }
                }

                return (int)realMask;
            }
        }

        private static Dictionary<Type, EnumDisplayInfo> s_enumInfoTable = new Dictionary<Type, EnumDisplayInfo>();

        public static EnumDisplayInfo GetDisplayInfo(Type type)
        {
            EnumDisplayInfo info;
            if (!s_enumInfoTable.TryGetValue(type, out info))
            {
                info = new EnumDisplayInfo(type);
                s_enumInfoTable.Add(type, info);
            }

            return info;
        }

        public static int GetMinValue(Type enumType)
        {
            return GetDisplayInfo(enumType).minValue;
        }

        public static int GetMaxValue(Type enumType)
        {
            return GetDisplayInfo(enumType).maxValue;
        }

        public static bool GetFlag(uint maskField, int enumValue)
        {
            if (enumValue < 0 || enumValue >= UINT_MASK_FIELD_LENGTH) { return false; }
            return (maskField & (1u << enumValue)) != 0u;
        }

        public static void SetFlag(ref uint maskField, int enumValue, bool value)
        {
            if (enumValue < 0 || enumValue >= UINT_MASK_FIELD_LENGTH) { return; }
            if (value)
            {
                maskField |= (1u << enumValue);
            }
            else
            {
                maskField &= ~(1u << enumValue);
            }
        }

        public static void SetFlag(ref uint maskField, bool value, int enumValue, params int[] enumValues)
        {
            SetFlag(ref maskField, enumValue, value);
            if (enumValues != null && enumValues.Length > 0)
            {
                foreach (var ev in enumValues) { SetFlag(ref maskField, ev, value); }
            }
        }

        public static uint SetFlag(uint maskField, int enumValue, params int[] enumValues)
        {
            if (enumValue >= 0 && enumValue < UINT_MASK_FIELD_LENGTH) { maskField |= (1u << enumValue); }

            if (enumValues != null && enumValues.Length > 0)
            {
                foreach (var ev in enumValues)
                {
                    if (ev >= 0 && ev < UINT_MASK_FIELD_LENGTH) { maskField |= (1u << ev); }
                }
            }

            return maskField;
        }

        public static uint UnsetFlag(uint maskField, int enumValue, params int[] enumValues)
        {
            if (enumValue >= 0 && enumValue < UINT_MASK_FIELD_LENGTH) { maskField &= ~(1u << enumValue); }

            if (enumValues != null && enumValues.Length > 0)
            {
                foreach (var ev in enumValues)
                {
                    if (ev >= 0 && ev < UINT_MASK_FIELD_LENGTH) { maskField &= ~(1u << ev); }
                }
            }

            return maskField;
        }

        public static bool GetFlag(ulong maskField, int enumValue)
        {
            if (enumValue < 0 || enumValue >= ULONG_MASK_FIELD_LENGTH) { return false; }
            return (maskField & (1ul << enumValue)) != 0ul;
        }

        public static void SetFlag(ref ulong maskField, int enumValue, bool value)
        {
            if (enumValue < 0 || enumValue >= ULONG_MASK_FIELD_LENGTH) { return; }
            if (value)
            {
                maskField |= (1ul << enumValue);
            }
            else
            {
                maskField &= ~(1ul << enumValue);
            }
        }

        public static void SetFlag(ref ulong maskField, bool value, int enumValue, params int[] enumValues)
        {
            SetFlag(ref maskField, enumValue, value);
            if (enumValues != null && enumValues.Length > 0)
            {
                foreach (var ev in enumValues) { SetFlag(ref maskField, ev, value); }
            }
        }

        public static ulong SetFlag(ulong maskField, int enumValue, params int[] enumValues)
        {
            if (enumValue >= 0 && enumValue < ULONG_MASK_FIELD_LENGTH) { maskField |= (1ul << enumValue); }

            if (enumValues != null && enumValues.Length > 0)
            {
                foreach (var ev in enumValues)
                {
                    if (ev >= 0 && ev < ULONG_MASK_FIELD_LENGTH) { maskField |= (1ul << ev); }
                }
            }

            return maskField;
        }

        public static ulong UnsetFlag(ulong maskField, int enumValue, params int[] enumValues)
        {
            if (enumValue >= 0 && enumValue < ULONG_MASK_FIELD_LENGTH) { maskField &= ~(1ul << enumValue); }

            if (enumValues != null && enumValues.Length > 0)
            {
                foreach (var ev in enumValues)
                {
                    if (ev >= 0 && ev < ULONG_MASK_FIELD_LENGTH) { maskField &= ~(1ul << ev); }
                }
            }

            return maskField;
        }
    }
}