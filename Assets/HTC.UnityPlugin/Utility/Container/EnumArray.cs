//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Utility
{
    [Serializable]
    public abstract class EnumArrayBase
    {
        public const int DEFAULT_LENGTH_LIMIT = 1024;

        public abstract Type EnumType { get; }
        public abstract Type ElementType { get; }
        public abstract int MinInt { get; }
        public abstract int MaxInt { get; }
        public abstract int Length { get; }
        public abstract int Capacity { get; }
        public abstract string EnumIntName(int enumInt);
        public abstract bool IsDefined(int enumInt);
        public abstract void Clear();
        public abstract void FillCapacityToLength();
        public abstract void TrimCapacityToLength();
    }

    [Serializable]
    public abstract class EnumArrayBase<TEnum> : EnumArrayBase
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        public struct EnumEnumerator : IEnumerator<TEnum>, IEnumerable<TEnum>
        {
            private int i;
            private int iCurrent;
            private int iDst;

            object IEnumerator.Current { get { return Current; } }
            public TEnum Current { get { return enums[iCurrent]; } }

            public static EnumEnumerator Default { get { return new EnumEnumerator() { iDst = StaticLength }; } }

            public EnumEnumerator(TEnum from)
            {
                i = iCurrent = E2I(from) - StaticMinInt;
                iDst = StaticLength;
            }

            public EnumEnumerator(TEnum from, TEnum to)
            {
                i = iCurrent = E2I(from) - StaticMinInt;
                iDst = E2I(to) - StaticMinInt;
                if (iDst > i) { ++iDst; }
                else { --iDst; }
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; iCurrent = 0; }
            public EnumEnumerator GetEnumerator() { return this; }
            IEnumerator<TEnum> IEnumerable<TEnum>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                if (enums == null) { return false; }
                while (i != iDst)
                {
                    iCurrent = i;
                    if (i > iDst) { --i; } else { ++i; }
                    if (enumIsDefined[iCurrent])
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        protected static readonly TEnum[] enums;
        protected static readonly bool[] enumIsDefined;
        protected static Func<TEnum, int> funcE2I;
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
            if (underlyingType == typeof(ulong))
            { rangeCheckFunc = RangeCheckFromUInt64; }
            else
            { rangeCheckFunc = RangeCheckFromInt64; }

            if (underlyingType == typeof(int))
            { funcE2I = EqualityComparer<TEnum>.Default.GetHashCode; }
            else
            { funcE2I = (e) => (int)(object)e; }

            // find out min/max/length value in defined enum values
            var min = int.MaxValue;
            var max = int.MinValue;
            var enums = Enum.GetValues(StaticEnumType) as TEnum[];
            foreach (var e in enums)
            {
                int i;
                if (rangeCheckFunc(e, out i))
                {
                    if (i <= min)
                    {
                        StaticMinInt = min = i;
                        StaticMin = e;
                    }

                    if (i >= max)
                    {
                        StaticMaxInt = max = i;
                        StaticMax = e;
                    }
                }
            }

            if (StaticMinInt != min)
            {
                Debug.LogWarning("All defined values for " + StaticEnumType.Name + " are out of int range, set StaticLength to 1.");
                StaticMinInt = 0;
                StaticMin = default(TEnum);
                StaticMaxInt = 0;
                StaticMax = default(TEnum);
                StaticLength = 1;
            }
            else
            {
                if (StaticMinInt < (int.MaxValue - DEFAULT_LENGTH_LIMIT) && (StaticMinInt + DEFAULT_LENGTH_LIMIT) < StaticMaxInt)
                {
                    Debug.LogWarning("DefinedLength for " + StaticEnumType.Name + " out of range, will be clamped to less then " + DEFAULT_LENGTH_LIMIT + ".");
                    for (StaticMaxInt = StaticMinInt + DEFAULT_LENGTH_LIMIT; StaticMaxInt > StaticMinInt; --StaticMaxInt)
                    {
                        if (Enum.IsDefined(StaticEnumType, StaticMaxInt)) { break; }
                    }

                    StaticMax = (TEnum)(object)StaticMaxInt;
                }

                StaticLength = StaticMaxInt - StaticMinInt + 1;
            }

            // create an int array with invalid enum values
            EnumArrayBase<TEnum>.enums = new TEnum[StaticLength];
            enumIsDefined = new bool[StaticLength];
            foreach (var e in enums)
            {
                int i;
                if (rangeCheckFunc(e, out i) && i >= StaticMinInt && i <= StaticMaxInt)
                {
                    var index = i - StaticMinInt;
                    EnumArrayBase<TEnum>.enums[index] = e;
                    enumIsDefined[index] = true;
                }
            }

            // resolve undefined enums
            for (int i = 0, imax = StaticLength; i < imax; ++i)
            {
                if (!enumIsDefined[i])
                {
                    EnumArrayBase<TEnum>.enums[i] = (TEnum)(object)(i + StaticMinInt);
                }
            }
        }

        public override Type EnumType { get { return StaticEnumType; } }

        public override string EnumIntName(int enumInt) { return I2E(enumInt).ToString(); }

        public override bool IsDefined(int enumInt) { return StaticIsDefined(enumInt); }

        public static bool StaticIsDefined(int enumInt)
        {
            var i = enumInt - StaticMinInt;
            return i >= 0 && i < StaticLength && enumIsDefined[i];
        }

        public static bool StaticIsDefined(TEnum e)
        {
            return StaticIsDefined(E2I(e));
        }

        public static EnumEnumerator StaticEnums { get { return EnumEnumerator.Default; } }

        public static EnumEnumerator StaticEnumsFrom(TEnum from) { return new EnumEnumerator(from); }

        public static EnumEnumerator StaticEnumsFrom(TEnum from, TEnum to) { return new EnumEnumerator(from, to); }

        public static int E2I(TEnum e)
        {
            return funcE2I(e);
        }

        public static void SetEnumToInt32Resolver(Func<TEnum, int> func)
        {
            if (func != null) { funcE2I = func; }
        }

        public static TEnum I2E(int ei)
        {
            return enums[ei - StaticMinInt];
        }

        private delegate bool RangeCheckFunc(TEnum e, out int ei);

        private static bool RangeCheckFromUInt64(TEnum e, out int ei)
        {
            var l = (ulong)(object)e;
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

        private static bool RangeCheckFromInt64(TEnum e, out int ei)
        {
            var l = (long)(object)e;
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
            private int i;
            private int iCurrent;
            private int iDst;

            object IEnumerator.Current { get { return Current; } }
            public TValue Current { get { return elements[iCurrent]; } }

            public ValueEnumerator(EnumArray<TEnum, TValue> array)
            {
                elements = array.elements;
                i = iCurrent = 0;
                iDst = StaticLength;
            }

            public ValueEnumerator(EnumArray<TEnum, TValue> array, TEnum from)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - StaticMinInt;
                iDst = StaticLength;
            }

            public ValueEnumerator(EnumArray<TEnum, TValue> array, TEnum from, TEnum to)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - StaticMinInt;
                iDst = E2I(to) - StaticMinInt;
                if (iDst > i) { ++iDst; }
                else { --iDst; }
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; iCurrent = 0; }
            public ValueEnumerator GetEnumerator() { return this; }
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                if (elements == null) { return false; }
                while (i != iDst)
                {
                    iCurrent = i;
                    if (i > iDst) { --i; } else { ++i; }
                    if (enumIsDefined[iCurrent])
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public struct EnumValueEnumerator : IEnumerator<KeyValuePair<TEnum, TValue>>, IEnumerable<KeyValuePair<TEnum, TValue>>
        {
            private readonly TValue[] elements;
            private int i;
            private int iCurrent;
            private int iDst;

            object IEnumerator.Current { get { return Current; } }
            public KeyValuePair<TEnum, TValue> Current { get { return new KeyValuePair<TEnum, TValue>(enums[iCurrent], elements[iCurrent]); } }

            public EnumValueEnumerator(EnumArray<TEnum, TValue> array)
            {
                elements = array.elements;
                i = iCurrent = 0;
                iDst = StaticLength;
            }

            public EnumValueEnumerator(EnumArray<TEnum, TValue> array, TEnum from)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - StaticMinInt;
                iDst = StaticLength;
            }

            public EnumValueEnumerator(EnumArray<TEnum, TValue> array, TEnum from, TEnum to)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - StaticMinInt;
                iDst = E2I(to) - StaticMinInt;
                if (iDst > i) { ++iDst; }
                else { --iDst; }
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; iCurrent = 0; }
            public EnumValueEnumerator GetEnumerator() { return this; }
            IEnumerator<KeyValuePair<TEnum, TValue>> IEnumerable<KeyValuePair<TEnum, TValue>>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                if (elements == null) { return false; }
                while (i != iDst)
                {
                    iCurrent = i;
                    if (i > iDst) { --i; } else { ++i; }
                    if (enumIsDefined[iCurrent])
                    {
                        return true;
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

        public EnumArray() { elements = new TValue[Length]; }

        public EnumArray(Func<TEnum, int> resolver) : this() { SetEnumToInt32Resolver(resolver); }

        public EnumArray(TValue initValue) : this() { Clear(initValue); }

        public EnumArray(Func<TEnum, int> resolver, TValue initValue) : this(resolver) { Clear(initValue); }

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

        public ValueEnumerator Values { get { FillCapacityToLength(); return new ValueEnumerator(this); } }

        public ValueEnumerator ValuesFrom(TEnum from) { FillCapacityToLength(); return new ValueEnumerator(this, from); }

        public ValueEnumerator ValuesFrom(TEnum from, TEnum to) { FillCapacityToLength(); return new ValueEnumerator(this, from, to); }

        public EnumEnumerator Enums { get { return StaticEnums; } }

        public EnumEnumerator EnumsFrom(TEnum from) { return StaticEnumsFrom(from); }

        public EnumEnumerator EnumsFrom(TEnum from, TEnum to) { return StaticEnumsFrom(from, to); }

        public EnumValueEnumerator EnumValues { get { FillCapacityToLength(); return new EnumValueEnumerator(this); } }

        public EnumValueEnumerator EnumValuesFrom(TEnum from) { FillCapacityToLength(); return new EnumValueEnumerator(this, from); }

        public EnumValueEnumerator EnumValuesFrom(TEnum from, TEnum to) { FillCapacityToLength(); return new EnumValueEnumerator(this, from, to); }

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
                elements = new TValue[Length];
                return;
            }

            var len = Length;
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

            var len = Length;
            if (elements.Length > len)
            {
                Array.Resize(ref elements, len);
            }
        }
    }
}