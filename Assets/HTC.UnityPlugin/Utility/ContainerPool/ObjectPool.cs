//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Utility
{
    public class ObjectPool<T>
    {
        private readonly Stack<T> stack = new Stack<T>();
        private readonly Func<T> actionOnNew;
        private readonly UnityAction<T> actionOnGet;
        private readonly UnityAction<T> actionOnRelease;

        public int CountAll { get; private set; }
        public int CountInactive { get { return stack.Count; } }
        public int CountActive { get { return CountAll - CountInactive; } }

        public ObjectPool(Func<T> onNew, UnityAction<T> onGet = null, UnityAction<T> onRelease = null)
        {
            actionOnNew = onNew;
            actionOnGet = onGet;
            actionOnRelease = onRelease;
        }

        public T Get()
        {
            T element;
            if (stack.Count == 0)
            {
                element = actionOnNew == null ? default(T) : actionOnNew.Invoke();
                CountAll++;
            }
            else
            {
                element = stack.Pop();
            }

            if (actionOnGet != null) { actionOnGet.Invoke(element); }
            return element;
        }

        public void Release(T element)
        {
            if (stack.Count > 0 && ReferenceEquals(stack.Peek(), element))
            {
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            }

            if (actionOnRelease != null) { actionOnRelease.Invoke(element); }

            stack.Push(element);

            if (stack.Count > CountAll)
            {
                CountAll = stack.Count;
            }
        }
    }
}