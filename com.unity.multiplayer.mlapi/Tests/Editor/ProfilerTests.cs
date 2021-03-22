using System;
using System.Collections.Generic;
using MLAPI.Profiling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

    public class TestHasProfilable : IHasProfilableTransport
    {
        internal static class ProfilerConstants
        {
            public const string NetworkTestData = nameof(NetworkTestData);
        }

        private TestTransport m_Transport;

        public ITransportProfilerData GetTransport()
        {
            return m_Transport;
        }

        public void Initialize(bool useNullTransport)
        {
            m_Transport = useNullTransport ? null : new TestTransport();
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
        }
    }

    public class NoTickDataException : Exception
    {
    }

    public class ProfilerTests
    {
        private static void BreakDownTestProfiler(bool useNullTransport)
        {
            if (useNullTransport)
            {
                ProfilerNotifier.OnPerformanceDataEvent -= TestProfilerOnPerformanceDataEventNoTransport;
            }
            else
            {
                ProfilerNotifier.OnPerformanceDataEvent -= TestProfilerOnPerformanceDataEventNormal;
            }

            ProfilerNotifier.OnNoTickDataEvent -= TestProfilerNotifierOnOnNoTickDataEvent;
        }

        private static TestHasProfilable SetupTestProfiler(bool useNullTransport)
        {
            ProfilerNotifier.OnNoTickDataEvent += TestProfilerNotifierOnOnNoTickDataEvent;
            if (useNullTransport)
            {
                ProfilerNotifier.OnPerformanceDataEvent += TestProfilerOnPerformanceDataEventNoTransport;
            }
            else
            {
                ProfilerNotifier.OnPerformanceDataEvent += TestProfilerOnPerformanceDataEventNormal;
            }

            EditorApplication.UnlockReloadAssemblies();
            var testProfiler = new TestHasProfilable();
            testProfiler.Initialize(useNullTransport);
            return testProfiler;
        }

        [Test]
        public void TestNormalRegisterAndNotifyFlowNull()
        {
            const bool useNullTransport = true;
            TestHasProfilable testProfiler = SetupTestProfiler(useNullTransport);

            TestHasProfilable.ProfilerBeginTick();
            testProfiler.Send();
            TestHasProfilable.NotifyProfilerListeners();

            BreakDownTestProfiler(useNullTransport);
        }

        [Test]
        public void TestNormalRegisterAndNotifyFlow()
        {
            const bool useNullTransport = false;
            TestHasProfilable testProfiler = SetupTestProfiler(useNullTransport);

            TestHasProfilable.ProfilerBeginTick();
            testProfiler.Send();
            TestHasProfilable.NotifyProfilerListeners();

            BreakDownTestProfiler(useNullTransport);
        }

        [Test]
        public void TestDroppedRegisterAndNotifyFlow()
        {
            const bool useNullTransport = false;
            TestHasProfilable testProfiler = SetupTestProfiler(useNullTransport);

            TestHasProfilable.ProfilerBeginTick();
            testProfiler.Send();
            TestHasProfilable.NotifyProfilerListeners();

            // Capturing data after notifying listeners is bad
            Assert.Catch<NoTickDataException>(() =>
            {
                testProfiler.Send();
            });
            Assert.Catch<NoTickDataException>(() =>
            {
                testProfiler.Send();
            });
            TestHasProfilable.ProfilerBeginTick();

            BreakDownTestProfiler(useNullTransport);
        }


        [Test]
        public void TestProperMatchRegisterAndNotifyFlow()
        {
            const bool useNullTransport = false;
            TestHasProfilable testProfiler = SetupTestProfiler(useNullTransport);

            TestHasProfilable.NotifyProfilerListeners();
            TestHasProfilable.ProfilerBeginTick();
            testProfiler.Send();
            TestHasProfilable.NotifyProfilerListeners();

            BreakDownTestProfiler(useNullTransport);
        }

        private static void TestProfilerOnPerformanceDataEventNormal(PerformanceTickData profilerData)
        {
            Assert.IsTrue(profilerData.HasData(TestHasProfilable.ProfilerConstants.NetworkTestData));
            Assert.IsTrue(profilerData.HasData(TestTransport.ProfilerConstants.TransportTestData));
        }

        private static void TestProfilerOnPerformanceDataEventNoTransport(PerformanceTickData profilerData)
        {
            Assert.IsTrue(profilerData.HasData(TestHasProfilable.ProfilerConstants.NetworkTestData));
            Assert.IsFalse(profilerData.HasData(TestTransport.ProfilerConstants.TransportTestData));
        }

        private static void TestProfilerNotifierOnOnNoTickDataEvent()
        {
            throw new NoTickDataException();
        }
    }
}
