//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using UnityEngine;

namespace HTC.UnityPlugin.LiteCoroutineSystem
{
    public sealed class WaitForLateUpdate : YieldInstruction { }

    public sealed class WaitForAsyncOperation : CustomYieldInstruction
    {
        public AsyncOperation asyncOp { get; set; }

        public WaitForAsyncOperation(AsyncOperation asyncOp) { this.asyncOp = asyncOp; }

        public override bool keepWaiting { get { return asyncOp != null && !asyncOp.isDone; } }
    }

    // Creates a yield instruction to wait for a given number of seconds using scaled time
    public sealed class WaitForSecondsScaledTime : IEnumerator
    {
        private float waitUntilTime = -1;

        public float waitTime { get; set; }

        public object Current { get { return null; } }

        public WaitForSecondsScaledTime(float time) { waitTime = time; }

        public bool MoveNext()
        {
            var now = Time.time;
            if (waitUntilTime < 0) { waitUntilTime = now + waitTime; }
            var wait = now < waitUntilTime;
            if (!wait) { Reset(); } // Reset so it can be reused.
            return wait;
        }

        public void Reset() { waitUntilTime = -1; }

        public WaitForSecondsScaledTime SetWaitTime(float time) { waitTime = time; return this; }
    }

    public sealed class WaitForSecondsUnscaledTime : IEnumerator
    {
        private float waitUntilTime = -1;

        public float waitTime { get; set; }

        public object Current { get { return null; } }

        public WaitForSecondsUnscaledTime(float time) { waitTime = time; }

        public bool MoveNext()
        {
            var now = Time.unscaledTime;
            if (waitUntilTime < 0) { waitUntilTime = now + waitTime; }
            var wait = now < waitUntilTime;
            if (!wait) { Reset(); } // Reset so it can be reused.
            return wait;
        }

        public void Reset() { waitUntilTime = -1; }

        public WaitForSecondsUnscaledTime SetWaitTime(float time) { waitTime = time; return this; }
    }

    /// <summary>
    /// When yield returned in main thread coroutine, it works normally as other Wait instruction, skip frames for a while.
    /// When yield returned in LiteTask background thread, it will trigger Thread.Sleep for a while and continue the enumerating without restarting a new thread.
    /// </summary>
    public sealed class WaitForTicks : IEnumerator
    {
        private long waitUntilTicks = -1;

        public long waitTicks { get; set; }

        public TimeSpan waitTime { get { return new TimeSpan(waitTicks); } set { waitTicks = value.Ticks; } }

        public object Current { get { return null; } }

        public WaitForTicks(long ticks) { waitTicks = ticks; }
        public WaitForTicks(TimeSpan time) : this(time.Ticks) { }
        public static WaitForTicks Seconds(float value) { return new WaitForTicks(TicksFromSeconds(value)); }
        public static WaitForTicks MiliSeconds(float value) { return new WaitForTicks(TicksFromMiliSeconds(value)); }

        public bool MoveNext()
        {
            var now = DateTime.UtcNow.Ticks;
            if (waitUntilTicks < 0) { waitUntilTicks = now + waitTicks; }
            var wait = now < waitUntilTicks;
            if (!wait) { Reset(); } // Reset so it can be reused.
            return wait;
        }

        public void Reset() { waitUntilTicks = -1; }

        public WaitForTicks SetWaitTicks(long ticks) { waitTicks = ticks; return this; }

        public WaitForTicks SetWaitTime(TimeSpan time) { waitTime = time; return this; }

        public WaitForTicks SetWaitSeconds(float value) { waitTicks = TicksFromSeconds(value); return this; }

        public WaitForTicks SetWaitMiliSeconds(float value) { waitTicks = TicksFromMiliSeconds(value); return this; }

        private static long TicksFromSeconds(float value) { return (long)(value * TimeSpan.TicksPerSecond); }

        private static long TicksFromMiliSeconds(float value) { return (long)(value * TimeSpan.TicksPerMillisecond); }
    }
}