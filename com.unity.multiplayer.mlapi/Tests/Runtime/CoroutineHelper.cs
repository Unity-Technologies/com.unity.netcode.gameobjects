using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.RuntimeTests
{
    public class CoroutineResultWrapper<T>
    {
        public T Result;
    }

    public static class CoroutineHelper
    {
        static List<CoroutineRunner> s_CoroutineRunners = new List<CoroutineRunner>();

        /// <summary>
        /// Runs a IEnumerator as a Coroutine on a dummy GameObject. Used to get exceptions coming from the coroutine
        /// </summary>
        /// <param name="name">The name of the coroutine (for debugging)</param>
        /// <param name="enumerator">The IEnumerator to run</param>
        public static Coroutine Run(IEnumerator enumerator, string name = "Unknown")
        {
            var coroutineRunner = new GameObject($"Coroutine: {name}").AddComponent<CoroutineRunner>();
            s_CoroutineRunners.Add(coroutineRunner);
            return coroutineRunner.StartCoroutine(enumerator);
        }

        public static void Cleanup()
        {
            // Destroy the temporary GameObjects used to run co-routines
            foreach (var coroutineRunner in s_CoroutineRunners)
            {
                if (coroutineRunner)
                {
                    UnityEngine.Object.Destroy(coroutineRunner);
                }
            }
            s_CoroutineRunners.Clear();
        }

        public static IEnumerator WaitOneFrame() => WaitNumFrames(1);
        public static IEnumerator WaitNumFrames(int numFrames)
        {
            Debug.Log($"Waiting {numFrames} frames");

            int currentFrame = Time.frameCount;
            int targetFrame = currentFrame + numFrames;
            while (Time.frameCount < targetFrame)
            {
                Debug.Log($"Waited a frame. Current: {Time.frameCount}, Target: {targetFrame}");
                yield return null;
            }

            Debug.Log("Finished waiting.");
        }

        /// <summary>
        /// Waits for a condition to be met
        /// </summary>
        /// <param name="condition">The predicate to wait for</param>
        /// <param name="result">The result. If null, it will fail on timeout</param>
        /// <param name="maxFramesBeforeTimeout">The max frames to wait for before timing out</param>
        public static IEnumerator WaitUntilConditionWithTimeout(Func<bool> condition, CoroutineResultWrapper<bool> result = null, int maxFramesBeforeTimeout = 64, Action onTimeout = null)
        {
            if (condition == null)
            {
                throw new ArgumentNullException("Condition cannot be null");
            }

            bool success = false;
            var startFrameNumber = Time.frameCount;
            while (Time.frameCount - startFrameNumber <= maxFramesBeforeTimeout)
            {
                if (condition())
                {
                    success = true;
                    break;
                }

                yield return WaitOneFrame();
            }

            if (result != null)
            {
                result.Result = success;
            }
            else
            {
                Assert.True(success, "PREDICATE CONDITION");
            }
        }
    }
}
