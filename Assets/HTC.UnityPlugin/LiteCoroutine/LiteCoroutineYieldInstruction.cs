//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
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
    public sealed class WaitForSecondsScaledTime : CustomYieldInstruction
    {
        private float timeout;

        public float seconds { get; private set; }

        public WaitForSecondsScaledTime(float seconds) { ResetTimer(seconds); }

        public override bool keepWaiting { get { return Time.time < timeout; } }

        public WaitForSecondsScaledTime ResetTimer() { timeout = Time.time + seconds; return this; }
        public WaitForSecondsScaledTime ResetTimer(float seconds) { this.seconds = seconds; return ResetTimer(); }
    }

    public sealed class WaitForTicks : CustomYieldInstruction
    {
        private long timeout;

        private static long now { get { return DateTime.UtcNow.Ticks; } }

        public long ticks { get; private set; }
        public TimeSpan duration { get { return new TimeSpan(ticks); } }

        public override bool keepWaiting { get { return now < timeout; } }

        public WaitForTicks(long ticks) { ResetTimer(ticks); }
        public WaitForTicks(TimeSpan duration) : this(duration.Ticks) { }

        public static WaitForTicks Seconds(long value) { return new WaitForTicks(value > 0L ? value * TimeSpan.TicksPerSecond : 0L); }
        public static WaitForTicks MiliSeconds(long value) { return new WaitForTicks(value > 0L ? value * TimeSpan.TicksPerMillisecond : 0L); }

        public WaitForTicks ResetTimer() { timeout = now + ticks; return this; }
        public WaitForTicks ResetTimer(long ticks) { this.ticks = ticks; return ResetTimer(); }
        public WaitForTicks ResetTimer(TimeSpan duration) { return ResetTimer(duration.Ticks); }
    }
}