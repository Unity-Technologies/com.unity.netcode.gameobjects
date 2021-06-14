using System.Collections;
using MLAPI.Timing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Timing
{

    /// <summary>
    /// Runtime tests to test the network time system with the Unity player loop.
    /// </summary>
    public class NetworkTimeSystemTests
    {
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
            var test = new MonoBehaviourTest<PlayerLoopTimeTestComponent>();

            yield return test;
        }

        /// <summary>
        /// Tests whether the time system invokes the correct amount of ticks over a period of time.
        /// Note we cannot test against Time.Time directly because of floating point precision. Our time is more precise leading to different results.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator CorrectAmountTicksTest()
        {
            var timeSystem = NetworkManager.Singleton.NetworkTimeSystem;
            var delta = timeSystem.PredictedTime.FixedDeltaTime;

            while (timeSystem.PredictedTime.Time < 3f)
            {
                yield return null;
                Assert.AreEqual(Mathf.FloorToInt((timeSystem.PredictedTime.TimeAsFloat / delta)), NetworkManager.Singleton.PredictedTime.Tick );
                Assert.AreEqual(Mathf.FloorToInt((timeSystem.ServerTime.TimeAsFloat / delta)), NetworkManager.Singleton.ServerTime.Tick );
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

    }

    public class PlayerLoopTimeTestComponent : MonoBehaviour, IMonoBehaviourTest
    {
        public const int passes = 100;

        private int m_UpdatePasses = 0;

        private int m_LastFixedUpdateTick = 0;
        private int m_TickOffset = -1;

        private NetworkTime m_PredictedTimePreviousUpdate;
        private NetworkTime m_ServerTimePreviousUpdate;
        private NetworkTime m_PredictedTimePreviousFixedUpdate;

        public void Start()
        {
            // Run fixed update at same rate as network tick
            Time.fixedDeltaTime = NetworkManager.Singleton.PredictedTime.FixedDeltaTime;

            // Uncap fixed time else we might skip fixed updates
            Time.maximumDeltaTime = float.MaxValue;
        }

        public void Update()
        {
            // This must run first else it wont run if there is an exception
            m_UpdatePasses++;

            var predictedTime = NetworkManager.Singleton.PredictedTime;
            var serverTime = NetworkManager.Singleton.ServerTime;

            // time should have advanced on the host/server
            Assert.True(m_PredictedTimePreviousUpdate.Time < predictedTime.Time);
            Assert.True(m_ServerTimePreviousUpdate.Time < serverTime.Time);

            // time should be further then last fixed step in update
            Assert.True(m_PredictedTimePreviousFixedUpdate.FixedTime < predictedTime.Time );

            // we should be in same or further tick then fixed update
            Assert.True(m_PredictedTimePreviousFixedUpdate.Tick <= predictedTime.Tick);

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


            m_PredictedTimePreviousUpdate = predictedTime;
        }

        public void FixedUpdate()
        {
            var time = NetworkManager.Singleton.PredictedTime;

            m_PredictedTimePreviousFixedUpdate = time;

            Assert.AreEqual(Time.fixedDeltaTime, time.FixedDeltaTime);

            m_LastFixedUpdateTick++;
        }

        public bool IsTestFinished => m_UpdatePasses >= passes;
    }

}
