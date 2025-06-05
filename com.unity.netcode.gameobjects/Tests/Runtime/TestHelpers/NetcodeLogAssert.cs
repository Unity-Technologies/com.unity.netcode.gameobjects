using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetcodeLogAssert : IDisposable
    {
        private struct LogData
        {
            public LogType LogType;
            public string Message;
            public string StackTrace;
        }

        private readonly object m_Lock = new object();
        private bool m_Disposed;

        private List<LogData> AllLogs { get; }

        private bool m_ResetIgnoreFailingMessagesOnTearDown;
        public NetcodeLogAssert(bool ignorFailingMessages = false, bool resetOnTearDown = true)
        {
            LogAssert.ignoreFailingMessages = ignorFailingMessages;
            m_ResetIgnoreFailingMessagesOnTearDown = resetOnTearDown;
            AllLogs = new List<LogData>();
            Activate();
        }

        private void Activate()
        {
            Application.logMessageReceivedThreaded += AddLog;
        }

        private void Deactivate()
        {
            Application.logMessageReceivedThreaded -= AddLog;
        }

        public void AddLog(string message, string stacktrace, LogType type)
        {
            lock (m_Lock)
            {
                var log = new LogData
                {
                    LogType = type,
                    Message = message,
                    StackTrace = stacktrace,
                };

                AllLogs.Add(log);
            }
        }

        [UnityTearDown]
        public void OnTearDown()
        {
            // Defaults to true and will reset LogAssert.ignoreFailingMessages during tear down
            if (m_ResetIgnoreFailingMessagesOnTearDown)
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Always reset when disposing
            LogAssert.ignoreFailingMessages = false;
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;

            if (disposing)
            {
                Deactivate();
            }
        }

        public void LogWasNotReceived(LogType type, string message)
        {
            lock (m_Lock)
            {
                foreach (var logEvent in AllLogs)
                {
                    if (logEvent.LogType == type && message.Equals(logEvent.Message))
                    {
                        Assert.Fail($"Unexpected log: [{logEvent.LogType}] {logEvent.Message}");
                    }
                }
            }
        }

        public void LogWasNotReceived(LogType type, Regex messageRegex)
        {
            lock (m_Lock)
            {
                foreach (var logEvent in AllLogs)
                {
                    if (logEvent.LogType == type && messageRegex.IsMatch(logEvent.Message))
                    {
                        Assert.Fail($"Unexpected log: [{logEvent.LogType}] {logEvent.Message}");
                    }
                }
            }
        }

        public void LogWasReceived(LogType type, string message)
        {
            lock (m_Lock)
            {
                var found = false;
                foreach (var logEvent in AllLogs)
                {
                    if (logEvent.LogType == type && message.Equals(logEvent.Message))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Assert.Fail($"Expected log was not received: [{type}] {message}");
                }
            }
        }

        public void LogWasReceived(LogType type, Regex messageRegex)
        {
            lock (m_Lock)
            {
                var found = false;
                foreach (var logEvent in AllLogs)
                {
                    if (logEvent.LogType == type && messageRegex.IsMatch(logEvent.Message))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Assert.Fail($"Expected log was not received: [{type}] {messageRegex}");
                }
            }
        }

        public bool HasLogBeenReceived(LogType type, string message)
        {
            var found = false;
            lock (m_Lock)
            {
                foreach (var logEvent in AllLogs)
                {
                    if (logEvent.LogType == type && message.Equals(logEvent.Message))
                    {
                        found = true;
                        break;
                    }
                }
            }
            return found;
        }

        public void Reset()
        {
            lock (m_Lock)
            {
                AllLogs.Clear();
            }
        }
    }
}
