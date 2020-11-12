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
        public abstract bool IsEnumIntDefined(int enumInt);
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
        public struct EnumKeyEnumerator : IEnumerator<TEnum>, IEnumerable<TEnum>
        {
            private int i;
            private int iCurrent;
            private int iDst;

            object IEnumerator.Current { get { return Current; } }
            public TEnum Current { get { return enums[iCurrent]; } }

            public static EnumKeyEnumerator Default { get { return new EnumKeyEnumerator() { iDst = DefinedLength }; } }

            public EnumKeyEnumerator(TEnum from)
            {
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = DefinedLength;
            }

            public EnumKeyEnumerator(TEnum from, TEnum to)
            {
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = E2I(to) - DefinedMinInt;
                if (iDst > i) { ++iDst; }
                else { --iDst; }
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public EnumKeyEnumerator GetEnumerator() { return this; }
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
        public static readonly int DefinedMinInt;
        public static readonly TEnum DefinedMin;
        public static readonly int DefinedMaxInt;
        public static readonly TEnum DefinedMax;
        public static readonly int DefinedLength;
        public static readonly Type BaseEnumType = typeof(TEnum);

        static EnumArrayBase()
        {
#if !CSHARP_7_OR_LATER
            if (!BaseEnumType.IsEnum) { throw new Exception(BaseEnumType.Name + " is not enum type!"); }
#endif
            RangeCheckFunc rangeCheckFunc = null;
            if (Enum.GetUnderlyingType(BaseEnumType) == typeof(ulong))
            { rangeCheckFunc = RangeCheckFromUInt64; }
            else
            { rangeCheckFunc = RangeCheckFromInt64; }

            // find out min/max/length value in defined enum values
            var min = int.MaxValue;
            var max = int.MinValue;
            var enums = Enum.GetValues(BaseEnumType) as TEnum[];
            foreach (var e in enums)
            {
                int i;
                if (rangeCheckFunc(e, out i))
                {
                    if (i <= min)
                    {
                        DefinedMinInt = min = i;
                        DefinedMin = e;
                    }

                    if (i >= max)
                    {
                        DefinedMaxInt = max = i;
                        DefinedMax = e;
                    }
                }
            }

            if (DefinedMinInt != min)
            {
                Debug.LogWarning("All defined value for " + BaseEnumType.Name + " out of int range, DefinedLength will be set to 1.");
                DefinedMinInt = 0;
                DefinedMin = default(TEnum);
                DefinedMaxInt = 0;
                DefinedMax = default(TEnum);
                DefinedLength = 1;
            }
            else
            {
                if (DefinedMinInt < (int.MaxValue - DEFAULT_LENGTH_LIMIT) && (DefinedMinInt + DEFAULT_LENGTH_LIMIT) < DefinedMaxInt)
                {
                    Debug.LogWarning("DefinedLength for " + BaseEnumType.Name + " out of range, will be clamped to less then " + DEFAULT_LENGTH_LIMIT + ".");
                    for (DefinedMaxInt = DefinedMinInt + DEFAULT_LENGTH_LIMIT; DefinedMaxInt > DefinedMinInt; --DefinedMaxInt)
                    {
                        if (Enum.IsDefined(BaseEnumType, DefinedMaxInt)) { break; }
                    }

                    DefinedMax = FromInt32(DefinedMaxInt);
                }

                DefinedLength = DefinedMaxInt - DefinedMinInt + 1;
            }

            // create an int array with invalid enum values
            EnumArrayBase<TEnum>.enums = new TEnum[DefinedLength];
            enumIsDefined = new bool[DefinedLength];
            foreach (var e in enums)
            {
                int i;
                if (rangeCheckFunc(e, out i) && i >= DefinedMinInt && i <= DefinedMaxInt)
                {
                    var index = i - DefinedMinInt;
                    EnumArrayBase<TEnum>.enums[index] = e;
                    enumIsDefined[index] = true;
                }
            }

            // resolve undefined enums
            for (int i = 0, imax = DefinedLength; i < imax; ++i)
            {
                if (!enumIsDefined[i])
                {
                    EnumArrayBase<TEnum>.enums[i] = FromInt32(i + DefinedMinInt);
                }
            }
        }

        public override Type EnumType { get { return BaseEnumType; } }

        public override string EnumIntName(int enumInt) { return BaseEnumIntName(enumInt); }

        public override bool IsEnumIntDefined(int enumInt) { return BaseIsEnumIntDefined(enumInt); }

        public bool IsDefined(TEnum e) { return BaseIsDefined(e); }

        public static string BaseEnumIntName(int enumInt) { return I2E(enumInt).ToString(); }

        public static bool BaseIsEnumIntDefined(int enumInt)
        {
            var i = enumInt - DefinedMinInt;
            return i >= 0 && i < DefinedLength && enumIsDefined[i];
        }

        public static bool BaseIsDefined(TEnum e)
        {
            return BaseIsEnumIntDefined(E2I(e));
        }

        public static EnumKeyEnumerator BaseEnumKeys { get { return EnumKeyEnumerator.Default; } }

        public static EnumKeyEnumerator BaseEnumKeysFrom(TEnum from) { return new EnumKeyEnumerator(from); }

        public static EnumKeyEnumerator BaseEnumKeysFrom(TEnum from, TEnum to) { return new EnumKeyEnumerator(from, to); }

        protected static int E2I(TEnum e)
        {
            return ToInt32(e);
        }

        protected static TEnum I2E(int ev)
        {
            return enums[ev - DefinedMinInt];
        }

        private delegate bool RangeCheckFunc(TEnum e, out int v);

        private static bool RangeCheckFromUInt64(TEnum e, out int v)
        {
            var l = ToUInt64(e);
            if (l <= int.MaxValue)
            {
                v = (int)l;
                return true;
            }
            else
            {
                v = int.MaxValue;
                return false;
            }
        }

        private static bool RangeCheckFromInt64(TEnum e, out int v)
        {
            var l = ToInt64(e);
            if (l < int.MinValue)
            {
                v = int.MinValue;
                return false;
            }
            else if (l <= int.MaxValue)
            {
                v = (int)l;
                return true;
            }
            else
            {
                v = int.MaxValue;
                return false;
            }
        }

        //protected static readonly Func<byte, TEnum> FromByte = GenerateConvertToEnum<byte>();
        //protected static readonly Func<sbyte, TEnum> FromSByte = GenerateConvertToEnum<sbyte>();
        //protected static readonly Func<short, TEnum> FromInt16 = GenerateConvertToEnum<short>();
        //protected static readonly Func<ushort, TEnum> FromUInt16 = GenerateConvertToEnum<ushort>();
        protected static readonly Func<int, TEnum> FromInt32 = GenerateConvertToEnum<int>();
        //protected static readonly Func<uint, TEnum> FromUInt32 = GenerateConvertToEnum<uint>();
        //protected static readonly Func<long, TEnum> FromInt64 = GenerateConvertToEnum<long>();
        //protected static readonly Func<ulong, TEnum> FromUInt64 = GenerateConvertToEnum<ulong>();

        //protected static readonly Func<TEnum, byte> ToByte = GenerateConvertToValue<byte>();
        //protected static readonly Func<TEnum, sbyte> ToSByte = GenerateConvertToValue<sbyte>();
        //protected static readonly Func<TEnum, short> ToInt16 = GenerateConvertToValue<short>();
        //protected static readonly Func<TEnum, ushort> ToUInt16 = GenerateConvertToValue<ushort>();
        protected static readonly Func<TEnum, int> ToInt32 = GenerateConvertToValue<int>();
        //protected static readonly Func<TEnum, uint> ToUInt32 = GenerateConvertToValue<uint>();
        protected static readonly Func<TEnum, long> ToInt64 = GenerateConvertToValue<long>();
        protected static readonly Func<TEnum, ulong> ToUInt64 = GenerateConvertToValue<ulong>();

        private static Func<T, TEnum> GenerateConvertToEnum<T>()
        {
            var parameter = Expression.Parameter(typeof(T), "value");
            var dynamicMethod = Expression.Lambda<Func<T, TEnum>>(Expression.Convert(parameter, typeof(TEnum)), parameter);
            return dynamicMethod.Compile();
        }

        private static Func<TEnum, T> GenerateConvertToValue<T>()
        {
            var parameter = Expression.Parameter(typeof(TEnum), "value");
            var dynamicMethod = Expression.Lambda<Func<TEnum, T>>(Expression.Convert(parameter, typeof(T)), parameter);
            return dynamicMethod.Compile();
        }

    }

    [Serializable]
    public class EnumArray<TEnum, TElement> : EnumArrayBase<TEnum>, IEnumerable<KeyValuePair<TEnum, TElement>>
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        public interface IReadOnly : IEnumerable<KeyValuePair<TEnum, TElement>>
        {
            Type EnumType { get; }
            Type ElementType { get; }
            TEnum Min { get; }
            TEnum Max { get; }
            int MinInt { get; }
            int MaxInt { get; }
            TElement this[TEnum e] { get; }
            TElement this[int ev] { get; }
            string EnumName(int enumInt);
            EnumKeyEnumerator EnumKeys { get; }
            EnumKeyEnumerator EnumKeysFrom(TEnum from);
            EnumKeyEnumerator EnumKeysFrom(TEnum from, TEnum to);
            ElementEnumerator Elements { get; }
            ElementEnumerator ElementsFrom(TEnum from);
            ElementEnumerator ElementsFrom(TEnum from, TEnum to);
            new Enumerator GetEnumerator();
            Enumerator EnumerateFrom(TEnum from);
            Enumerator EnumerateFrom(TEnum from, TEnum to);

            /// <summary>
            /// Length defined by TEnum value
            /// </summary>
            int Length { get; }
            /// <summary>
            /// Real length for the underlying array (underlying array could be overridden by Unity's serialization)
            /// </summary>
            int Capacity { get; }
        }

        public struct ElementEnumerator : IEnumerator<TElement>, IEnumerable<TElement>
        {
            private readonly TElement[] elements;
            private int i;
            private int iCurrent;
            private int iDst;

            object IEnumerator.Current { get { return Current; } }
            public TElement Current { get { return elements[iCurrent]; } }

            public ElementEnumerator(EnumArray<TEnum, TElement> array)
            {
                elements = array.elements;
                i = iCurrent = 0;
                iDst = DefinedLength;
            }

            public ElementEnumerator(EnumArray<TEnum, TElement> array, TEnum from)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = DefinedLength;
            }

            public ElementEnumerator(EnumArray<TEnum, TElement> array, TEnum from, TEnum to)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = E2I(to) - DefinedMinInt;
                if (iDst > i) { ++iDst; }
                else { --iDst; }
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public ElementEnumerator GetEnumerator() { return this; }
            IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() { return this; }
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

        public struct Enumerator : IEnumerator<KeyValuePair<TEnum, TElement>>, IEnumerable<KeyValuePair<TEnum, TElement>>
        {
            private readonly TElement[] elements;
            private int i;
            private int iCurrent;
            private int iDst;

            object IEnumerator.Current { get { return Current; } }
            public KeyValuePair<TEnum, TElement> Current { get { return new KeyValuePair<TEnum, TElement>(enums[iCurrent], elements[iCurrent]); } }

            public Enumerator(EnumArray<TEnum, TElement> array)
            {
                elements = array.elements;
                i = iCurrent = 0;
                iDst = DefinedLength;
            }

            public Enumerator(EnumArray<TEnum, TElement> array, TEnum from)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = DefinedLength;
            }

            public Enumerator(EnumArray<TEnum, TElement> array, TEnum from, TEnum to)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = E2I(to) - DefinedMinInt;
                if (iDst > i) { ++iDst; }
                else { --iDst; }
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public Enumerator GetEnumerator() { return this; }
            IEnumerator<KeyValuePair<TEnum, TElement>> IEnumerable<KeyValuePair<TEnum, TElement>>.GetEnumerator() { return this; }
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

        [Serializable]
        private class ReadOnlyEnumArray : IReadOnly
        {
            public readonly EnumArray<TEnum, TElement> source;
            public ReadOnlyEnumArray(EnumArray<TEnum, TElement> source) { this.source = source; }
            public Type EnumType { get { return source.EnumType; } }
            public Type ElementType { get { return source.ElementType; } }
            public TEnum Min { get { return source.Min; } }
            public TEnum Max { get { return source.Max; } }
            public int MinInt { get { return source.MinInt; } }
            public int MaxInt { get { return source.MaxInt; } }
            public int Length { get { return source.Length; } }
            public int Capacity { get { return source.Capacity; } }
            public TElement this[TEnum e] { get { return source[e]; } }
            public TElement this[int ev] { get { return source[ev]; } }
            public string EnumName(int enumInt) { return source.EnumIntName(enumInt); }
            public EnumKeyEnumerator EnumKeys { get { return source.EnumKeys; } }
            public EnumKeyEnumerator EnumKeysFrom(TEnum from) { return source.EnumKeysFrom(from); }
            public EnumKeyEnumerator EnumKeysFrom(TEnum from, TEnum to) { return source.EnumKeysFrom(from, to); }
            public ElementEnumerator Elements { get { return source.Elements; } }
            public ElementEnumerator ElementsFrom(TEnum from) { return source.ElementsFrom(from); }
            public ElementEnumerator ElementsFrom(TEnum from, TEnum to) { return source.ElementsFrom(from, to); }
            public Enumerator GetEnumerator() { return source.GetEnumerator(); }
            public Enumerator EnumerateFrom(TEnum from) { return source.EnumerateFrom(from); }
            public Enumerator EnumerateFrom(TEnum from, TEnum to) { return source.EnumerateFrom(from, to); }
            IEnumerator<KeyValuePair<TEnum, TElement>> IEnumerable<KeyValuePair<TEnum, TElement>>.GetEnumerator() { return source.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return source.GetEnumerator(); }
        }

        [SerializeField]
        [FormerlySerializedAs("m_array")]
        private TElement[] elements;
        private ReadOnlyEnumArray readOnly;

        public EnumArray() { elements = new TElement[Length]; }

        public EnumArray(TElement initValue) : this() { Clear(initValue); }

        public override Type ElementType { get { return typeof(TElement); } }

        public override int MinInt { get { return DefinedMinInt; } }

        public override int MaxInt { get { return DefinedMaxInt; } }

        public TEnum Min { get { return DefinedMin; } }

        public TEnum Max { get { return DefinedMax; } }

        /// <summary>
        /// Length defined by TEnum value
        /// </summary>
        public override int Length { get { return DefinedLength; } }

        /// <summary>
        /// Real length for the underlying array (underlying array could be overridden by Unity's serialization)
        /// </summary>
        public override int Capacity { get { return elements == null ? 0 : elements.Length; } }

        public IReadOnly ReadOnly { get { return readOnly != null ? readOnly : (readOnly = new ReadOnlyEnumArray(this)); } }

        public EnumKeyEnumerator EnumKeys { get { return new EnumKeyEnumerator(); } }

        public EnumKeyEnumerator EnumKeysFrom(TEnum from) { return new EnumKeyEnumerator(from); }

        public EnumKeyEnumerator EnumKeysFrom(TEnum from, TEnum to) { return new EnumKeyEnumerator(from, to); }

        public ElementEnumerator Elements { get { FillCapacityToLength(); return new ElementEnumerator(this); } }

        public ElementEnumerator ElementsFrom(TEnum from) { FillCapacityToLength(); return new ElementEnumerator(this, from); }

        public ElementEnumerator ElementsFrom(TEnum from, TEnum to) { FillCapacityToLength(); return new ElementEnumerator(this, from, to); }

        public Enumerator GetEnumerator() { FillCapacityToLength(); return new Enumerator(this); }

        public Enumerator EnumerateFrom(TEnum from) { FillCapacityToLength(); return new Enumerator(this, from); }

        public Enumerator EnumerateFrom(TEnum from, TEnum to) { FillCapacityToLength(); return new Enumerator(this, from, to); }

        IEnumerator<KeyValuePair<TEnum, TElement>> IEnumerable<KeyValuePair<TEnum, TElement>>.GetEnumerator() { return GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public TElement this[TEnum e]
        {
            get { return this[E2I(e)]; }
            set { this[E2I(e)] = value; }
        }

        public TElement this[int ev]
        {
            get { FillCapacityToLength(); return elements[ev - DefinedMinInt]; }
            set { FillCapacityToLength(); elements[ev - DefinedMinInt] = value; }
        }

        public override void Clear()
        {
            if (elements != null && elements.Length > 0)
            {
                Array.Clear(elements, 0, elements.Length);
            }
        }

        public void Clear(TElement clearWith)
        {
            FillCapacityToLength();
            for (int i = elements.Length - 1; i >= 0; --i) { elements[i] = clearWith; }
        }

        public void CopyFrom(EnumArray<TEnum, TElement> source)
        {
            if (ReferenceEquals(this, source)) { return; }
            FillCapacityToLength();
            source.FillCapacityToLength();
            Array.Copy(source.elements, 0, elements, 0, DefinedLength);
        }

        public static void Copy(EnumArray<TEnum, TElement> srcArray, TEnum srcEnumIndex, EnumArray<TEnum, TElement> dstArray, TEnum dstEnumIndex, int length)
        {
            Copy(srcArray, E2I(srcEnumIndex), dstArray, E2I(dstEnumIndex), length);
        }

        public static void Copy(EnumArray<TEnum, TElement> srcArray, int srcEnumValueIndex, EnumArray<TEnum, TElement> dstArray, int dstEnumValueIndex, int length)
        {
            srcArray.FillCapacityToLength();
            dstArray.FillCapacityToLength();
            Array.Copy(srcArray.elements, srcEnumValueIndex - DefinedMinInt, dstArray.elements, dstEnumValueIndex - DefinedMinInt, length);
        }

        public override void FillCapacityToLength()
        {
            if (elements == null)
            {
                elements = new TElement[Length];
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