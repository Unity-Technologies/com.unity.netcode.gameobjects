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
        /// On the server local time should always be equal to server time. This test ensures that this is the case.
        /// </summary>
        [Test]
        public void LocalTimeEqualServerTimeTest()
        {
            var steps = TimingTestHelper.GetRandomTimeSteps(100f, 0.01f, 0.1f, 42);

            var serverTimeProvider = new ServerNetworkTimeProvider();
            var serverTime = new NetworkTime(60, 30);
            var localTime = serverTime;
            var startTime = serverTime;

            TimingTestHelper.ApplySteps(serverTimeProvider, steps, ref localTime, ref serverTime, step =>
            {
                Assert.IsTrue(Mathf.Approximately(serverTime.TimeAsFloat, localTime.TimeAsFloat));
            });

            Assert.IsTrue(serverTime.Time > startTime.Time);

        }

        /// <summary>
        /// <see cref="ServerNetworkTimeProvider"/> should throw when trying to use it on a client. This is not supported.
        /// </summary>
        [Test]
        public void InitializeClientFail()
        {
            NetworkTime serverTime = default;
            NetworkTime localTime = default;

            var serverTimeProvider = new ServerNetworkTimeProvider();
            Assert.Throws<InvalidOperationException>(() => { serverTimeProvider.InitializeClient(ref localTime, ref serverTime); });
        }

    }
}


