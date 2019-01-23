//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

namespace HTC.UnityPlugin.Utility
{
    public class OrderedIndexedSet<T> : IndexedSet<T>
    {
        public OrderedIndexedSet() : base() { }

        public OrderedIndexedSet(int capacity) : base(capacity) { }

        public override void Insert(int index, T item)
        {
            m_Dictionary.Add(item, index);
            m_List.Insert(index, item);

            for (int i = index + 1, imax = m_List.Count; i < imax; ++i)
            {
                m_Dictionary[m_List[i]] = i;
            }
        }

        public override void RemoveAt(int index)
        {
            m_Dictionary.Remove(m_List[index]);
            m_List.RemoveAt(index);

            for (int i = index, imax = m_List.Count; i < imax; ++i)
            {
                m_Dictionary[m_List[i]] = i;
            }
        }

        public T GetFirst()
        {
            return m_List[0];
        }

        public bool TryGetFirst(out T item)
        {
            if (m_List.Count == 0)
            {
                item = default(T);
                return false;
            }
            else
            {
                item = GetFirst();
                return true;
            }
        }

        public T GetLast()
        {
            return m_List[m_List.Count - 1];
        }

        public bool TryGetLast(out T item)
        {
            if (m_List.Count == 0)
            {
                item = default(T);
                return false;
            }
            else
            {
                item = GetLast();
                return true;
            }
        }
    }
}