//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;

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
        public abstract string EnumName(int enumInt);
        public abstract void Clear();
        public abstract void EnsureLength();
    }

    public interface IReadOnlyEnumArray<TEnum, TElement> : IEnumerable<TElement>
    {
        Type EnumType { get; }
        Type ElementType { get; }
        TEnum Min { get; }
        TEnum Max { get; }
        int MinInt { get; }
        int MaxInt { get; }
        int Length { get; }
        TElement this[TEnum e] { get; }
        TElement this[int ev] { get; }
        string EnumName(int enumInt);
    }

    [Serializable]
    public abstract class EnumArrayBase<TEnum> : EnumArrayBase
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        public struct DefinedEnumEnumerator : IEnumerable<TEnum>, IEnumerator<TEnum>, IEnumerator, IDisposable
        {
            private int i;

            public TEnum Current { get; private set; }

            object IEnumerator.Current { get { return Current; } }

            void IDisposable.Dispose() { }

            void IEnumerator.Reset() { i = 0; }

            bool IEnumerator.MoveNext()
            {
                while (++i <= EnumLength)
                {
                    if (keyIsValid[i - 1])
                    {
                        Current = keys[i - 1];
                        return true;
                    }
                }

                return false;
            }

            IEnumerator<TEnum> IEnumerable<TEnum>.GetEnumerator() { return this; }

            IEnumerator IEnumerable.GetEnumerator() { return this; }
        }

        private static readonly bool[] keyIsValid;
        private static readonly TEnum[] keys;
        public static readonly int EnumMinInt;
        public static readonly TEnum EnumMin;
        public static readonly int EnumMaxInt;
        public static readonly TEnum EnumMax;
        public static readonly int EnumLength;

        static EnumArrayBase()
        {
#if !CSHARP_7_OR_LATER
            if (!typeof(TEnum).IsEnum) { throw new Exception(typeof(TEnum).Name + " is not enum type!"); }
#endif
            // find out min/max/length value in defined enum values
            EnumMinInt = int.MaxValue;
            EnumMaxInt = int.MinValue;
            var enums = Enum.GetValues(typeof(TEnum)) as TEnum[];
            foreach (var e in enums)
            {
                var i = E2I(e);

                if (i < EnumMinInt)
                {
                    EnumMinInt = i;
                    EnumMin = e;
                }

                if (i > EnumMaxInt)
                {
                    EnumMaxInt = i;
                    EnumMax = e;
                }
            }
            EnumLength = EnumMaxInt - EnumMinInt + 1;

            // create an int array with invalid enum values
            keys = new TEnum[EnumLength];
            keyIsValid = new bool[EnumLength];
            foreach (var e in enums)
            {
                var i = E2I(e) - EnumMinInt;
                keys[i] = e;
                keyIsValid[i] = true;
            }
        }

        public override Type EnumType { get { return typeof(TEnum); } }

        public override int MinInt { get { return EnumMinInt; } }

        public override int MaxInt { get { return EnumMaxInt; } }

        public TEnum Min { get { return EnumMin; } }

        public TEnum Max { get { return EnumMax; } }

        public override string EnumName(int enumInt) { return I2E(enumInt).ToString(); }

        public override int Length { get { return EnumLength; } }

        public static IEnumerable<TEnum> AllDefinedEnums
        {
            get { return new DefinedEnumEnumerator(); }
        }

        public static bool IsDefined(TEnum e)
        {
            return IsDefined(E2I(e));
        }

        public static bool IsDefined(int ev)
        {
            var i = ev - EnumMinInt;
            return i >= 0 && i < EnumLength && keyIsValid[i];
        }

        public static int E2I(TEnum e)
        {
            return EqualityComparer<TEnum>.Default.GetHashCode(e);
        }

        public static TEnum I2E(int ev)
        {
            return keys[ev - EnumMinInt];
        }
    }

    [Serializable]
    public class EnumArray<TEnum, TElement> : EnumArrayBase<TEnum>, IEnumerable<TElement>
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        private class ReadOnlyEnumArray : IReadOnlyEnumArray<TEnum, TElement>
        {
            private readonly EnumArray<TEnum, TElement> source;
            public ReadOnlyEnumArray(EnumArray<TEnum, TElement> source) { this.source = source; }
            public TElement this[TEnum e] { get { return source[e]; } }
            public TElement this[int ev] { get { return source[ev]; } }
            public Type EnumType { get { return source.EnumType; } }
            public Type ElementType { get { return source.ElementType; } }
            public TEnum Min { get { return source.Min; } }
            public TEnum Max { get { return source.Max; } }
            public int MinInt { get { return source.MinInt; } }
            public int MaxInt { get { return source.MaxInt; } }
            public int Length { get { return source.Length; } }
            public string EnumName(int enumInt) { return source.EnumName(enumInt); }
            public IEnumerator<TElement> GetEnumerator() { return source.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return source.GetEnumerator(); }
        }

        [UnityEngine.SerializeField]
        private TElement[] m_array;
        private ReadOnlyEnumArray m_readOnly;

        public EnumArray()
        {
            m_array = new TElement[EnumLength];
        }

        public EnumArray(TElement initValue) : this()
        {
            Clear(initValue);
        }

        public override Type ElementType { get { return typeof(TElement); } }

        public IReadOnlyEnumArray<TEnum, TElement> ReadOnly { get { return m_readOnly != null ? m_readOnly : (m_readOnly = new ReadOnlyEnumArray(this)); } }

        public TElement this[TEnum e]
        {
            get { return this[E2I(e)]; }
            set { this[E2I(e)] = value; }
        }

        public TElement this[int ev]
        {
            get
            {
                var i = ev - EnumMinInt;
                return i >= 0 && i < m_array.Length ? m_array[i] : default(TElement);
            }
            set
            {
                var i = ev - EnumMinInt;
                if (i >= 0 && i < EnumLength)
                {
                    EnsureLength();
                    m_array[i] = value;
                }
            }
        }

        public override void Clear()
        {
            Array.Clear(m_array, 0, m_array.Length);
        }

        public void Clear(TElement clearWith)
        {
            for (int i = m_array.Length - 1; i >= 0; --i) { m_array[i] = clearWith; }
        }

        public override void EnsureLength()
        {
            if (m_array == null) { m_array = new TElement[EnumLength]; }
            else if (m_array.Length < EnumLength)
            {
                var oldArray = m_array;
                m_array = new TElement[EnumLength];
                Array.Copy(oldArray, 0, m_array, 0, oldArray.Length);
            }
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return ((IEnumerable<TElement>)m_array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_array.GetEnumerator();
        }
    }
}