using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class TestTransport : ITransportProfilerData
    {
        internal static class ProfilerConstants
        {
            public const string TransportTestData = nameof(TransportTestData);
        }

        private static ProfilingDataStore s_TransportProfilerData = new ProfilingDataStore();

        public void BeginNewTick()
        {
            s_TransportProfilerData.Clear();
        }

        public IReadOnlyDictionary<string, int> GetTransportProfilerData()
        {
            return s_TransportProfilerData.GetReadonly();
        }

        public void Send(string testMessage)
        {
            PerformanceDataManager.Increment(ProfilerConstants.TransportTestData);
        }
    }

    public class TestProfiler : IProfilableTransportProvider
    {
        internal static class ProfilerConstants
        {
            public const string NetworkTestData = nameof(NetworkTestData);
        }

        private TestTransport m_Transport;
        private bool m_HasSentAnyData;

        public ITransportProfilerData Transport => m_Transport;
        public bool HasSentAnyData => m_HasSentAnyData;

        public void Initialize(bool useNullTransport)
        {
            m_Transport = useNullTransport ? null : new TestTransport();
            m_HasSentAnyData = false;
            ProfilerNotifier.Initialize(this);
        }

        public static void ProfilerBeginTick()
        {
            ProfilerNotifier.ProfilerBeginTick();
        }

        public static void NotifyProfilerListeners()
        {
            ProfilerNotifier.NotifyProfilerListeners();
        }

        public void Send()
        {
            ProfilerNotifier.Increment(ProfilerConstants.NetworkTestData);
            m_Transport?.Send("testMessage");
            m_HasSentAnyData = true;
        }
    }

    public class NoTickDataException : Exception
    {
    }

    public class ProfilerTests
    {
        private static TestProfiler SetupTestProfiler(bool useNullTransport)
        {
            var testProfiler = new TestProfiler();
            testProfiler.Initialize(useNullTransport);
            return testProfiler;
        }

        private static void RegisterStaticAsserts(bool useNullTransport)
        {
            ProfilerNotifier.OnNoTickDataEvent += RaiseExceptionNoTickDataEvent;
            if (useNullTransport)
            {
                ProfilerNotifier.OnPerformanceDataEvent += AssertNetworkDataExists;
            }
            else
            {
                ProfilerNotifier.OnPerformanceDataEvent += AssertNetworkAndTransportDataExists;
            }
        }

        private static void AssertNetworkAndTransportDataExists(PerformanceTickData profilerData)
        {
            Assert.IsTrue(profilerData.HasData(TestProfiler.ProfilerConstants.NetworkTestData));
            Assert.IsTrue(profilerData.HasData(TestTransport.ProfilerConstants.TransportTestData));
        }

        private static void AssertNetworkDataExists(PerformanceTickData profilerData)
        {
            Assert.IsTrue(profilerData.HasData(TestProfiler.ProfilerConstants.NetworkTestData));
            Assert.IsFalse(profilerData.HasData(TestTransport.ProfilerConstants.TransportTestData));
        }

        private static void RaiseExceptionNoTickDataEvent()
        {
            throw new NoTickDataException();
        }

        [TearDown]
        public void TearDown()
        {
            ProfilerNotifier.OnPerformanceDataEvent -= AssertNetworkAndTransportDataExists;
            ProfilerNotifier.OnPerformanceDataEvent -= AssertNetworkDataExists;
            ProfilerNotifier.OnNoTickDataEvent -= RaiseExceptionNoTickDataEvent;
        }

        [Test]
        public void TestSentNoData()
        {
            const bool useNullTransport = true;
            TestProfiler testProfiler = SetupTestProfiler(useNullTransport);

            TestProfiler.ProfilerBeginTick();
            TestProfiler.NotifyProfilerListeners();

            Assert.False(testProfiler.HasSentAnyData);
        }

        [Test]
        public void TestNormalRegisterAndNotifyFlow_NullTransport()
        {
            const bool useNullTransport = true;
            TestProfiler testProfiler = SetupTestProfiler(useNullTransport);

            RegisterStaticAsserts(useNullTransport);

            TestProfiler.ProfilerBeginTick();
            testProfiler.Send();
            TestProfiler.NotifyProfilerListeners();

            Assert.IsTrue(testProfiler.HasSentAnyData);
        }

        [Test]
        public void TestNormalRegisterAndNotifyFlow()
        {
            const bool useNullTransport = false;
            TestProfiler testProfiler = SetupTestProfiler(useNullTransport);

            RegisterStaticAsserts(useNullTransport);

            TestProfiler.ProfilerBeginTick();
            testProfiler.Send();
            TestProfiler.NotifyProfilerListeners();

            Assert.IsTrue(testProfiler.HasSentAnyData);
        }

        [Test]
        public void TestDroppedRegisterAndNotifyFlow()
        {
            const bool useNullTransport = false;
            TestProfiler testProfiler = SetupTestProfiler(useNullTransport);

            RegisterStaticAsserts(useNullTransport);

            TestProfiler.ProfilerBeginTick();
            testProfiler.Send();
            TestProfiler.NotifyProfilerListeners();

            // Capturing data after notifying listeners is bad
            Assert.Catch<NoTickDataException>(() =>
            {
                testProfiler.Send();
            });

            Assert.IsTrue(testProfiler.HasSentAnyData);
        }

        [Test]
        public void TestProperMatchRegisterAndNotifyFlow()
        {
            const bool useNullTransport = false;
            TestProfiler testProfiler = SetupTestProfiler(useNullTransport);

            RegisterStaticAsserts(useNullTransport);

            TestProfiler.NotifyProfilerListeners();
            TestProfiler.ProfilerBeginTick();
            testProfiler.Send();
            TestProfiler.NotifyProfilerListeners();

            Assert.IsTrue(testProfiler.HasSentAnyData);
        }
    }
}
