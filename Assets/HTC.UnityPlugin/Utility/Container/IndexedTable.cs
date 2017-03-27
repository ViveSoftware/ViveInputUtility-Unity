//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HTC.UnityPlugin.Utility
{
    public interface IIndexedTableReadOnly<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        int Count { get; }
        ICollection<TKey> Keys { get; }
        ICollection<TValue> Values { get; }

        TValue this[TKey key] { get; }

        TKey GetKeyByIndex(int index);
        TValue GetValueByIndex(int index);
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);
        void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);
        int IndexOf(TKey item);
    }

    public class IndexedTable<TKey, TValue> : IDictionary<TKey, TValue>, IIndexedTableReadOnly<TKey, TValue>
    {
        private class ReadOnlyWrapper : IIndexedTableReadOnly<TKey, TValue>
        {
            private IndexedTable<TKey, TValue> m_container;

            public ReadOnlyWrapper(IndexedTable<TKey, TValue> container) { m_container = container; }

            public TValue this[TKey key] { get { return m_container[key]; } }

            public int Count { get { return m_container.Count; } }

            public ICollection<TKey> Keys { get { return m_container.Keys; } }

            public ICollection<TValue> Values { get { return m_container.Values; } }

            public bool ContainsKey(TKey key) { return m_container.ContainsKey(key); }

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { m_container.CopyTo(array, arrayIndex); }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { return m_container.GetEnumerator(); }

            public int IndexOf(TKey item) { return m_container.IndexOf(item); }

            public TKey GetKeyByIndex(int index) { return m_container.GetKeyByIndex(index); }

            public TValue GetValueByIndex(int index) { return m_container.GetValueByIndex(index); }

            public bool TryGetValue(TKey key, out TValue value) { return m_container.TryGetValue(key, out value); }

            IEnumerator IEnumerable.GetEnumerator() { return ((IIndexedTableReadOnly<TKey, TValue>)m_container).GetEnumerator(); }
        }

        protected readonly Dictionary<TKey, int> m_Dictionary;
        protected readonly List<TKey> m_KeyList;
        protected readonly List<TValue> m_ValueList;
        protected IIndexedTableReadOnly<TKey, TValue> m_readOnly;

        public IndexedTable()
        {
            m_Dictionary = new Dictionary<TKey, int>();
            m_KeyList = new List<TKey>();
            m_ValueList = new List<TValue>();
        }

        public IndexedTable(int capacity)
        {
            m_Dictionary = new Dictionary<TKey, int>(capacity);
            m_KeyList = new List<TKey>(capacity);
            m_ValueList = new List<TValue>(capacity);
        }

        public int Count
        {
            get
            {
                if (m_Dictionary.Count != m_KeyList.Count || m_Dictionary.Count != m_ValueList.Count)
                {
                    UnityEngine.Debug.LogWarning("m_Dictionary.Count=" + m_Dictionary.Count + " m_KeyList.Count=" + m_KeyList.Count + " m_ValueList.Count=" + m_ValueList.Count);
                    UnityEngine.Debug.LogWarning(m_Dictionary.ToString());
                    UnityEngine.Debug.LogWarning(m_KeyList.ToString());
                    UnityEngine.Debug.LogWarning(m_ValueList.ToString());
                }
                return m_Dictionary.Count;
            }
        }

        public bool IsReadOnly { get { return false; } }

        public IIndexedTableReadOnly<TKey, TValue> ReadOnly { get { return m_readOnly ?? (m_readOnly = new ReadOnlyWrapper(this)); } }

        public TKey GetKeyByIndex(int index)
        {
            if (index < 0 || index >= m_KeyList.Count)
            {
                UnityEngine.Debug.LogWarning("index=" + index + " m_Dictionary.Count=" + m_Dictionary.Count + " m_KeyList.Count=" + m_KeyList.Count + " m_ValueList.Count=" + m_ValueList.Count);
                string msg = "{ ";
                for (int i = 0; i < m_KeyList.Count; ++i)
                {
                    msg += m_KeyList[i].ToString() + ",";
                }
                UnityEngine.Debug.LogWarning("KeyList=" + msg + " }");
            }
            return m_KeyList[index];
        }

        public TValue GetValueByIndex(int index)
        {
            return m_ValueList[index];
        }

        public KeyValuePair<TKey, TValue> GetKeyValuePairByIndex(int index)
        {
            return new KeyValuePair<TKey, TValue>(m_KeyList[index], m_ValueList[index]);
        }

        public ICollection<TKey> Keys { get { return m_Dictionary.Keys; } }

        public ICollection<TValue> Values { get { return new List<TValue>(m_ValueList); } }

        public TValue this[TKey key]
        {
            get { return m_ValueList[m_Dictionary[key]]; }
            set
            {
                int index;
                if (m_Dictionary.TryGetValue(key, out index))
                {
                    m_ValueList[index] = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public void Add(TKey key, TValue value = default(TValue))
        {
            m_Dictionary.Add(key, m_Dictionary.Count);
            m_KeyList.Add(key);
            m_ValueList.Add(value);
        }

        public bool AddUniqueKey(TKey key, TValue value = default(TValue))
        {
            if (m_Dictionary.ContainsKey(key)) { return false; }

            Add(key, value);
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return m_Dictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            int index;
            if (!m_Dictionary.TryGetValue(key, out index)) { return false; }

            RemoveAt(index);
            return true;
        }

        public virtual void RemoveAt(int index)
        {
            m_Dictionary.Remove(m_KeyList[index]);

            if (index == m_KeyList.Count - 1)
            {
                m_KeyList.RemoveAt(index);
                m_ValueList.RemoveAt(index);
            }
            else
            {
                var replaceItemIndex = m_KeyList.Count - 1;
                var replaceItemKey = m_KeyList[replaceItemIndex];
                var replaceItemValue = m_ValueList[replaceItemIndex];

                m_KeyList[index] = replaceItemKey;
                m_ValueList[index] = replaceItemValue;

                m_Dictionary[replaceItemKey] = index;

                m_KeyList.RemoveAt(replaceItemIndex);
                m_ValueList.RemoveAt(replaceItemIndex);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index;
            if (m_Dictionary.TryGetValue(key, out index))
            {
                value = m_ValueList[index];
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int index;
            if (!m_Dictionary.TryGetValue(item.Key, out index)) { return false; }
            return EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(GetKeyValuePairByIndex(index), item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array is null.");
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex is less than 0.");
            }

            if (array.Length - arrayIndex < m_Dictionary.Count)
            {
                throw new ArgumentException("The number of elements in the source IndexedTable<TKey, TValue> is greater than the available space from arrayIndex to the end of the destination array.");
            }

            for (int i = 0, imax = m_Dictionary.Count; i < imax; ++i)
            {
                array[i + arrayIndex] = new KeyValuePair<TKey, TValue>(m_KeyList[i], m_ValueList[i]);
            }
        }

        public int IndexOf(TKey item)
        {
            int index;
            return m_Dictionary.TryGetValue(item, out index) ? index : -1;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Contains(item)) { return false; }
            return Remove(item.Key);
        }

        public void RemoveAll(Predicate<KeyValuePair<TKey, TValue>> match)
        {
            var removed = 0;

            for (int i = 0, imax = m_Dictionary.Count; i < imax; ++i)
            {
                if (match(GetKeyValuePairByIndex(i)))
                {
                    m_Dictionary.Remove(m_KeyList[i]);
                    ++removed;
                }
                else
                {
                    if (removed != 0)
                    {
                        m_Dictionary[m_KeyList[i]] = i - removed;
                        m_KeyList[i - removed] = m_KeyList[i];
                        m_ValueList[i - removed] = m_ValueList[i];
                    }
                }
            }

            for (; removed > 0; --removed)
            {
                m_KeyList.RemoveAt(m_KeyList.Count - 1);
                m_ValueList.RemoveAt(m_ValueList.Count - 1);
            }
        }

        private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private int iterator = 0;
            private IndexedTable<TKey, TValue> container;

            public Enumerator(IndexedTable<TKey, TValue> c) { container = c; }

            public KeyValuePair<TKey, TValue> Current { get { return container.GetKeyValuePairByIndex(iterator); } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() { container = null; }

            public bool MoveNext() { ++iterator; return iterator < container.Count; }

            public void Reset() { iterator = 0; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            m_Dictionary.Clear();
            m_KeyList.Clear();
            m_ValueList.Clear();
        }

        public ReadOnlyCollection<TKey> AsReadOnly()
        {
            return m_KeyList.AsReadOnly();
        }
    }
}