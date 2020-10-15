using System;
using System.Collections;
using System.Collections.Generic;

namespace HTC.UnityPlugin.Utility
{
    public abstract class EnumArrayBase<TEnum>
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

    public class EnumArray<TEnum, TValue> : EnumArrayBase<TEnum>, IEnumerable<TValue>
#if CSHARP_7_OR_LATER
        where TEnum : Enum
#endif
    {
        private readonly TValue[] m_array;

        public EnumArray()
        {
            m_array = new TValue[EnumLength];
        }

        public EnumArray(TValue initValue) : this()
        {
            for (int i = 0, imax = m_array.Length; i < imax; ++i)
            {
                m_array[i] = initValue;
            }
        }

        public TEnum Min { get { return EnumMin; } }

        public TEnum Max { get { return EnumMax; } }

        public int Length { get { return EnumLength; } }

        public TValue this[TEnum e]
        {
            get { return this[E2I(e)]; }
            set { this[E2I(e)] = value; }
        }

        public TValue this[int ev]
        {
            get { return m_array[ev - EnumMinInt]; }
            set { m_array[ev - EnumMinInt] = value; }
        }

        public void Clear()
        {
            Array.Clear(m_array, 0, m_array.Length);
        }

        public void Clear(TValue clearWith)
        {
            for (int i = m_array.Length - 1; i >= 0; --i) { m_array[i] = clearWith; }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return ((IEnumerable<TValue>)m_array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_array.GetEnumerator();
        }
    }
}