//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========
#pragma warning disable 0649
#pragma warning disable 0168

using System.Collections;
using UnityEngine;

namespace HTC.UnityPlugin.LiteCoroutineSystem.Example
{
    public class LiteCoroutineExample : MonoBehaviour
    {
        private LiteCoroutine m_handle;

        private IEnumerator Start()
        {
            // Start coroutine using static function
            Debug.Log("### Start Coroutine with Static Function");
            LiteCoroutine coroutineHandle1 = LiteCoroutine.StartCoroutine(PromptCoroutine());
            yield return coroutineHandle1;

            // Stop/Interrupt coroutine
            Debug.Log("### Stop Coroutine");
            LiteCoroutine coroutineHandle2 = LiteCoroutine.StartCoroutine(PromptCoroutine());
            yield return NewWaitInstruction(2.5f);
            coroutineHandle2.Stop();
            yield return coroutineHandle2;

            // IsNullOrDone works on null handle
            if (m_handle.IsNullOrDone())
            {
                // Create & Assign new handle since m_handle is null
                Debug.Log("### Initiate Coroutine Handle and Start New Coroutine");
                LiteCoroutine.StartCoroutine(ref m_handle, PromptCoroutine());
                yield return m_handle;

                // Reuse m_handle since m_handle is no null
                Debug.Log("### Reuse Coroutine Handle and Start New Coroutine");
                LiteCoroutine.StartCoroutine(ref m_handle, PromptCoroutine());

                // Stop & Restart using existing coroutine handle
                yield return NewWaitInstruction(2.5f);
                Debug.Log("### Stop and Restart Coroutine Using Existing Handle");
                LiteCoroutine.StartCoroutine(ref m_handle, PromptCoroutine());
            }

            // Manually wait for coroutine handle finished
            while (!m_handle.IsDone)
            {
                yield return null;
            }
            Debug.Log("### Manually Wait for Coroutine Handle to be Finished");
        }

        private IEnumerator PromptCoroutine(int promptCount = 3, float promptInterval = 1f)
        {
            var wait = NewWaitInstruction(promptInterval);

            for (int i = 0, imax = promptCount; i < imax; ++i)
            {
                yield return wait;
                Debug.Log("count:" + i);
            }

            Debug.Log("done");
        }

        private IEnumerator NewWaitInstruction(float seconds)
        {
#if UNITY_5_4_OR_NEWER
            return new WaitForSecondsRealtime(seconds);
#else
            return new WaitForSecondsUnscaledTime(seconds);
#endif
        }
    }
}