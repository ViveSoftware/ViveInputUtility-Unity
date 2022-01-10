//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;

namespace HTC.UnityPlugin.Utility
{
    public static class ListPool<T>
    {
        private static readonly ObjectPool<List<T>> pool = new ObjectPool<List<T>>(() => new List<T>(), null, e => e.Clear());

        public static List<T> Get()
        {
            return pool.Get();
        }

        public static void Release(List<T> toRelease)
        {
            pool.Release(toRelease);
        }
    }
}