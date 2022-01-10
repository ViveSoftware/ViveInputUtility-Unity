//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
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

        public static readonly IEnumerator ToForeground = new JumpEnumerator();
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
            StartInBackground = true;
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

        public LiteTask RestartTask(IEnumerator routine)
        {
            lock (stateLock)
            {
                if (!IsDone) { throw new Exception("Task not done yet."); }
                innerRoutine = routine;
                current = null;
                Exception = null;
                state = routine == null ? RunningState.Done : RunningState.Init;
                return this;
            }
        }

        public LiteTask RestartTask(IEnumerator routine, bool startInBackground)
        {
            lock (stateLock)
            {
                if (!IsDone) { throw new Exception("Task not done yet."); }
                innerRoutine = routine;
                current = null;
                Exception = null;
                state = routine == null ? RunningState.Done : RunningState.Init;
                StartInBackground = startInBackground;
                return this;
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
                    current = null;
                    state = RunningState.RunningBackground;
                    ThreadPool.QueueUserWorkItem(wcBackgroundMoveNextState);
                    return true;
                }

                return false;
            }
        }

        private bool InnerMoveNext(out object innerCurrent, out Exception innerException)
        {
            bool hasNext;
            try
            {
                hasNext = innerRoutine.MoveNext();
                innerCurrent = hasNext ? innerRoutine.Current : null;
                innerException = null;
            }
            catch (Exception ex)
            {
                hasNext = false;
                innerCurrent = null;
                innerException = ex;
            }
            return hasNext;
        }

        private void ForegroundMoveNextState()
        {
            while (ForegroundInnerMoveNext()) { }
        }

        private bool ForegroundInnerMoveNext()
        {
            bool hasNext;
            object innerCurrent;
            Exception innerException;

            hasNext = InnerMoveNext(out innerCurrent, out innerException);

            lock (stateLock)
            {
                if (innerException != null)
                {
                    Exception = innerException;
                    state = RunningState.Exception;

                    if (!Silent) { Debug.LogException(innerException); }
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

                if (innerCurrent != null)
                {
                    if (innerCurrent == ToBackground)
                    {
                        state = RunningState.ToBackground;
                        return false;
                    }

                    if (innerCurrent == ToForeground)
                    {
                        return true;
                    }
                }

                current = innerCurrent;
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
            object innerCurrent;
            Exception innerException;
            outSleepTime = TimeSpan.Zero;

            hasNext = InnerMoveNext(out innerCurrent, out innerException);

            lock (stateLock)
            {
                if (innerException != null)
                {
                    Exception = innerException;
                    state = RunningState.Exception;

                    if (!Silent) { Debug.LogException(innerException); }
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

                if (innerCurrent != null)
                {
                    if (innerCurrent == ToBackground)
                    {
                        return true;
                    }

                    if (innerCurrent == ToForeground)
                    {
                        state = RunningState.ToForeground;
                        return false;
                    }

                    if (innerCurrent is WaitForTicks)
                    {
                        outSleepTime = ((WaitForTicks)innerCurrent).waitTime;
                        return true;
                    }
                }

                current = innerCurrent;
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

    public class LiteTaskPool
    {
        private static LiteTaskPool defaultPool;
        public static LiteTaskPool Default { get { return defaultPool ?? (defaultPool = new LiteTaskPool()); } }

        private Stack<LiteTask> pool = new Stack<LiteTask>();
        private HashSet<LiteTask> set = new HashSet<LiteTask>();
        private List<LiteTask> runningTasks;
        private LiteCoroutine waitRoutine;
        private Predicate<LiteTask> removeFinishedTask;

        public int poolCount { get { lock (pool) { return pool.Count; } } }

        public int releasingCount { get { lock (pool) { return runningTasks.Count; } } }

        public LiteTask Get(IEnumerator routine = null, bool startInBackground = true)
        {
            lock (pool)
            {
                if (pool.Count == 0)
                {
                    return new LiteTask(routine, startInBackground);
                }
                else
                {
                    var task = pool.Pop();
                    set.Remove(task);
                    return task.RestartTask(routine, startInBackground);
                }
            }
        }

        public LiteTask Get(out LiteTask task, IEnumerator routine = null, bool startInBackground = true)
        {
            lock (pool)
            {
                if (pool.Count == 0)
                {
                    return task = new LiteTask(routine, startInBackground);
                }
                else
                {
                    task = pool.Pop();
                    set.Remove(task);
                    return task.RestartTask(routine, startInBackground);
                }
            }
        }

        public void Release(LiteTask task)
        {
            lock (pool)
            {
                if (task != null && set.Add(task))
                {
                    task.Cancel();

                    if (task.IsDone)
                    {
                        task.RestartTask(null, true);
                        pool.Push(task);
                    }
                    else
                    {
                        if (runningTasks == null)
                        {
                            runningTasks = new List<LiteTask>() { task };
                            removeFinishedTask = RemoveFinishedTask;
                        }
                        else
                        {
                            runningTasks.Add(task);
                        }

                        if (waitRoutine.IsNullOrDone())
                        {
                            LiteCoroutine.StartCoroutine(ref waitRoutine, WaitForTasks());
                        }
                    }
                }
            }
        }

        private IEnumerator WaitForTasks()
        {
            while (true)
            {
                lock (pool)
                {
                    runningTasks.RemoveAll(removeFinishedTask);
                    if (runningTasks.Count == 0) { yield break; }
                }
                yield return null;
            }
        }

        private bool RemoveFinishedTask(LiteTask task)
        {
            if (!task.IsDone) { return false; }
            task.RestartTask(null, true);
            pool.Push(task);
            return true;
        }
    }
}