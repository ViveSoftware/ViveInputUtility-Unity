//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;

namespace HTC.UnityPlugin.LiteCoroutineSystem
{
    public static class LiteCoroutineExtension
    {
        public static bool IsNullOrDone(this LiteCoroutine handle)
        {
            return handle == null || handle.IsDone;
        }
    }

    public abstract partial class LiteCoroutine : IEnumerator
    {
        public abstract LiteCoroutineManager OwnerManager { get; }

        public abstract bool IsDone { get; }

        public abstract void Stop();

        public abstract void RestartCoroutine(IEnumerator routine, bool runImmediate = true);

        public object Current { get { return null; } }

        public bool MoveNext() { return !IsDone; }

        public void Reset() { throw new NotImplementedException(); }

        private sealed class Handle : LiteCoroutine
        {
            private readonly Manager manager;
            public bool isDone;
            public YieldStack stack;

            public override LiteCoroutineManager OwnerManager
            {
                get { return manager; }
            }

            public override bool IsDone
            {
                get { lock (this) { return isDone; } }
            }

            public Handle(Manager manager)
            {
                this.manager = manager;
            }

            public override void Stop()
            {
                lock (this)
                {
                    stack = null;
                    isDone = true;
                }
            }

            public override void RestartCoroutine(IEnumerator routine, bool runImmediate = true)
            {
                manager.AddPendingYieldStack(this, routine, runImmediate);
            }
        }
    }
}