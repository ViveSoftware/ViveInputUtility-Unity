//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.LiteCoroutineSystem
{
    public abstract class LiteCoroutineManager
    {
        public abstract int CoroutineCount { get; }

        public abstract bool NoDelayCalls { get; }

        public abstract event Action DelayUpdateCall;

        public abstract event Action DelayLateUpdateCall;

        public abstract event Action DelayFixedUpdateCall;

        public abstract event Action DelayEndOfFrameCall;

        public abstract void MainUpdate();

        public abstract void LateUpdate();

        public abstract void FixedeUpdate();

        public abstract void EndOfFrameUpdate();

        public abstract LiteCoroutine CreateHandle();

        public abstract void StopAllCoroutine();

        public LiteCoroutine StartCoroutine(IEnumerator routine, bool runImmediate = true)
        {
            var handle = CreateHandle();
            handle.RestartCoroutine(routine, runImmediate);
            return handle;
        }

        public LiteCoroutine StartCoroutine(ref LiteCoroutine handle, IEnumerator routine, bool runImmediate = true)
        {
            if (handle == null || handle.OwnerManager != this)
            {
                handle = CreateHandle();
            }

            handle.RestartCoroutine(routine, runImmediate);
            return handle;
        }
    }

    public abstract partial class LiteCoroutine : IEnumerator
    {
        // able to start coroutine in background thread
        private sealed class Manager : LiteCoroutineManager
        {
            private readonly YieldStackPool pool = new YieldStackPool();
            private readonly List<YieldStack> workingStacks = new List<YieldStack>();
            private readonly List<YieldStack> tempStageStacks = new List<YieldStack>();
            private readonly List<YieldStack> lateUpdateStageStacks = new List<YieldStack>();
            private readonly List<YieldStack> fixedUpdateStageStacks = new List<YieldStack>();
            private readonly List<YieldStack> endOfFrameStageStacks = new List<YieldStack>();
            private Predicate<YieldStack> removeAllInvalidYieldStackPredicate;

            private readonly object delayCallLock = new object();
            private Action delayUpdateCall;
            private Action delayLateUpdateCall;
            private Action delayFixedUpdateCall;
            private Action delayEndOfFrameCall;

            public override int CoroutineCount { get { lock (workingStacks) { return workingStacks.Count; } } }

            public override bool NoDelayCalls { get { lock (delayCallLock) { return delayUpdateCall == null && delayLateUpdateCall == null && delayFixedUpdateCall == null && delayEndOfFrameCall == null; } } }

            public override event Action DelayUpdateCall { add { SafeAddDelayAction(ref delayUpdateCall, value); } remove { SafeRemoveDelayAction(ref delayUpdateCall, value); } }

            public override event Action DelayLateUpdateCall { add { SafeAddDelayAction(ref delayLateUpdateCall, value); } remove { SafeRemoveDelayAction(ref delayLateUpdateCall, value); } }

            public override event Action DelayFixedUpdateCall { add { SafeAddDelayAction(ref delayFixedUpdateCall, value); } remove { SafeRemoveDelayAction(ref delayFixedUpdateCall, value); } }

            public override event Action DelayEndOfFrameCall { add { SafeAddDelayAction(ref delayEndOfFrameCall, value); } remove { SafeRemoveDelayAction(ref delayEndOfFrameCall, value); } }

            public Manager()
            {
                removeAllInvalidYieldStackPredicate = RemoveAllInvalidYieldStackPredicate;
            }

            private void SafeAddDelayAction(ref Action delayActions, Action value)
            {
                if (value != null)
                {
                    lock (delayCallLock)
                    {
                        if (delayActions == null) { delayActions = value; }
                        else { delayActions += value; }
                    }
                }
            }

            private void SafeRemoveDelayAction(ref Action delayActions, Action value)
            {
                if (value != null && delayActions != null)
                {
                    lock (delayCallLock)
                    {
                        delayActions -= value;
                    }
                }
            }

            private void ExecuteDelayAction(ref Action delayActions)
            {
                if (delayActions != null)
                {
                    Action temp;
                    lock (delayCallLock)
                    {
                        temp = delayActions;
                        delayActions = null;
                    }
                    temp.Invoke();
                }
            }

            public override LiteCoroutine CreateHandle()
            {
                return new Handle(this);
            }

            public void AddPendingYieldStack(Handle handle, IEnumerator routine, bool runImmediate)
            {
                var newStack = pool.Get(handle, routine);
                newStack.waitForUpdate = true;

                if (runImmediate)
                {
                    if (!newStack.MoveNext())
                    {
                        pool.Release(newStack);
                        newStack = null;
                    }
                }

                if (newStack == null)
                {
                    lock (handle)
                    {
                        handle.stack = null;
                        handle.isDone = true;
                    }
                }
                else
                {
                    lock (workingStacks)
                    {
                        lock (handle)
                        {
                            handle.stack = newStack;
                            handle.isDone = false;
                            workingStacks.Add(newStack);

                            List<YieldStack> stageStacks;
                            if (runImmediate && TryGetOtherStageFromYieldInstruction(newStack.yieldInstruction, out stageStacks))
                            {
                                lock (stageStacks)
                                {
                                    newStack.waitForUpdate = false;
                                    stageStacks.Add(newStack);
                                }
                            }
                        }
                    }
                }
            }

            private bool RemoveAllInvalidYieldStackPredicate(YieldStack stack)
            {
                bool validHandle;
                var handle = stack.handle;
                lock (handle)
                {
                    validHandle = !handle.isDone && handle.stack == stack;
                }

                if (validHandle)
                {
                    if (stack.waitForUpdate)
                    {
                        tempStageStacks.Add(stack);
                    }
                }
                else
                {
                    List<YieldStack> stageStacks;
                    if (TryGetOtherStageFromYieldInstruction(stack.yieldInstruction, out stageStacks))
                    {
                        lock (stageStacks)
                        {
                            // TODO: indexed set?
                            stageStacks.Remove(stack);
                        }
                    }
                    pool.Release(stack);
                }

                return !validHandle;
            }

            public override void MainUpdate()
            {
                lock (workingStacks)
                {
                    if (workingStacks.Count > 0)
                    {
                        workingStacks.RemoveAll(removeAllInvalidYieldStackPredicate);
                    }
                }

                if (tempStageStacks.Count > 0)
                {
                    foreach (var stack in tempStageStacks)
                    {
                        if (!stack.MoveNext())
                        {
                            var handle = stack.handle;
                            lock (handle)
                            {
                                if (!handle.isDone && handle.stack == stack)
                                {
                                    handle.isDone = true;
                                    handle.stack = null;
                                }
                            }
                            continue;
                        }

                        List<YieldStack> stageStacks;
                        if (TryGetOtherStageFromYieldInstruction(stack.yieldInstruction, out stageStacks))
                        {
                            lock (stageStacks)
                            {
                                // join other stage
                                stack.waitForUpdate = false;
                                stageStacks.Add(stack);
                            }
                        }
                    }

                    tempStageStacks.Clear();
                }

                ExecuteDelayAction(ref delayUpdateCall);
            }

            public override void LateUpdate() { PerformOtherStaget(lateUpdateStageStacks); ExecuteDelayAction(ref delayLateUpdateCall); }

            public override void FixedeUpdate() { PerformOtherStaget(fixedUpdateStageStacks); ExecuteDelayAction(ref delayFixedUpdateCall); }

            public override void EndOfFrameUpdate() { PerformOtherStaget(endOfFrameStageStacks); ExecuteDelayAction(ref delayEndOfFrameCall); }

            private void PerformOtherStaget(List<YieldStack> stacks)
            {
                lock (stacks)
                {
                    if (stacks.Count == 0) { return; }
                    tempStageStacks.AddRange(stacks);
                    stacks.Clear();
                }

                foreach (var stack in tempStageStacks)
                {
                    var handle = stack.handle;
                    lock (handle)
                    {
                        if (handle.isDone || handle.stack != stack)
                        {
                            // job stopped manually (after regular update)
                            // just skip here and wait for being released in next regular update
                            continue;
                        }
                    }

                    if (!stack.MoveNext())
                    {
                        // job is done
                        // mark as done and wait for being released in next regular update
                        lock (handle)
                        {
                            if (!handle.isDone && handle.stack == stack)
                            {
                                handle.isDone = true;
                                handle.stack = null;
                            }
                        }
                        continue;
                    }

                    List<YieldStack> stageStacks;
                    if (TryGetOtherStageFromYieldInstruction(stack.yieldInstruction, out stageStacks))
                    {
                        if (stageStacks != stacks)
                        {
                            lock (stageStacks)
                            {
                                // move to other stage
                                stageStacks.Add(stack);
                            }
                        }
                        else
                        {
                            lock (stack)
                            {
                                // re-join current stage
                                stacks.Add(stack);
                            }
                        }

                        continue;
                    }

                    // back to regular update stage
                    stack.waitForUpdate = true;
                }

                tempStageStacks.Clear();
            }

            public override void StopAllCoroutine()
            {
                lock (workingStacks)
                {
                    foreach (var stack in workingStacks)
                    {
                        var handle = stack.handle;
                        lock (handle)
                        {
                            if (!handle.isDone)
                            {
                                handle.isDone = true;
                                handle.stack = null;
                            }
                        }
                    }
                }
            }

            private bool TryGetOtherStageFromYieldInstruction(YieldInstruction yieldInst, out List<YieldStack> stageStacks)
            {
                if (yieldInst != null)
                {
                    if (yieldInst is WaitForLateUpdate)
                    {
                        stageStacks = lateUpdateStageStacks;
                        return true;
                    }
                    else if (yieldInst is WaitForEndOfFrame)
                    {
                        stageStacks = endOfFrameStageStacks;
                        return true;
                    }
                    else if (yieldInst is WaitForFixedUpdate)
                    {
                        stageStacks = fixedUpdateStageStacks;
                        return true;
                    }
                    else if (yieldInst is WaitForSeconds)
                    {
                        Debug.LogWarning("WaitForSeconds is not supported, use WaitForSecondsScaledTime instead");
                    }
                    else
                    {
                        Debug.LogWarning("Got unknown YieldInstruction. " + yieldInst.GetType().Name);
                    }
                }

                stageStacks = null;
                return false;
            }
        }
    }
}