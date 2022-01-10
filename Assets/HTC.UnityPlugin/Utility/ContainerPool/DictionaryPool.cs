//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;

namespace HTC.UnityPlugin.Utility
{
    public static class DictionaryPool<TKey, TValue>
    {
        private static readonly ObjectPool<Dictionary<TKey, TValue>> pool = new ObjectPool<Dictionary<TKey, TValue>>(() => new Dictionary<TKey, TValue>(), null, e => e.Clear());

        public static Dictionary<TKey, TValue> Get()
        {
            return pool.Get();
        }

        public static void Release(Dictionary<TKey, TValue> toRelease)
        {
            pool.Release(toRelease);
        }
    }
}