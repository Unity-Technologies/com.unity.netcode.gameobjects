using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace MLAPI.RuntimeTests
{
    public class TickSystemTests: IDisposable
    {
        private NetworkTickSystem m_TickSystem = null;
        private float m_TestDuration = 3.0f;
        private float m_SleepInterval = 0.001f;
        private float m_TickInterval = 0.010f;

        public void Dispose()
        {
            m_TickSystem.Dispose();
            m_TickSystem = null;
            NetworkUpdateLoop.UnregisterLoopSystems();
        }

        [UnityTest]
        public IEnumerator VerifyTickSystem()
        {
            m_TickSystem = new NetworkTickSystem(m_TickInterval);

            var startTick = m_TickSystem.GetTick();
            var startTime = Time.unscaledTime;

            var lastTick = startTick;
            do
            {
                var currentTick = m_TickSystem.GetTick();
                Assert.IsTrue(currentTick >= lastTick); // check monotonicity of ticks
                lastTick = currentTick;

                yield return new WaitForSeconds(m_SleepInterval);
            } while (Time.unscaledTime - startTime <= m_TestDuration);

            var endTick = m_TickSystem.GetTick();
            var endTime = Time.unscaledTime;

            var elapsedTicks = endTick - startTick;
            var elapsedTime = endTime - startTime;

            var elapsedTicksExpected = (int)(elapsedTime / m_TickInterval);
            Assert.Less(Math.Abs(elapsedTicksExpected - elapsedTicks), 2); // +/- 1 is OK
        }
    }
}
