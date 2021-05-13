//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Utility
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class InvalidEnumArrayIndexAttribute : Attribute { }

    public abstract class EnumToIntResolver { }

    /// <summary>
    /// A base generic class that provide function that converts Enum value into int value.
    /// 
    /// </summary>
    /// <remarks>
    /// Resolver should provide faster function to convert from TEnum into int value
    /// If not defined, EnumArrayBase<TEnum> static constructor can only provide slower convert function like "(int)(object)enumValue" or "EqualityComparer<TEnum>.Default.GetHashCode(enumValue)"
    /// 
    /// <example>
    /// <code>
    /// class MyEnumResolver : EnumToIntResolver<MyEnum>
    /// {
    ///     public override int Resolve(MyEnum e) { return (int)e; }
    /// }
    /// </code>
    /// </example>
    ///
    /// Define custom resolver class for each enum type in your project, EnumToIntResolverCache will find it and instantiate/cache their instance
    /// </remarks>
    /// <seealso cref="EnumArray{TEnum, TValue}"/>
    /// <seealso cref="EnumToIntResolverCache"/>
    public abstract class EnumToIntResolver<TEnum> : EnumToIntResolver
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        public abstract int Resolve(TEnum e);
    }

    public static class EnumToIntResolverCache
    {
        private static Dictionary<Type, Type> typeCache;
        private static Dictionary<Type, EnumToIntResolver> instanceCache;

        public static void ClearCache()
        {
            typeCache = null;
            instanceCache = null;
        }

        public static bool TryGetCachedResolverType<TEnum>(out Type resolverType)
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
        {
            if (typeCache == null)
            {
                typeCache = new Dictionary<Type, Type>();
                FindAllResolverTypesInAllDomain(typeCache);
            }

            return typeCache.TryGetValue(typeof(TEnum), out resolverType);
        }

        public static bool TryGetCachedResolverInstance<TEnum>(out EnumToIntResolver<TEnum> resolver)
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
        {
            var enumType = typeof(TEnum);
            var resolverBase = default(EnumToIntResolver);
            if (instanceCache != null && instanceCache.TryGetValue(enumType, out resolverBase))
            {
                resolver = resolverBase as EnumToIntResolver<TEnum>;
                return true;
            }
            else
            {
                Type resolverType;
                if (TryGetCachedResolverType<TEnum>(out resolverType))
                {
                    resolver = (EnumToIntResolver<TEnum>)Activator.CreateInstance(resolverType);
                    if (instanceCache == null) { instanceCache = new Dictionary<Type, EnumToIntResolver>(); }
                    instanceCache[enumType] = resolver;
                    return true;
                }

                resolver = null;
                return false;
            }
        }

        public static int FindAllResolverTypesInAllDomain(IDictionary<Type, Type> outDict)
        {
            var count = 0;
            var resolverTypeDef = typeof(EnumToIntResolver<>);
            var resolverBaseType = typeof(EnumToIntResolver);
            var currentAsm = resolverTypeDef.Assembly;
            var currentAsmName = currentAsm.GetName().Name;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var referencingCurrentAsm = false;
                if (asm == currentAsm)
                {
                    referencingCurrentAsm = true;
                }
                else
                {
                    foreach (var asmref in asm.GetReferencedAssemblies())
                    {
                        if (asmref.Name == currentAsmName)
                        {
                            referencingCurrentAsm = true;
                            break;
                        }
                    }
                }

                if (referencingCurrentAsm)
                {
                    // try find valid role enum type in assembly
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.IsAbstract) { continue; }
                        if (!type.IsSubclassOf(resolverBaseType)) { continue; }
                        if (!type.BaseType.IsGenericType) { continue; }
                        if (type.BaseType.GetGenericTypeDefinition() != resolverTypeDef) { continue; }
                        foreach (var typeParam in type.BaseType.GetGenericArguments())
                        {
#if !CSHARP_7_OR_LATER
                            if (!typeParam.IsEnum) { break; }
#endif
                            if (!outDict.ContainsKey(typeParam))
                            {
                                outDict[typeParam] = type;
                                ++count;
                                //Debug.Log("Found reslover type (" + type.FullName + ") for " + typeParam.Name);
                            }
                            break;
                        }
                    }
                }
            }

            return count;
        }
    }

    [Serializable]
    public abstract class EnumArrayBase
    {
        public const int DEFAULT_LENGTH_LIMIT = 1024;

        protected delegate bool RangeCheckFunc(object e, out int ei);

        public abstract Type EnumType { get; }
        public abstract Type ElementType { get; }
        public abstract int MinInt { get; }
        public abstract int MaxInt { get; }
        public abstract int Length { get; }
        public abstract int Capacity { get; }
        public abstract string EnumName(int enumInt);
        public abstract string EnumNameWithAlias(int enumInt);
        public abstract bool IsValidIndex(int enumInt);
        public abstract void Clear();
        public abstract void FillCapacityToLength();
        public abstract void TrimCapacityToLength();

        protected static bool WithoutInvalidIndexAttr(FieldInfo fi)
        {
            return fi.IsStatic && fi.GetCustomAttributes(typeof(InvalidEnumArrayIndexAttribute), true).Length == 0;
        }

        protected static int CompareMetadataToken(FieldInfo fi)
        {
            return fi.MetadataToken;
        }

        protected static bool RangeCheckFromUInt8(object e, out int ei) { ei = (byte)e; return true; }
        protected static bool RangeCheckFromInt8(object e, out int ei) { ei = (sbyte)e; return true; }
        protected static bool RangeCheckFromInt16(object e, out int ei) { ei = (short)e; return true; }
        protected static bool RangeCheckFromUInt16(object e, out int ei) { ei = (ushort)e; return true; }
        protected static bool RangeCheckFromInt32(object e, out int ei) { ei = (int)e; return true; }

        protected static bool RangeCheckFromUInt32(object e, out int ei)
        {
            var l = (uint)e;
            if (l <= int.MaxValue)
            {
                ei = (int)l;
                return true;
            }
            else
            {
                ei = int.MaxValue;
                return false;
            }
        }

        protected static bool RangeCheckFromInt64(object e, out int ei)
        {
            var l = (long)e;
            if (l < int.MinValue)
            {
                ei = int.MinValue;
                return false;
            }
            else if (l <= int.MaxValue)
            {
                ei = (int)l;
                return true;
            }
            else
            {
                ei = int.MaxValue;
                return false;
            }
        }

        protected static bool RangeCheckFromUInt64(object e, out int ei)
        {
            var l = (ulong)e;
            if (l <= int.MaxValue)
            {
                ei = (int)l;
                return true;
            }
            else
            {
                ei = int.MaxValue;
                return false;
            }
        }
    }

    [Serializable]
    public abstract class EnumArrayBase<TEnum> : EnumArrayBase
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        public struct EnumEnumerator : IEnumerator<TEnum>, IEnumerable<TEnum>
        {
            private readonly int iStart;
            private readonly int iEnd;
            private int iCurrent;

            object IEnumerator.Current { get { return Current; } }
            public TEnum Current { get { return InternalI2E(iCurrent); } }

            public static EnumEnumerator All
            {
                get
                {
                    return new EnumEnumerator(0, StaticLength - 1);
                }
            }

            public static EnumEnumerator From(TEnum from)
            {
                var ifrom = E2I(from) - StaticMinInt; return new EnumEnumerator(ifrom, Mathf.Max(ifrom, StaticLength - 1));
            }

            public static EnumEnumerator FromTo(TEnum from, TEnum to)
            {
                return new EnumEnumerator(E2I(from) - StaticMinInt, E2I(to) - StaticMinInt);
            }

            private EnumEnumerator(int from, int to)
            {
                if (StaticLength == 0)
                {
                    iStart = iEnd = iCurrent = -1;
                }
                else
                {
                    from = Mathf.Clamp(from, 0, StaticLength - 1);
                    to = Mathf.Clamp(to, 0, StaticLength - 1);

                    if (from <= to)
                    {
                        iStart = iCurrent = from - 1;
                        iEnd = to;
                    }
                    else
                    {
                        iStart = iCurrent = from + 1;
                        iEnd = to;
                    }
                }
            }

            void IDisposable.Dispose() { }
            public void Reset() { iCurrent = iStart; }
            public EnumEnumerator GetEnumerator() { return this; }
            IEnumerator<TEnum> IEnumerable<TEnum>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                while (iCurrent != iEnd)
                {
                    iCurrent = iCurrent > iEnd ? (iCurrent - 1) : (iCurrent + 1);
                    if (InternalStaticIsValidIndex(iCurrent))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static readonly TEnum[] enums;
        private static readonly string[] enumNames;
        private static readonly string[] enumNameWithAliases;
        private static Func<TEnum, int> funcE2I;
        public static readonly int StaticMinInt;
        public static readonly TEnum StaticMin;
        public static readonly int StaticMaxInt;
        public static readonly TEnum StaticMax;
        public static readonly int StaticLength;
        public static readonly Type StaticEnumType = typeof(TEnum);

        static EnumArrayBase()
        {
#if !CSHARP_7_OR_LATER
            if (!StaticEnumType.IsEnum) { throw new Exception(StaticEnumType.Name + " is not enum type!"); }
#endif
            var underlyingType = Enum.GetUnderlyingType(StaticEnumType);
            var rangeCheckFunc = default(RangeCheckFunc);
            if (underlyingType == typeof(int))
            {
                rangeCheckFunc = RangeCheckFromInt32;
            }
            else if (underlyingType == typeof(uint))
            {
                rangeCheckFunc = RangeCheckFromUInt32;
            }
            else if (underlyingType == typeof(long))
            {
                rangeCheckFunc = RangeCheckFromInt64;
            }
            else if (underlyingType == typeof(ulong))
            {
                rangeCheckFunc = RangeCheckFromUInt64;
            }
            else if (underlyingType == typeof(byte))
            {
                rangeCheckFunc = RangeCheckFromUInt8;
            }
            else if (underlyingType == typeof(sbyte))
            {
                rangeCheckFunc = RangeCheckFromInt8;
            }
            else if (underlyingType == typeof(short))
            {
                rangeCheckFunc = RangeCheckFromInt16;
            }
            else if (underlyingType == typeof(ushort))
            {
                rangeCheckFunc = RangeCheckFromUInt16;
            }

            // find out min/max/length value in defined enum values
            var fields = StaticEnumType.GetFields().Where(WithoutInvalidIndexAttr).OrderBy((Func<FieldInfo, int>)CompareMetadataToken).ToArray();
            var fieldEnums = new TEnum[fields.Length];
            var fieldValues = new int[fields.Length];
            var fieldNames = new string[fields.Length];

            var validIndexFound = false;
            for (int i = 0, imax = fields.Length; i < imax; ++i)
            {
                var field = fields[i];
                var vo = field.GetValue(null);
                var ve = (TEnum)vo;
                int vi;
                if (rangeCheckFunc(vo, out vi))
                {
                    if (!validIndexFound)
                    {
                        validIndexFound = true;
                        StaticMinInt = StaticMaxInt = vi;
                        StaticMin = StaticMax = ve;
                    }
                    else
                    {
                        if (vi < StaticMinInt)
                        {
                            if (StaticMaxInt >= int.MinValue + DEFAULT_LENGTH_LIMIT && vi <= StaticMaxInt - DEFAULT_LENGTH_LIMIT)
                            {
                                Debug.Log("[EnumArray] too small. vi=" + vi + " min=" + StaticMinInt + " max=" + StaticMaxInt);
                                continue; // length out of range
                            }
                            StaticMinInt = vi;
                            StaticMin = ve;
                        }
                        else if (vi > StaticMaxInt)
                        {
                            if (StaticMinInt <= int.MaxValue - DEFAULT_LENGTH_LIMIT && vi >= StaticMinInt + DEFAULT_LENGTH_LIMIT)
                            {
                                Debug.Log("[EnumArray] too large. vi=" + vi + " min=" + StaticMinInt + " max=" + StaticMaxInt);
                                continue; // length out of range
                            }
                            StaticMaxInt = vi;
                            StaticMax = ve;
                        }
                    }

                    fieldValues[i] = vi;
                    fieldEnums[i] = ve;
                    fieldNames[i] = field.Name;
                }
            }

            if (!validIndexFound)
            {
                Debug.LogWarning("Valid index for EnumArray not found. type:" + StaticEnumType.Name);
                StaticMinInt = 0;
                StaticMin = default(TEnum);
                StaticMaxInt = 0;
                StaticMax = default(TEnum);
                StaticLength = 0;
            }

            StaticLength = StaticMaxInt - StaticMinInt + 1;

            // create an int array with invalid enum values
            enums = new TEnum[StaticLength];
            enumNames = new string[StaticLength];
            enumNameWithAliases = new string[StaticLength];
            for (int fi = 0, imax = fields.Length; fi < imax; ++fi)
            {
                if (fieldNames[fi] == null) { continue; }

                var i = fieldValues[fi] - StaticMinInt;
                if (enumNames[i] == null)
                {
                    enums[i] = fieldEnums[fi];
                    enumNames[i] = fieldNames[fi];
                }
                else if (enumNameWithAliases[i] == null)
                {
                    enumNameWithAliases[i] = fieldNames[fi];
                }
                else
                {
                    enumNameWithAliases[i] += ", " + fieldNames[fi];
                }
            }

            for (int i = 0, imax = StaticLength; i < imax; ++i)
            {
                if (enumNames[i] != null)
                {
                    if (enumNameWithAliases[i] != null)
                    {
                        enumNameWithAliases[i] = enumNames[i] + " (" + enumNameWithAliases[i] + ")";
                    }
                    else
                    {
                        enumNameWithAliases[i] = enumNames[i];
                    }
                }
            }
        }

        public override Type EnumType { get { return StaticEnumType; } }

        public override string EnumName(int enumInt)
        {
            return StaticEnumName(enumInt);
        }

        public override string EnumNameWithAlias(int enumInt)
        {
            return StaticEnumNameWithAlias(enumInt);
        }

        public static string StaticEnumName(TEnum e)
        {
            return StaticEnumName(E2I(e));
        }

        public static string StaticEnumName(int enumInt)
        {
            return enumNames[enumInt - StaticMinInt];
        }

        public static string StaticEnumNameWithAlias(int enumInt)
        {
            return enumNameWithAliases[enumInt - StaticMinInt];
        }

        public override bool IsValidIndex(int enumInt)
        {
            return StaticIsValidIndex(enumInt);
        }

        public static bool StaticIsValidIndex(TEnum e)
        {
            return StaticIsValidIndex(E2I(e));
        }

        public static bool StaticIsValidIndex(int enumInt)
        {
            var i = enumInt - StaticMinInt;
            return i >= 0 && i < StaticLength && InternalStaticIsValidIndex(i);
        }

        protected static bool InternalStaticIsValidIndex(int index)
        {
            return enumNames[index] != null;
        }

        public static EnumEnumerator StaticEnums { get { return EnumEnumerator.All; } }

        public static EnumEnumerator StaticEnumsFrom(TEnum from) { return EnumEnumerator.From(from); }

        public static EnumEnumerator StaticEnumsFrom(TEnum from, TEnum to) { return EnumEnumerator.FromTo(from, to); }

        public static void InitializeFuncE2I()
        {
            if (funcE2I != null) { return; }

            // Find first found custom resolver
            // resolver should provide faster function to convert TEnum to int value
            // most common & fasteast converting is to directly cast in the script like "return (int)enumValue;"
            // In this generic constructor, we can only use slower converting function like "(int)(object)enumValue" or "EqualityComparer<TEnum>.Default.GetHashCode(enumValue)"
            EnumToIntResolver<TEnum> resolver;
            if (EnumToIntResolverCache.TryGetCachedResolverInstance(out resolver))
            {
                funcE2I = resolver.Resolve;
                return;
            }

            WarnBoxingResolver();

            var underlyingType = Enum.GetUnderlyingType(StaticEnumType);
            if (underlyingType == typeof(int))
            {
                funcE2I = EqualityComparer<TEnum>.Default.GetHashCode;
            }
            else if (underlyingType == typeof(uint))
            {
                funcE2I = e => (int)(uint)(object)e;
            }
            else if (underlyingType == typeof(long))
            {
                funcE2I = e => (int)(long)(object)e;
            }
            else if (underlyingType == typeof(ulong))
            {
                funcE2I = e => (int)(ulong)(object)e;
            }
            else if (underlyingType == typeof(byte))
            {
                funcE2I = e => (byte)(object)e;
            }
            else if (underlyingType == typeof(sbyte))
            {
                funcE2I = e => (sbyte)(object)e;
            }
            else if (underlyingType == typeof(short))
            {
                funcE2I = e => (short)(object)e;
            }
            else if (underlyingType == typeof(ushort))
            {
                funcE2I = e => (ushort)(object)e;
            }
        }

        public static int E2I(TEnum e)
        {
            InitializeFuncE2I();
            return funcE2I(e);
        }

        public static TEnum I2E(int ei)
        {
            return InternalI2E(ei - StaticMinInt);
        }

        protected static TEnum InternalI2E(int index)
        {
            return enums[index];
        }

        private static void WarnBoxingResolver()
        {
            Debug.LogWarning("Boxing Resolver for enum " + StaticEnumType.Name + " is used. Define subclass of class EnumToIntResolver<" + StaticEnumType.Name + "> to provide faster resolver instead.");
        }
    }

    [Serializable]
    public class EnumArray<TEnum, TValue> : EnumArrayBase<TEnum>, IEnumerable<TValue>
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        public interface IReadOnly : IEnumerable<TValue>
        {
            Type EnumType { get; }
            Type ElementType { get; }
            TEnum Min { get; }
            TEnum Max { get; }
            int MinInt { get; }
            int MaxInt { get; }
            TValue this[TEnum e] { get; }
            TValue this[int ev] { get; }
            new ValueEnumerator GetEnumerator();
            ValueEnumerator Values { get; }
            ValueEnumerator ValuesFrom(TEnum from);
            ValueEnumerator ValuesFrom(TEnum from, TEnum to);
            EnumEnumerator Enums { get; }
            EnumEnumerator EnumsFrom(TEnum from);
            EnumEnumerator EnumsFrom(TEnum from, TEnum to);
            EnumValueEnumerator EnumValues { get; }
            EnumValueEnumerator EnumValuesFrom(TEnum from);
            EnumValueEnumerator EnumValuesFrom(TEnum from, TEnum to);

            /// <summary>
            /// Length between min and max TEnum value
            /// </summary>
            int Length { get; }
            /// <summary>
            /// Real length for the underlying array
            /// (underlying array could be overridden with unexpected length by Unity's serialization)
            /// </summary>
            int Capacity { get; }
        }

        [Serializable]
        private class ReadOnlyEnumArray : IReadOnly
        {
            public readonly EnumArray<TEnum, TValue> source;
            public ReadOnlyEnumArray(EnumArray<TEnum, TValue> source) { this.source = source; }
            public Type EnumType { get { return source.EnumType; } }
            public Type ElementType { get { return source.ElementType; } }
            public TEnum Min { get { return source.Min; } }
            public TEnum Max { get { return source.Max; } }
            public int MinInt { get { return source.MinInt; } }
            public int MaxInt { get { return source.MaxInt; } }
            public int Length { get { return source.Length; } }
            public int Capacity { get { return source.Capacity; } }
            public TValue this[TEnum e] { get { return source[e]; } }
            public TValue this[int ev] { get { return source[ev]; } }
            public ValueEnumerator GetEnumerator() { return source.GetEnumerator(); }
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() { return source.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return source.GetEnumerator(); }
            public ValueEnumerator Values { get { return source.Values; } }
            public ValueEnumerator ValuesFrom(TEnum from) { return source.ValuesFrom(from); }
            public ValueEnumerator ValuesFrom(TEnum from, TEnum to) { return source.ValuesFrom(from, to); }
            public EnumEnumerator Enums { get { return source.Enums; } }
            public EnumEnumerator EnumsFrom(TEnum from) { return source.EnumsFrom(from); }
            public EnumEnumerator EnumsFrom(TEnum from, TEnum to) { return source.EnumsFrom(from, to); }
            public EnumValueEnumerator EnumValues { get { return source.EnumValues; } }
            public EnumValueEnumerator EnumValuesFrom(TEnum from) { return source.EnumValuesFrom(from); }
            public EnumValueEnumerator EnumValuesFrom(TEnum from, TEnum to) { return source.EnumValuesFrom(from, to); }
        }

        public struct ValueEnumerator : IEnumerator<TValue>, IEnumerable<TValue>
        {
            private readonly TValue[] elements;
            private readonly int iStart;
            private readonly int iEnd;
            private int iCurrent;

            object IEnumerator.Current { get { return Current; } }
            public TValue Current { get { return elements[iCurrent]; } }

            public static ValueEnumerator All(EnumArray<TEnum, TValue> array)
            {
                return new ValueEnumerator(array, 0, StaticLength - 1);
            }

            public static ValueEnumerator From(EnumArray<TEnum, TValue> array, TEnum from)
            {
                var ifrom = E2I(from) - StaticMinInt;
                return new ValueEnumerator(array, ifrom, Mathf.Max(ifrom, StaticLength - 1));
            }

            public static ValueEnumerator FromTo(EnumArray<TEnum, TValue> array, TEnum from, TEnum to)
            {
                return new ValueEnumerator(array, E2I(from) - StaticMinInt, E2I(to) - StaticMinInt);
            }

            private ValueEnumerator(EnumArray<TEnum, TValue> array, int from, int to)
            {
                if (StaticLength == 0)
                {
                    elements = null;
                    iStart = iEnd = iCurrent = -1;
                }
                else
                {
                    from = Mathf.Clamp(from, 0, StaticLength - 1);
                    to = Mathf.Clamp(to, 0, StaticLength - 1);

                    if (from <= to)
                    {
                        iStart = iCurrent = from - 1;
                        iEnd = to;
                    }
                    else
                    {
                        iStart = iCurrent = from + 1;
                        iEnd = to;
                    }

                    array.FillCapacityToLength();
                    elements = array.elements;
                }
            }

            void IDisposable.Dispose() { }
            public void Reset() { iCurrent = iStart; }
            public ValueEnumerator GetEnumerator() { return this; }
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                if (elements != null)
                {
                    while (iCurrent != iEnd)
                    {
                        iCurrent = iCurrent > iEnd ? (iCurrent - 1) : (iCurrent + 1);
                        if (InternalStaticIsValidIndex(iCurrent))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public struct EnumValueEnumerator : IEnumerator<KeyValuePair<TEnum, TValue>>, IEnumerable<KeyValuePair<TEnum, TValue>>
        {
            private readonly TValue[] elements;
            private readonly int iStart;
            private readonly int iEnd;
            private int iCurrent;

            object IEnumerator.Current { get { return Current; } }
            public KeyValuePair<TEnum, TValue> Current { get { return new KeyValuePair<TEnum, TValue>(InternalI2E(iCurrent), elements[iCurrent]); } }

            public static EnumValueEnumerator All(EnumArray<TEnum, TValue> array)
            {
                return new EnumValueEnumerator(array, 0, StaticLength - 1);
            }

            public static EnumValueEnumerator From(EnumArray<TEnum, TValue> array, TEnum from)
            {
                var ifrom = E2I(from) - StaticMinInt;
                return new EnumValueEnumerator(array, ifrom, Mathf.Max(ifrom, StaticLength - 1));
            }

            public static EnumValueEnumerator FromTo(EnumArray<TEnum, TValue> array, TEnum from, TEnum to)
            {
                return new EnumValueEnumerator(array, E2I(from) - StaticMinInt, E2I(to) - StaticMinInt);
            }

            private EnumValueEnumerator(EnumArray<TEnum, TValue> array, int from, int to)
            {
                if (StaticLength == 0)
                {
                    elements = null;
                    iStart = iEnd = iCurrent = -1;
                }
                else
                {
                    from = Mathf.Clamp(from, 0, StaticLength - 1);
                    to = Mathf.Clamp(to, 0, StaticLength - 1);

                    if (from <= to)
                    {
                        iStart = iCurrent = from - 1;
                        iEnd = to;
                    }
                    else
                    {
                        iStart = iCurrent = from + 1;
                        iEnd = to;
                    }

                    array.FillCapacityToLength();
                    elements = array.elements;
                }
            }

            void IDisposable.Dispose() { }
            public void Reset() { iCurrent = iStart; }
            public EnumValueEnumerator GetEnumerator() { return this; }
            IEnumerator<KeyValuePair<TEnum, TValue>> IEnumerable<KeyValuePair<TEnum, TValue>>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                if (elements != null)
                {
                    while (iCurrent != iEnd)
                    {
                        iCurrent = iCurrent > iEnd ? (iCurrent - 1) : (iCurrent + 1);
                        if (InternalStaticIsValidIndex(iCurrent))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_array")]
        private TValue[] elements;
        private ReadOnlyEnumArray readOnly;

        public readonly static Type StaticElementType = typeof(TValue);

        public EnumArray() { elements = new TValue[StaticLength]; }

        public EnumArray(TValue initValue) : this() { Clear(initValue); }

        public override Type ElementType { get { return StaticElementType; } }

        public override int MinInt { get { return StaticMinInt; } }

        public override int MaxInt { get { return StaticMaxInt; } }

        public TEnum Min { get { return StaticMin; } }

        public TEnum Max { get { return StaticMax; } }

        /// <summary>
        /// Length between min and max TEnum value
        /// </summary>
        public override int Length { get { return StaticLength; } }

        /// <summary>
        /// Real length for the underlying array
        /// (underlying array could be overridden with unexpected length by Unity's serialization)
        /// </summary>
        public override int Capacity { get { return elements == null ? 0 : elements.Length; } }

        public IReadOnly ReadOnly { get { return readOnly != null ? readOnly : (readOnly = new ReadOnlyEnumArray(this)); } }

        public ValueEnumerator GetEnumerator() { return Values; }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() { return GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public ValueEnumerator Values { get { return ValueEnumerator.All(this); } }

        public ValueEnumerator ValuesFrom(TEnum from) { return ValueEnumerator.From(this, from); }

        public ValueEnumerator ValuesFrom(TEnum from, TEnum to) { return ValueEnumerator.FromTo(this, from, to); }

        public EnumEnumerator Enums { get { return StaticEnums; } }

        public EnumEnumerator EnumsFrom(TEnum from) { return StaticEnumsFrom(from); }

        public EnumEnumerator EnumsFrom(TEnum from, TEnum to) { return StaticEnumsFrom(from, to); }

        public EnumValueEnumerator EnumValues { get { return EnumValueEnumerator.All(this); } }

        public EnumValueEnumerator EnumValuesFrom(TEnum from) { return EnumValueEnumerator.From(this, from); }

        public EnumValueEnumerator EnumValuesFrom(TEnum from, TEnum to) { return EnumValueEnumerator.FromTo(this, from, to); }

        public TValue this[TEnum e]
        {
            get { return this[E2I(e)]; }
            set { this[E2I(e)] = value; }
        }

        public TValue this[int ei]
        {
            get { FillCapacityToLength(); return elements[ei - StaticMinInt]; }
            set { FillCapacityToLength(); elements[ei - StaticMinInt] = value; }
        }

        public override void Clear()
        {
            if (elements != null && elements.Length > 0)
            {
                Array.Clear(elements, 0, elements.Length);
            }
        }

        public void Clear(TValue clearWith)
        {
            FillCapacityToLength();
            for (int i = elements.Length - 1; i >= 0; --i) { elements[i] = clearWith; }
        }

        public void CopyFrom(EnumArray<TEnum, TValue> source)
        {
            if (ReferenceEquals(this, source)) { return; }
            FillCapacityToLength();
            source.FillCapacityToLength();
            Array.Copy(source.elements, 0, elements, 0, StaticLength);
        }

        public static void Copy(EnumArray<TEnum, TValue> srcArray, TEnum srcEnumIndex, EnumArray<TEnum, TValue> dstArray, TEnum dstEnumIndex, int length)
        {
            Copy(srcArray, E2I(srcEnumIndex), dstArray, E2I(dstEnumIndex), length);
        }

        public static void Copy(EnumArray<TEnum, TValue> srcArray, int srcEnumValueIndex, EnumArray<TEnum, TValue> dstArray, int dstEnumValueIndex, int length)
        {
            srcArray.FillCapacityToLength();
            dstArray.FillCapacityToLength();
            Array.Copy(srcArray.elements, srcEnumValueIndex - StaticMinInt, dstArray.elements, dstEnumValueIndex - StaticMinInt, length);
        }

        public override void FillCapacityToLength()
        {
            if (elements == null)
            {
                elements = new TValue[StaticLength];
                return;
            }

            var len = StaticLength;
            if (elements.Length < len)
            {
                Array.Resize(ref elements, len);
            }
        }

        public override void TrimCapacityToLength()
        {
            if (elements == null)
            {
                return;
            }

            var len = StaticLength;
            if (elements.Length > len)
            {
                Array.Resize(ref elements, len);
            }
        }
    }
}