//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Utility
{
    [Serializable]
    public abstract class EnumArrayBase
    {
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

            object IEnumerator.Current { get { return Current; } }
            public TEnum Current { get { return enums[i - 1]; } }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public EnumKeyEnumerator GetEnumerator() { return this; }
            IEnumerator<TEnum> IEnumerable<TEnum>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                while (i < DefinedLength)
                {
                    var index = i++;
                    if (enumIsDefined[index])
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static readonly bool[] enumIsDefined;
        private static readonly TEnum[] enums;
        public static readonly int DefinedMinInt;
        public static readonly TEnum DefinedMin;
        public static readonly int DefinedMaxInt;
        public static readonly TEnum DefinedMax;
        public static readonly int DefinedLength;

        static EnumArrayBase()
        {
#if !CSHARP_7_OR_LATER
            if (!typeof(TEnum).IsEnum) { throw new Exception(typeof(TEnum).Name + " is not enum type!"); }
#endif
            // find out min/max/length value in defined enum values
            DefinedMinInt = int.MaxValue;
            DefinedMaxInt = int.MinValue;
            var enums = Enum.GetValues(typeof(TEnum)) as TEnum[];
            foreach (var e in enums)
            {
                var i = E2I(e);

                if (i < DefinedMinInt)
                {
                    DefinedMinInt = i;
                    DefinedMin = e;
                }

                if (i > DefinedMaxInt)
                {
                    DefinedMaxInt = i;
                    DefinedMax = e;
                }
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

        public EnumKeyEnumerator EnumKeys { get { return BaseEnumKeys; } }

        public static Type BaseEnumType { get { return typeof(TEnum); } }

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

        public static int E2I(TEnum e)
        {
            return EqualityComparer<TEnum>.Default.GetHashCode(e);
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
            int Length { get; }
            int Capacity { get; }
            TElement this[TEnum e] { get; }
            TElement this[int ev] { get; }
            string EnumName(int enumInt);
            EnumKeyEnumerator EnumKeys { get; }
            ElementEnumerator Elements { get; }
            new Enumerator GetEnumerator();
        }

        public struct ElementEnumerator : IEnumerator<TElement>, IEnumerable<TElement>
        {
            private readonly TElement[] elements;
            private int i;

            object IEnumerator.Current { get { return Current; } }
            public TElement Current { get { return elements[i - 1]; } }

            public ElementEnumerator(TElement[] elements)
            {
                i = 0;
                this.elements = elements;
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public ElementEnumerator GetEnumerator() { return this; }
            IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                while (i < DefinedLength)
                {
                    var index = i++;
                    var enumInt = index + DefinedMinInt;
                    if (BaseIsEnumIntDefined(enumInt))
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

            object IEnumerator.Current { get { return Current; } }
            public KeyValuePair<TEnum, TElement> Current { get { return new KeyValuePair<TEnum, TElement>(I2E(i + DefinedMinInt - 1), elements[i - 1]); } }

            public Enumerator(TElement[] elements)
            {
                i = 0;
                this.elements = elements;
            }

            void IDisposable.Dispose() { }
            public void Reset() { i = 0; }
            public Enumerator GetEnumerator() { return this; }
            IEnumerator<KeyValuePair<TEnum, TElement>> IEnumerable<KeyValuePair<TEnum, TElement>>.GetEnumerator() { return this; }
            IEnumerator IEnumerable.GetEnumerator() { return this; }

            public bool MoveNext()
            {
                while (i < DefinedLength)
                {
                    var index = i++;
                    var enumInt = index + DefinedMinInt;
                    if (BaseIsEnumIntDefined(enumInt))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private class ReadOnlyEnumArray : IReadOnly
        {
            private readonly EnumArray<TEnum, TElement> source;
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
            public ElementEnumerator Elements { get { return source.Elements; } }
            public Enumerator GetEnumerator() { return source.GetEnumerator(); }
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

        public override int Length { get { return DefinedLength; } }

        public override int Capacity { get { return elements == null ? 0 : elements.Length; } }

        public IReadOnly ReadOnly { get { return readOnly != null ? readOnly : (readOnly = new ReadOnlyEnumArray(this)); } }

        public ElementEnumerator Elements { get { FillCapacityToLength(); return new ElementEnumerator(elements); } }

        public Enumerator GetEnumerator() { FillCapacityToLength(); return new Enumerator(elements); }

        IEnumerator<KeyValuePair<TEnum, TElement>> IEnumerable<KeyValuePair<TEnum, TElement>>.GetEnumerator() { return GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public TElement this[TEnum e]
        {
            get { return this[E2I(e)]; }
            set { this[E2I(e)] = value; }
        }

        public TElement this[int ev]
        {
            get { FillCapacityToLength(); return elements[ev - MinInt]; }
            set { FillCapacityToLength(); elements[ev - MinInt] = value; }
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