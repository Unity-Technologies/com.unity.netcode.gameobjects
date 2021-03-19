using System;
using System.Collections.Generic;
using MLAPI.Logging;
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

    public static class ProfilerNotifier
    {
        public delegate void PerformanceDataEventHandler(PerformanceTickData profilerData);

        public static event PerformanceDataEventHandler OnPerformanceDataEvent;

        public delegate void NoTickDataHandler();

        public static event NoTickDataHandler OnNoTickDataEvent;

        private static IHasProfilableTransport s_HasProfilableTransport;
        private static bool s_FailsafeCheck;

        public static void Initialize(IHasProfilableTransport hasProfilableNetwork)
        {
            s_HasProfilableTransport = hasProfilableNetwork
                                       ?? throw new ArgumentNullException(
                                           $"{nameof(hasProfilableNetwork)} was not set");
            s_FailsafeCheck = false;
        }

        public static void ProfilerBeginTick()
        {
            PerformanceDataManager.BeginNewTick();
            var transport = s_HasProfilableTransport.GetTransport();
            transport?.BeginNewTick();
            s_FailsafeCheck = true;
        }

        public static void NotifyProfilerListeners()
        {
            if (!s_FailsafeCheck)
                return;

            s_FailsafeCheck = false;

            var data = PerformanceDataManager.GetData();
            var eventHandler = OnPerformanceDataEvent;
            if (eventHandler != null)
            {
                if (data != null)
                {
                    var transport = s_HasProfilableTransport.GetTransport();
                    if (transport != null)
                    {
                        var transportProfilerData = transport.GetTransportProfilerData();

                        PerformanceDataManager.AddTransportData(transportProfilerData);
                    }

                    eventHandler.Invoke(data);
                }
                else
                {
                    NetworkLog.LogWarning(
                        "No data available. Did you forget to call PerformanceDataManager.BeginNewTick() first?");
                }
            }
        }

        public static void Increment(string fieldName, int count = 1)
        {
            if (!s_FailsafeCheck)
            {
                OnNoTickDataEvent?.Invoke();
            }

            PerformanceDataManager.Increment(fieldName);
        }
    }

    public interface IHasProfilableTransport
    {
        public ITransportProfilerData GetTransport();
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
