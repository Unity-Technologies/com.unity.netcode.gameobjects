using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.Timing;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.EditorTests.Timing
{
    public class ServerNetworkTimeProviderTests
    {

        /// <summary>
        /// On the server predicted time should always be equal to server time. This test ensures that this is the case.
        /// </summary>
        [Test]
        public void PredictedTimeEqualServerTimeTest()
        {
            var steps = TimingTestHelper.GetRandomTimeSteps(100f, 0.01f, 0.1f, 42);

            var serverTimeProvider = new ServerNetworkTimeProvider();
            var serverTime = new NetworkTime(60, 30);
            var predictedTime = serverTime;
            var startTime = serverTime;

            TimingTestHelper.ApplySteps(serverTimeProvider, steps, ref predictedTime, ref serverTime, step =>
            {
                Assert.IsTrue(Mathf.Approximately(serverTime.Time, predictedTime.Time));
            } );

            Assert.IsTrue(serverTime.Time > startTime.Time);

        }

        /// <summary>
        /// <see cref="ServerNetworkTimeProvider"/> should throw when trying to use it on a client. This is not supported.
        /// </summary>
        [Test]
        public void InitializeClientFail()
        {
            NetworkTime serverTime = default;
            NetworkTime predictedTime = default;

            var serverTimeProvider = new ServerNetworkTimeProvider();
            Assert.Throws<InvalidOperationException>(() => { serverTimeProvider.InitializeClient(ref predictedTime, ref serverTime); });
        }

    }
}


