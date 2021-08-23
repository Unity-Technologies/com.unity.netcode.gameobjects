using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Runtime tests to test the network time system with the Unity player loop.
    /// </summary>
    public class NetworkTimeSystemTests
    {
        private MonoBehaviourTest<PlayerLoopTimeTestComponent> m_MonoBehaviourTest; // cache for teardown

        [SetUp]
        public void Setup()
        {
            // Create, instantiate, and host
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out _));
        }

        /// <summary>
        /// Tests whether time is accessible and has correct values inside Update/FixedUpdate.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator PlayerLoopTimeTest()
        {
            m_MonoBehaviourTest = new MonoBehaviourTest<PlayerLoopTimeTestComponent>();

            yield return m_MonoBehaviourTest;
        }

        /// <summary>
        /// Tests whether the time system invokes the correct amount of ticks over a period of time.
        /// Note we cannot test against Time.Time directly because of floating point precision. Our time is more precise leading to different results.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator CorrectAmountTicksTest()
        {
            var tickSystem = NetworkManager.Singleton.NetworkTickSystem;
            var delta = tickSystem.LocalTime.FixedDeltaTime;

            while (tickSystem.LocalTime.Time < 3f)
            {
                yield return null;
                Assert.AreEqual(Mathf.FloorToInt((tickSystem.LocalTime.TimeAsFloat / delta)), NetworkManager.Singleton.LocalTime.Tick);
                Assert.AreEqual(Mathf.FloorToInt((tickSystem.ServerTime.TimeAsFloat / delta)), NetworkManager.Singleton.ServerTime.Tick);
                Assert.True(Mathf.Approximately((float)NetworkManager.Singleton.LocalTime.Time, (float)NetworkManager.Singleton.ServerTime.Time));
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();

            if (m_MonoBehaviourTest != null)
            {
                Object.DestroyImmediate(m_MonoBehaviourTest.gameObject);
            }
        }

    }

    public class PlayerLoopTimeTestComponent : MonoBehaviour, IMonoBehaviourTest
    {
        public const int Passes = 100;

        private int m_UpdatePasses = 0;

        private int m_LastFixedUpdateTick = 0;
        private int m_TickOffset = -1;

        private NetworkTime m_LocalTimePreviousUpdate;
        private NetworkTime m_ServerTimePreviousUpdate;
        private NetworkTime m_LocalTimePreviousFixedUpdate;

        public void Start()
        {
            // Run fixed update at same rate as network tick
            Time.fixedDeltaTime = NetworkManager.Singleton.LocalTime.FixedDeltaTime;

            // Uncap fixed time else we might skip fixed updates
            Time.maximumDeltaTime = float.MaxValue;
        }

        public void Update()
        {
            // This must run first else it wont run if there is an exception
            m_UpdatePasses++;

            var localTime = NetworkManager.Singleton.LocalTime;
            var serverTime = NetworkManager.Singleton.ServerTime;

            // time should have advanced on the host/server
            Assert.True(m_LocalTimePreviousUpdate.Time < localTime.Time);
            Assert.True(m_ServerTimePreviousUpdate.Time < serverTime.Time);

            // time should be further then last fixed step in update
            Assert.True(m_LocalTimePreviousFixedUpdate.FixedTime < localTime.Time);

            // we should be in same or further tick then fixed update
            Assert.True(m_LocalTimePreviousFixedUpdate.Tick <= localTime.Tick);

            // fixed update should result in same amounts of tick as network time
            if (m_TickOffset == -1)
            {
                m_TickOffset = serverTime.Tick - m_LastFixedUpdateTick;
            }
            else
            {
                // offset of  1 is ok, this happens due to different tick duration offsets
                Assert.True(Mathf.Abs(serverTime.Tick - m_TickOffset - m_LastFixedUpdateTick) <= 1);
            }

            m_LocalTimePreviousUpdate = localTime;
        }

        public void FixedUpdate()
        {
            var time = NetworkManager.Singleton.LocalTime;

            m_LocalTimePreviousFixedUpdate = time;

            Assert.AreEqual(Time.fixedDeltaTime, time.FixedDeltaTime);
            Assert.True(Mathf.Approximately((float)NetworkManager.Singleton.LocalTime.Time, (float)NetworkManager.Singleton.ServerTime.Time));

            m_LastFixedUpdateTick++;
        }

        public bool IsTestFinished => m_UpdatePasses >= Passes;
    }

}
