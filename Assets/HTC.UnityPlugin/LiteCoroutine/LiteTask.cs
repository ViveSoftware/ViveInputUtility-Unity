//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace HTC.UnityPlugin.LiteCoroutineSystem
{
    public enum LiteTaskState
    {
        Init,
        Running,
        Done,
        Cancelled,
        Exception
    }

    public class LiteTask : IEnumerator
    {
        public object Current { get; private set; }
        public bool MoveNext() { return OnMoveNext(); }
        public void Reset() { innerRoutine.Reset(); }

        private enum RunningState
        {
            Init,
            RunningAsync,
            PendingYield,
            ToBackground,
            RunningSync,
            CancellationRequested,
            Done,
            Exception
        }

        private class JumpEnumerator : IEnumerator
        {
            object IEnumerator.Current { get { return null; } }
            bool IEnumerator.MoveNext() { return false; }
            void IEnumerator.Reset() { }
        }

        public static readonly IEnumerator ToForground = new JumpEnumerator();
        public static readonly IEnumerator ToBackground = new JumpEnumerator();

        private IEnumerator innerRoutine;
        private RunningState state;
        private object stateLock = new object();
        private bool isBachgroundThreadRunning;

        public LiteTaskState State { get { lock (stateLock) { return ToTaskState(state); } } }

        public Exception Exception { get; protected set; }

        public bool StartInBackground { get; private set; }

        public bool IsDone
        {
            get
            {
                lock (stateLock)
                {
                    var ts = ToTaskState(state);
                    return ts != LiteTaskState.Init && ts != LiteTaskState.Running;
                }
            }
        }

        public LiteTask(IEnumerator routine, bool startInBackground = true)
        {
            innerRoutine = routine;
            state = RunningState.Init;
            StartInBackground = startInBackground;
        }

        private static LiteTaskState ToTaskState(RunningState value)
        {
            switch (value)
            {
                case RunningState.CancellationRequested:
                    return LiteTaskState.Cancelled;
                case RunningState.Done:
                    return LiteTaskState.Done;
                case RunningState.Exception:
                    return LiteTaskState.Exception;
                case RunningState.Init:
                    return LiteTaskState.Init;
                default:
                    return LiteTaskState.Running;
            }
        }

        public void Cancel()
        {
            lock (stateLock)
            {
                switch (state)
                {
                    case RunningState.Done:
                    case RunningState.Exception:
                    case RunningState.CancellationRequested:
                        break;
                    default:
                        state = RunningState.CancellationRequested;
                        break;
                }
            }
        }

        public IEnumerator Wait()
        {
            while (!IsDone)
            {
                yield return null;
            }
        }

        private bool OnMoveNext()
        {
            if (isBachgroundThreadRunning)
            {
                lock (stateLock)
                {
                    switch (state)
                    {
                        case RunningState.RunningAsync:
                            Current = null;
                            return true;

                        case RunningState.PendingYield:
                            state = RunningState.ToBackground;
                            isBachgroundThreadRunning = false;
                            // Current should have been set in MoveNextLoop
                            return true;

                        //case RunningState.RunningSync:
                        //case RunningState.Exception:
                        //case RunningState.CancellationRequested:
                        //case RunningState.Done:
                        default:
                            isBachgroundThreadRunning = false;
                            break;
                    }
                }
            }

            if (state == RunningState.Init)
            {
                state = StartInBackground ? RunningState.ToBackground : RunningState.RunningSync;
            }

            if (state == RunningState.RunningSync)
            {
                MoveNextLoop(false);
            }

            switch (state)
            {
                case RunningState.ToBackground:
                    state = RunningState.RunningAsync;
                    isBachgroundThreadRunning = true;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(MoveNextLoopAsync));
                    Current = null;
                    return true;

                case RunningState.PendingYield:
                    state = RunningState.RunningSync;
                    return true;

                //case RunningState.Exception:
                //case RunningState.CancellationRequested:
                //case RunningState.Done:
                default:
                    Current = null;
                    return false;
            }
        }

        private void MoveNextLoopAsync(object state)
        {
            MoveNextLoop(true);
        }

        private void MoveNextLoop(bool isInBackground)
        {
            while (true)
            {
                bool hasNext;

                try
                {
                    hasNext = innerRoutine.MoveNext();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    lock (stateLock)
                    {
                        state = RunningState.Exception;
                        Exception = ex;
                        return;
                    }
                }

                if (!hasNext)
                {
                    ChangeStateIfNotCanceled(RunningState.Done);
                    return;
                }

                var current = innerRoutine.Current;
                if (current != null)
                {
                    if (current == ToForground)
                    {
                        if (isInBackground)
                        {
                            ChangeStateIfNotCanceled(RunningState.RunningSync);
                            return;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (current == ToBackground)
                    {
                        if (isInBackground)
                        {
                            continue;
                        }
                        else
                        {
                            ChangeStateIfNotCanceled(RunningState.ToBackground);
                            return;
                        }
                    }
                    else if (isInBackground && current is WaitForTicks)
                    {
                        Thread.Sleep(((WaitForTicks)current).duration);
                        continue;
                    }
                }

                ChangeStateIfNotCanceled(RunningState.PendingYield, current);
                return;
            }
        }

        private void ChangeStateIfNotCanceled(RunningState value, object current = null)
        {
            lock (stateLock)
            {
                if (state != RunningState.CancellationRequested)
                {
                    Current = current;
                    state = value;
                }
            }
        }
    }
}