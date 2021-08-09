using System;

namespace Unity.Netcode
{
    internal static class ProfilerNotifier
    {
        public delegate void PerformanceDataEventHandler(PerformanceTickData profilerData);

        public static event PerformanceDataEventHandler OnPerformanceDataEvent;

        public delegate void NoTickDataHandler();

        public static event NoTickDataHandler OnNoTickDataEvent;

        private static IProfilableTransportProvider s_ProfilableTransportProvider;
        private static bool s_FailsafeCheck;

        public static void Initialize(IProfilableTransportProvider profilableNetwork)
        {
            s_ProfilableTransportProvider = profilableNetwork
                                       ?? throw new ArgumentNullException(
                                           $"{nameof(profilableNetwork)} was not set");
            s_FailsafeCheck = false;
        }

        public static void ProfilerBeginTick()
        {
            PerformanceDataManager.BeginNewTick();
            var transport = s_ProfilableTransportProvider.Transport;
            transport?.BeginNewTick();
            s_FailsafeCheck = true;
        }

        public static void NotifyProfilerListeners()
        {
            if (!s_FailsafeCheck)
            {
                return;
            }

            s_FailsafeCheck = false;

            var data = PerformanceDataManager.GetData();
            var eventHandler = OnPerformanceDataEvent;
            if (eventHandler != null)
            {
                if (data != null)
                {
                    var transport = s_ProfilableTransportProvider.Transport;
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
}
