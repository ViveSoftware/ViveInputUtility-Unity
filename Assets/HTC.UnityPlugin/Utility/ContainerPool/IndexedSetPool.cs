//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

namespace HTC.UnityPlugin.Utility
{
    public static class IndexedSetPool<T>
    {
        private static readonly ObjectPool<IndexedSet<T>> pool = new ObjectPool<IndexedSet<T>>(() => new IndexedSet<T>(), null, e => e.Clear());

        public static IndexedSet<T> Get()
        {
            return pool.Get();
        }

        public static void Release(IndexedSet<T> toRelease)
        {
            pool.Release(toRelease);
        }
    }
}