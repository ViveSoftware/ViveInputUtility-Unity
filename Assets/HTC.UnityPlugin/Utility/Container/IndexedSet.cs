//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HTC.UnityPlugin.Utility
{
    public interface IIndexedSetReadOnly<T> : IEnumerable<T>
    {
        int Count { get; }

        T this[int index] { get; }
        bool Contains(T item);
        void CopyTo(T[] array, int arrayIndex);
        int IndexOf(T item);
    }

    public class IndexedSet<T> : IList<T>, IIndexedSetReadOnly<T>
    {
        private class ReadOnlyWrapper : IIndexedSetReadOnly<T>
        {
            private IndexedSet<T> m_container;

            public ReadOnlyWrapper(IndexedSet<T> container) { m_container = container; }

            public T this[int index] { get { return m_container[index]; } }

            public int Count { get { return m_container.Count; } }

            public bool Contains(T item) { return m_container.Contains(item); }

            public void CopyTo(T[] array, int arrayIndex) { m_container.CopyTo(array, arrayIndex); }

            public int IndexOf(T item) { return m_container.IndexOf(item); }

            public IEnumerator<T> GetEnumerator() { return m_container.GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable)m_container).GetEnumerator(); }
        }

        protected readonly Dictionary<T, int> m_Dictionary;
        protected readonly List<T> m_List;
        protected IIndexedSetReadOnly<T> m_readOnly;

        public IndexedSet()
        {
            m_Dictionary = new Dictionary<T, int>();
            m_List = new List<T>();
        }

        public IndexedSet(int capacity)
        {
            m_Dictionary = new Dictionary<T, int>(capacity);
            m_List = new List<T>(capacity);
        }

        public int Count { get { return m_List.Count; } }

        public bool IsReadOnly { get { return false; } }

        public IIndexedSetReadOnly<T> ReadOnly { get { return m_readOnly ?? (m_readOnly = new ReadOnlyWrapper(this)); } }

        public T this[int index]
        {
            get { return m_List[index]; }
            set
            {
                T item = m_List[index];
                m_Dictionary.Remove(item);
                m_List[index] = value;
                m_Dictionary.Add(value, index);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        public void Add(T item)
        {
            m_Dictionary.Add(item, m_List.Count);
            m_List.Add(item);
        }

        public bool AddUnique(T item)
        {
            if (m_Dictionary.ContainsKey(item)) { return false; }

            Add(item);
            return true;
        }

        public bool Remove(T item)
        {
            int index;
            if (!m_Dictionary.TryGetValue(item, out index)) { return false; }

            RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            m_List.Clear();
            m_Dictionary.Clear();
        }

        public bool Contains(T item)
        {
            return m_Dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_List.CopyTo(array, arrayIndex);
        }

        public int IndexOf(T item)
        {
            int index;
            return m_Dictionary.TryGetValue(item, out index) ? index : -1;
        }

        public virtual void Insert(int index, T item)
        {
            throw new NotSupportedException("Not supported, because this container does not guarantee ordering.");
        }

        public virtual void RemoveAt(int index)
        {
            m_Dictionary.Remove(m_List[index]);

            if (index == m_List.Count - 1)
            {
                m_List.RemoveAt(index);
            }
            else
            {
                var replaceItemIndex = m_List.Count - 1;
                var replaceItem = m_List[replaceItemIndex];
                m_List[index] = replaceItem;
                m_Dictionary[replaceItem] = index;
                m_List.RemoveAt(replaceItemIndex);
            }
        }

        public void RemoveAll(Predicate<T> match)
        {
            var removed = 0;

            for (int i = 0, imax = m_List.Count; i < imax; ++i)
            {
                if (match(m_List[i]))
                {
                    m_Dictionary.Remove(m_List[i]);
                    ++removed;
                }
                else
                {
                    if (removed != 0)
                    {
                        m_Dictionary[m_List[i]] = i - removed;
                        m_List[i - removed] = m_List[i];
                    }
                }
            }

            for (; removed > 0; --removed)
            {
                m_List.RemoveAt(m_List.Count - 1);
            }
        }

        public void Sort(Comparison<T> sortLayoutFunction)
        {
            m_List.Sort(sortLayoutFunction);
            for (int i = m_List.Count - 1; i >= 0; --i)
            {
                m_Dictionary[m_List[i]] = i;
            }
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return m_List.AsReadOnly();
        }
    }
}
