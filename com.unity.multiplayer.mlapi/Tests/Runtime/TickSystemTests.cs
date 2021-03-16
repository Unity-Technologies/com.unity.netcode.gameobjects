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
        private float m_TestDuration = 5.0f;
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

            ushort tick0 = m_TickSystem.GetTick();
            ushort lastTick = tick0;
            float t0 = Time.unscaledTime;
            float t1;

            do
            {
                t1 = Time.unscaledTime;
                ushort tick = m_TickSystem.GetTick();

                Assert.IsTrue(tick >= lastTick); // check monotonicity of ticks

                lastTick = tick;
                yield return new WaitForSeconds(m_SleepInterval);
            } while (t1 - t0 <= m_TestDuration);

            int ticks = lastTick - tick0;
            int expectedTicks = (int)(m_TestDuration / m_TickInterval);

            // check overall number of ticks is within one tick of the expected value
            Assert.IsTrue(Math.Abs(expectedTicks - ticks) < 2);
        }
    }
}
