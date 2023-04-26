//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========
#pragma warning disable 0649
#pragma warning disable 0168

using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace HTC.UnityPlugin.LiteCoroutineSystem.Example
{
    public class LiteTaskExample : MonoBehaviour
    {
        private static Thread mainThread;

        private IEnumerator Start()
        {
            mainThread = Thread.CurrentThread;

            // LiteTask allow you to run heavy script in background thread without blocking Unity main thread
            Debug.Log("### LiteTask in Background Thread");
            var task = new LiteTask(HeavyTask());
            StartCoroutine(task);
            yield return MainThreadWaiting(task);


            // LiteTask also works with LiteCoroutine as well
            Debug.Log("### LiteTask in Background Thread with LiteCoroutine");
            task = new LiteTask(SleepTask());
            var handle = LiteCoroutine.StartCoroutine(task);
            yield return MainThreadWaiting(task);


            // Stop/Cancel LiteTask
            Debug.Log("### Cancelling LiteTask");
            task = new LiteTask(HeavyTask());
            LiteCoroutine.StartCoroutine(ref handle, task);
            yield return new WaitForSecondsUnscaledTime(2.5f);
            task.Cancel();
            yield return task.Wait();
            Debug.Log("Background task is cancelled");


            // Restart & Reuse LiteTask
            Debug.Log("### Restarting LiteTask");
            task = new LiteTask(HeavyTask());
            LiteCoroutine.StartCoroutine(ref handle, task);
            yield return new WaitForSecondsUnscaledTime(1.5f);
            try
            {
                task.RestartTask(HeavyTask());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.Log("Can't restart new task when task isn't done yet!");
            }
            yield return task.Wait();
            Debug.Log("Must restart new task after task is done...");
            yield return LiteCoroutine.StartCoroutine(ref handle, task.RestartTask(SleepTask()));


            // Exception in background thread
            Debug.Log("### Invalid Operation in Background Thread");
            yield return LiteCoroutine.StartCoroutine(ref handle, task.RestartTask(InvalidOperationInBackground()));


            // Jump between Main & Background thread
            Debug.Log("### Jump Between Main & Background Thread");
            yield return LiteCoroutine.StartCoroutine(ref handle, task.RestartTask(JumpToMainThread()));


            // Nested background task
            Debug.Log("### Nested Background Task");
            yield return LiteCoroutine.StartCoroutine(ref handle, task.RestartTask(NestedTasks()));

            // tobackground can interrupt
            // jump only works in LiteTask
            // nested background task
            // pool
        }

        private IEnumerator MainThreadWaiting(LiteTask task, float promptInterval = 0.5f)
        {
            var nextPromptTime = 0f;
            do
            {
                var now = Time.realtimeSinceStartup;
                if (now > nextPromptTime)
                {
                    nextPromptTime = now + promptInterval;
                    Debug.Log("Main thread waiting...");
                }
                yield return null;
            }
            while (!task.IsDone);
            Debug.Log("Main thread caught task finished");
        }

        private IEnumerator HeavyTask(int loopTime = 3)
        {
            for (int i = loopTime - 1; i >= 0; --i)
            {
                for (int j = 50000000; j > 0; --j)
                {
                    var sqrt = Mathf.Sqrt(j);
                }
                yield return LiteTask.ToBackground; // Any "yield return" will break current task iteration so that LiteTask have a chance to be cancelled
                Debug.Log("Background thread heavy task running... " + (loopTime - i));
            }

            Debug.Log("Background thread heavy task done");
        }

        private IEnumerator SleepTask(int loopTime = 5, float sleepInterval = 0.3f)
        {
            var sleepTime = new TimeSpan((long)(sleepInterval * TimeSpan.TicksPerSecond));
            for (int i = loopTime - 1; i >= 0; --i)
            {
                Thread.Sleep(sleepTime);
                yield return LiteTask.ToBackground; // Any "yield return" will break current task iteration so that LiteTask have a chance to be cancelled
                Debug.Log("Background thread sleep task running... " + (loopTime - i));
            }

            Debug.Log("Background thread sleep task done");
        }

        private IEnumerator InvalidOperationInBackground()
        {
            try { Debug.Log("Application.isPlaying=" + Application.isPlaying); } catch (Exception) { }

            yield return null;

            try { Debug.Log("GameObject.name=" + gameObject.name); } catch (Exception) { }

            yield return null;

            try { Debug.Log("Time.time=" + Time.time); } catch (Exception) { }
        }

        private IEnumerator JumpToMainThread()
        {
            Debug.Log("Sleep 1 sec...");
            Thread.Sleep(1000);

            yield return LiteTask.ToForeground;

            Debug.Log("Jump to main thread");

            try { Debug.Log("Application.isPlaying=" + Application.isPlaying); } catch (Exception) { }

            yield return null;

            try { Debug.Log("GameObject.name=" + gameObject.name); } catch (Exception) { }

            yield return null;

            try { Debug.Log("Time.time=" + Time.time); } catch (Exception) { }

            yield return LiteTask.ToBackground;

            Debug.Log("Jump to background thread");

            Debug.Log("Sleep 1 sec again...");
            Thread.Sleep(1000);
            Debug.Log("Done");
        }

        private IEnumerator NestedTasks()
        {
            IsMainThreadCallPrompt(); // background thread

            yield return IsMainThreadCallPromptEnumerator(); // main thread

            yield return new LiteTask(IsMainThreadCallPromptEnumerator()); // background thread
        }

        private IEnumerator IsMainThreadCallPromptEnumerator()
        {
            IsMainThreadCallPrompt();
            yield break;
        }

        private void IsMainThreadCallPrompt()
        {
            if (IsMainThreadCall()) { Debug.Log("Called from main thread"); }
            else { Debug.Log("Called from background thread"); }
        }

        private bool IsMainThreadCall()
        {
            return mainThread == Thread.CurrentThread;
        }
    }
}