using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Class used to handle asserting if certain log messages were not logged.
    /// </summary>
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ignorFailingMessages"><see cref="true"/> or <see cref="false"/></param>
        /// <param name="resetOnTearDown"><see cref="true"/> or <see cref="false"/></param>
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

        /// <summary>
        /// Invoke to add a log during a test.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="stacktrace">The stack trace of where the issue occurred.</param>
        /// <param name="type">The <see cref="LogType"/>.</param>
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

        /// <summary>
        /// Tear down method
        /// </summary>
        [UnityTearDown]
        public void OnTearDown()
        {
            // Defaults to true and will reset LogAssert.ignoreFailingMessages during tear down
            if (m_ResetIgnoreFailingMessagesOnTearDown)
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        /// <summary>
        /// Dispose method
        /// </summary>
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

        /// <summary>
        /// Assert if a log message was logged with the expectation it would not be.
        /// </summary>
        /// <param name="type"><see cref="LogType"/> to check for.</param>
        /// <param name="message"><see cref="string"/> containing the message to search for.</param>
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

        /// <summary>
        /// Assert if a log message was logged with the expectation it would not be. (RegEx version)
        /// </summary>
        /// <param name="type"><see cref="LogType"/> to check for.</param>
        /// <param name="messageRegex"><see cref="Regex"/> containing the message pattern to search for.</param>
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

        /// <summary>
        /// Assert if a log message was not logged with the expectation that it would be.
        /// </summary>
        /// <param name="type"><see cref="LogType"/> to check for.</param>
        /// <param name="message"><see cref="string"/> containing the message to search for.</param>
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

        /// <summary>
        /// Assert if a log message was not logged with the expectation that it would be. (RegEx version)
        /// </summary>
        /// <param name="type"><see cref="LogType"/> to check for.</param>
        /// <param name="messageRegex"><see cref="Regex"/> containing the message pattern to search for.</param>
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

        /// <summary>
        /// Determines if a log message was logged or not.
        /// </summary>
        /// <param name="type"><see cref="LogType"/> to check for.</param>
        /// <param name="message"><see cref="string"/> containing the message to search for.</param>
        /// <returns><see cref="true"/> or <see cref="false"/></returns>
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

        /// <summary>
        /// Clears out the log history that is searched.
        /// </summary>
        public void Reset()
        {
            lock (m_Lock)
            {
                AllLogs.Clear();
            }
        }
    }
}
