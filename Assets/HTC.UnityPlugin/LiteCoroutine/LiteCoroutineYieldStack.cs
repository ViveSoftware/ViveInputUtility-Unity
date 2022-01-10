//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.LiteCoroutineSystem
{
    public abstract partial class LiteCoroutine : IEnumerator
    {
        private class YieldStack
        {
            private Stack<IEnumerator> stack = new Stack<IEnumerator>();

            public Handle handle;
            public bool waitForUpdate;
            public YieldInstruction yieldInstruction { get; private set; }

            public void Push(IEnumerator routine)
            {
                stack.Push(routine);
            }

            public void Clear()
            {
                waitForUpdate = true;
                yieldInstruction = null;
                handle = null;
                stack.Clear();
            }

            public bool MoveNext()
            {
                while (stack.Count > 0)
                {
                    var routine = stack.Peek();
                    bool hasNext;
                    try
                    {
                        hasNext = routine.MoveNext();
                    }
                    catch (Exception e)
                    {
                        hasNext = false;
                        Debug.LogException(e);
                    }

                    if (!hasNext)
                    {
                        stack.Pop();
                        continue;
                    }

                    yieldInstruction = null;

                    var current = routine.Current;
                    if (current != null)
                    {
                        if (current is IEnumerator)
                        {
                            stack.Push((IEnumerator)current);
                            continue;
                        }
                        else if (current is AsyncOperation)
                        {
                            stack.Push(new WaitForAsyncOperation((AsyncOperation)current));
                            continue;
                        }
                        else if (current is YieldInstruction)
                        {
                            yieldInstruction = (YieldInstruction)current;
                        }
                        else
                        {
                            Debug.LogWarning("Got unknown yield object. " + routine.ToString());
                        }
                    }

                    return true;
                }

                return false;
            }
        }

        private class YieldStackPool
        {
            private Stack<YieldStack> pool = new Stack<YieldStack>();

            public YieldStack Get(Handle handle, IEnumerator routine)
            {
                lock (pool)
                {
                    var stack = pool.Count == 0 ? new YieldStack() : pool.Pop();
                    stack.handle = handle;
                    stack.Push(routine);
                    return stack;
                }
            }

            public void Release(YieldStack stack)
            {
                lock (pool)
                {
                    stack.Clear();
                    pool.Push(stack);
                }
            }
        }
    }
}