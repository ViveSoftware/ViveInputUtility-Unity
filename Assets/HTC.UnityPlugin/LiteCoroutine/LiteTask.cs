//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

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
        Exception,
    }

    public class LiteTask : IEnumerator
    {
        private enum RunningState
        {
            Init,
            RunningBackground,
            RunningForeground,
            ToBackground,
            ToForeground,
            PendingYield,
            CancelledBackground,
            Done,
            Cancelled,
            Exception,
        }

        private class JumpEnumerator : IEnumerator
        {
            object IEnumerator.Current { get { return null; } }
            bool IEnumerator.MoveNext() { return false; }
            void IEnumerator.Reset() { }
        }

        public static readonly IEnumerator ToForground = new JumpEnumerator();
        public static readonly IEnumerator ToBackground = new JumpEnumerator();

        private readonly WaitCallback wcBackgroundMoveNextState;
        private readonly object stateLock = new object();
        private IEnumerator innerRoutine;
        private RunningState state;
        private object current;

        public LiteTaskState State { get { lock (stateLock) { return ToTaskState(state); } } }

        public bool Silent { get; set; }

        public bool StartInBackground { get; private set; }

        public Exception Exception { get; private set; }

        object IEnumerator.Current { get { return current; } }

        public bool IsDone
        {
            get
            {
                lock (stateLock)
                {
                    switch (state)
                    {
                        case RunningState.Cancelled:
                        case RunningState.Done:
                        case RunningState.Exception:
                            return true;
                        default:
                            return false;
                    }
                }
            }
        }

        public LiteTask()
        {
            wcBackgroundMoveNextState = BackgroundMoveNextState;
            state = RunningState.Done;
        }

        public LiteTask(IEnumerator routine, bool startInBackground = true)
        {
            wcBackgroundMoveNextState = BackgroundMoveNextState;
            innerRoutine = routine;
            StartInBackground = startInBackground;
            state = routine == null ? RunningState.Done : RunningState.Init;
        }

        void IEnumerator.Reset() { throw new NotImplementedException(); }

        public void RestartTask(IEnumerator routine)
        {
            lock (stateLock)
            {
                if (!IsDone) { throw new Exception("Task not done yet."); }
                innerRoutine = routine;
                state = routine == null ? RunningState.Done : RunningState.Init;
            }
        }

        public void RestartTask(IEnumerator routine, bool startInBackground)
        {
            lock (stateLock)
            {
                if (!IsDone) { throw new Exception("Task not done yet."); }
                innerRoutine = routine;
                state = routine == null ? RunningState.Done : RunningState.Init;
                StartInBackground = startInBackground;
            }
        }

        private static LiteTaskState ToTaskState(RunningState value)
        {
            switch (value)
            {
                case RunningState.Done:
                    return LiteTaskState.Done;
                case RunningState.Cancelled:
                    return LiteTaskState.Cancelled;
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
                    case RunningState.CancelledBackground:
                    case RunningState.Done:
                    case RunningState.Exception:
                    case RunningState.Cancelled:
                        break;
                    case RunningState.RunningBackground:
                        state = RunningState.CancelledBackground;
                        break;
                    default:
                        state = RunningState.Cancelled;
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

        bool IEnumerator.MoveNext()
        {
            lock (stateLock)
            {
                switch (state)
                {
                    case RunningState.Init:
                        state = StartInBackground ? RunningState.ToBackground : RunningState.ToForeground;
                        break;
                    case RunningState.RunningBackground:
                    case RunningState.CancelledBackground:
                        current = null;
                        return true;
                    case RunningState.PendingYield:
                        state = RunningState.ToBackground;
                        return true;
                }

                if (state == RunningState.ToForeground)
                {
                    state = RunningState.RunningForeground;
                    ForegroundMoveNextState();

                    if (state == RunningState.PendingYield)
                    {
                        state = RunningState.ToForeground;
                        return true;
                    }
                }

                if (state == RunningState.ToBackground)
                {
                    state = RunningState.RunningBackground;
                    ThreadPool.QueueUserWorkItem(wcBackgroundMoveNextState);
                    current = null;
                    return true;
                }

                return false;
            }
        }

        private bool InnerMoveNext()
        {
            bool hasNext;
            try
            {
                hasNext = innerRoutine.MoveNext();
                current = hasNext ? innerRoutine.Current : null;
                Exception = null;
            }
            catch (Exception ex)
            {
                hasNext = false;
                current = null;
                Exception = ex;
            }
            return hasNext;
        }

        private void ForegroundMoveNextState()
        {
            while (ForegroundInnerMoveNext()) { }
        }

        private bool ForegroundInnerMoveNext()
        {
            var hasNext = InnerMoveNext();

            lock (stateLock)
            {
                if (Exception != null)
                {
                    state = RunningState.Exception;

                    if (!Silent) { Debug.LogException(Exception); }

                    return false;
                }

                if (state == RunningState.Cancelled)
                {
                    return false;
                }

                if (!hasNext)
                {
                    state = RunningState.Done;
                    return false;
                }

                if (current != null)
                {
                    if (current == ToBackground)
                    {
                        state = RunningState.ToBackground;
                        return false;
                    }

                    if (current == ToForground)
                    {
                        return true;
                    }
                }

                state = RunningState.PendingYield;
                return false;
            }
        }

        private void BackgroundMoveNextState(object o)
        {
            TimeSpan sleepTime;
            while (BackgroundInnerMoveNext(out sleepTime) && BackgroundSleep(sleepTime)) { }
        }

        private bool BackgroundInnerMoveNext(out TimeSpan outSleepTime)
        {
            bool hasNext;
            outSleepTime = TimeSpan.Zero;

            hasNext = InnerMoveNext();

            lock (stateLock)
            {
                if (Exception != null)
                {
                    state = RunningState.Exception;

                    if (!Silent) { Debug.LogException(Exception); }

                    return false;
                }

                if (state == RunningState.CancelledBackground)
                {
                    state = RunningState.Cancelled;
                    return false;
                }

                if (!hasNext)
                {
                    state = RunningState.Done;
                    return false;
                }

                if (current != null)
                {
                    if (current == ToBackground)
                    {
                        return true;
                    }

                    if (current == ToForground)
                    {
                        state = RunningState.ToForeground;
                        return false;
                    }

                    if (current is WaitForTicks)
                    {
                        outSleepTime = ((WaitForTicks)current).waitTime;
                        return true;
                    }
                }

                state = RunningState.PendingYield;
                return false;
            }
        }

        private bool BackgroundSleep(TimeSpan inSleepTime)
        {
            if (inSleepTime <= TimeSpan.Zero) { return true; }

            Thread.Sleep(inSleepTime);

            lock (stateLock)
            {
                if (state == RunningState.CancelledBackground)
                {
                    state = RunningState.Cancelled;
                    return false;
                }

                return true;
            }
        }
    }
}