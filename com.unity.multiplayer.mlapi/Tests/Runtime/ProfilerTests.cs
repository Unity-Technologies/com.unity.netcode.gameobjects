using System;
using System.Collections.Generic;
using MLAPI.Logging;
using MLAPI.Profiling;
using NUnit.Framework;

namespace MLAPI.RuntimeTests
{
    public class TestTransport : ITransportProfilerData
    {
        internal static class ProfilerConstants
        {
            public const string TransportTestData = nameof(TransportTestData);
        }

        private static readonly ProfilingDataStore TransportProfilerData = new ProfilingDataStore();

        public void BeginNewTick()
        {
            TransportProfilerData.Clear();
        }

        public IReadOnlyDictionary<string, int> GetTransportProfilerData()
        {
            return TransportProfilerData.GetReadonly();
        }

        public void Send(string testMessage)
        {
            PerformanceDataManager.Increment(ProfilerConstants.TransportTestData);
        }
    }

    public class TestProfiler
    {
        public delegate void PerformanceDataEventHandler(PerformanceTickData profilerData);

        public static event PerformanceDataEventHandler OnPerformanceDataEvent;

        internal static class ProfilerConstants
        {
            public const string NetworkTestData = nameof(NetworkTestData);
        }

        private TestTransport m_Transport;
        private int m_Counter;

        public void Initialize()
        {
            m_Transport = new TestTransport();
            m_Counter = 0;
        }

        public void ProfilerBeginTick()
        {
            PerformanceDataManager.BeginNewTick();
            m_Transport.BeginNewTick();
            m_Counter++;
        }

        public void NotifyProfilerListeners()
        {
            m_Counter--;
            m_Counter = Math.Max(0, m_Counter);

            var data = PerformanceDataManager.GetData();
            var eventHandler = OnPerformanceDataEvent;
            if (eventHandler != null)
            {
                if (data != null)
                {
                    var transportProfilerData = m_Transport.GetTransportProfilerData();

                    PerformanceDataManager.AddTransportData(transportProfilerData);

                    eventHandler.Invoke(data);
                }
                else
                {
                    NetworkLog.LogWarning(
                        "No data available. Did you forget to call PerformanceDataManager.BeginNewTick() first?");
                }
            }
        }

        public void Send(string testMessage)
        {
            if (m_Counter != 1)
            {
                throw new NoTickDataException(m_Counter);
            }
            PerformanceDataManager.Increment(ProfilerConstants.NetworkTestData);
            m_Transport.Send(testMessage);
        }
    }

    public class NoTickDataException : Exception
    {
        public NoTickDataException(int counter)
        : base(counter.ToString())
        {
        }
    }

    public class ProfilerTests
    {
        [SetUp]
        public void Setup()
        {
            TestProfiler.OnPerformanceDataEvent += TestProfilerOnPerformanceDataEventNormal;
        }

        [Test]
        public void TestNormalRegisterAndNotifyFlow()
        {
            var testProfiler = new TestProfiler();
            testProfiler.Initialize();

            testProfiler.ProfilerBeginTick();
            testProfiler.Send("NormalFlow");
            testProfiler.NotifyProfilerListeners();
        }

        [Test]
        public void TestDroppedRegisterAndNotifyFlow()
        {
            var testProfiler = new TestProfiler();
            testProfiler.Initialize();

            testProfiler.ProfilerBeginTick();
            testProfiler.Send("DroppedFlow");
            testProfiler.NotifyProfilerListeners();

            // Capturing data after notifying listeners is bad
            Assert.Catch<NoTickDataException>(() =>
            {
                testProfiler.Send("DroppedFlow");
            });
            Assert.Catch<NoTickDataException>(() =>
            {
                testProfiler.Send("DroppedFlow");
            });
            testProfiler.ProfilerBeginTick();
        }


        [Test]
        public void TestProperMatchRegisterAndNotifyFlow()
        {
            var testProfiler = new TestProfiler();
            testProfiler.Initialize();

            testProfiler.NotifyProfilerListeners();
            testProfiler.ProfilerBeginTick();
            testProfiler.Send("Normal");
            testProfiler.NotifyProfilerListeners();
        }

        private static void TestProfilerOnPerformanceDataEventNormal(PerformanceTickData profilerData)
        {
            Assert.IsTrue(profilerData.HasData(TestProfiler.ProfilerConstants.NetworkTestData));
            Assert.IsTrue(profilerData.HasData(TestTransport.ProfilerConstants.TransportTestData));
        }
    }
}
