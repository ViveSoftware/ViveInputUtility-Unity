//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.InteropServices;

namespace HTC.UnityPlugin.Utility
{
    [Serializable]
    public abstract class EnumArrayBase
    {
        public const int LENGTH_LIMIT = 1024;

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

        protected static void AssertSupportedRangeInt64(object e)
        {
            if (Convert.ToInt64(e) < 0L)
            {
                throw new Exception("Out of EnumArray supported long value, must larger then 0");
            }
        }

        protected static void AssertSupportedRangeUInt64(object e)
        {
            if (Convert.ToUInt64(e) > uint.MaxValue)
            {
                throw new Exception("Out of EnumArray supported ulong value, must less then " + uint.MaxValue);
            }
        }
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

            public EnumKeyEnumerator(TEnum from)
            {
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = DefinedLength - 1;
            }

            public EnumKeyEnumerator(TEnum from, TEnum to)
            {
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = E2I(to) - DefinedMinInt;
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public EnumKeyEnumerator GetEnumerator() { return this; }
            IEnumerator<TEnum> IEnumerable<TEnum>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
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

        private static readonly bool needRearrangeHashCode;
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
            var underlyingeType = Enum.GetUnderlyingType(BaseEnumType);
            if (underlyingeType == typeof(sbyte) || underlyingeType == typeof(short))
            {
                throw new Exception("Underlyinge type of " + BaseEnumType.Name + "(" + underlyingeType.Name + ") is not supported.");
            }

            Action<object> assertSupportedRange = null;
            if (underlyingeType == typeof(uint))
            {
                needRearrangeHashCode = true;
            }
            else if (underlyingeType == typeof(long))
            {
                needRearrangeHashCode = true;
                assertSupportedRange = AssertSupportedRangeInt64;
            }
            else if (underlyingeType == typeof(ulong))
            {
                needRearrangeHashCode = true;
                assertSupportedRange = AssertSupportedRangeUInt64;
            }

            // find out min/max/length value in defined enum values
            DefinedMinInt = int.MaxValue;
            DefinedMaxInt = int.MinValue;
            var enums = Enum.GetValues(BaseEnumType) as TEnum[];
            foreach (var e in enums)
            {
                if (assertSupportedRange != null) { assertSupportedRange(e); }
                var i = E2I(e);

                if (i <= DefinedMinInt)
                {
                    DefinedMinInt = i;
                    DefinedMin = e;
                }

                if (i >= DefinedMaxInt)
                {
                    DefinedMaxInt = i;
                    DefinedMax = e;
                }
            }

            if (DefinedMinInt < (int.MaxValue - LENGTH_LIMIT) && (DefinedMinInt + LENGTH_LIMIT) < DefinedMaxInt)
            {
                var min = (long)DefinedMinInt;
                var max = (long)DefinedMaxInt;
                var len = max - min;
                DefinedMinInt = 0;
                DefinedMaxInt = 0;
                throw new Exception("Defined length must less then " + LENGTH_LIMIT + ". min:" + DefinedMin + "(" + min + ") max:" + DefinedMax + "(" + max + ") len:" + len);
            }

            DefinedLength = DefinedMaxInt - DefinedMinInt + 1;

            // create an int array with invalid enum values
            EnumArrayBase<TEnum>.enums = new TEnum[DefinedLength];
            enumIsDefined = new bool[DefinedLength];
            foreach (var e in enums)
            {
                var i = E2I(e) - DefinedMinInt;
                EnumArrayBase<TEnum>.enums[i] = e;
                enumIsDefined[i] = true;
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

        public static EnumKeyEnumerator BaseEnumKeys { get { return new EnumKeyEnumerator(); } }

        public static EnumKeyEnumerator BaseEnumKeysFrom(TEnum from) { return new EnumKeyEnumerator(from); }

        public static EnumKeyEnumerator BaseEnumKeysFrom(TEnum from, TEnum to) { return new EnumKeyEnumerator(from, to); }

        public static int E2I(TEnum e)
        {
            var value = EqualityComparer<TEnum>.Default.GetHashCode(e);
            if (needRearrangeHashCode)
            {
                if (value >= 0) { value -= int.MaxValue; }
                else { value += int.MaxValue; }
                --value;
            }
            return value;
        }

        public static TEnum I2E(int ev)
        {
            return enums[ev - DefinedMinInt];
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
                iDst = DefinedLength - 1;
            }

            public ElementEnumerator(EnumArray<TEnum, TElement> array, TEnum from)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = DefinedLength - 1;
            }

            public ElementEnumerator(EnumArray<TEnum, TElement> array, TEnum from, TEnum to)
            {
                elements = array.elements;
                i = iCurrent = E2I(from) - DefinedMinInt;
                iDst = E2I(to) - DefinedMinInt;
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public ElementEnumerator GetEnumerator() { return this; }
            IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
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
                iDst = DefinedLength - 1;
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
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public Enumerator GetEnumerator() { return this; }
            IEnumerator<KeyValuePair<TEnum, TElement>> IEnumerable<KeyValuePair<TEnum, TElement>>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
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