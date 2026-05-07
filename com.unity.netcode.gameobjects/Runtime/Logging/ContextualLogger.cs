using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using LogType = UnityEngine.LogType;
using Object = UnityEngine.Object;

namespace Unity.Netcode.Logging
{
    /// <summary>
    /// Configurable structured logger.
    /// Each logger instance collects system-wide context
    /// (e.g. which <see cref="Object"/> to attribute the logs to; or the <see cref="NetworkManager"/> relating to this object)
    /// Each log is made with a <see cref="Context"/> object that collects the local context of this individual log line
    /// The contextual logger will combine the system-wide context and the local context into one structured log message.
    /// </summary>
    internal class ContextualLogger
    {
        private const string k_CompilationCondition = "UNITY_ASSERTIONS";

        private const string k_NetcodeHeader = "[Netcode] ";
        private readonly Object m_Object;
        private readonly LogBuilder m_Builder = new();

        private LogContextNetworkManager m_ManagerContext;
        private readonly GenericContext m_LoggerContext;

        /// <summary>
        /// Compatibility mode with the old behavior of NetworkLog
        /// TODO: remove this when enough of the codebase is using the new log system
        /// </summary>
        private readonly bool m_UseCompatibilityMode;

        /// <summary>
        /// Creates a minimally configured contextual logger
        /// </summary>
        /// <param name="useCompatibilityMode">Suppresses adding </param>
        public ContextualLogger(bool useCompatibilityMode = false)
        {
            m_UseCompatibilityMode = useCompatibilityMode;
            m_ManagerContext = new LogContextNetworkManager(true);
            m_Object = null;
            m_LoggerContext = GenericContext.Create();
        }

        public ContextualLogger(Object inspectorObject)
        {
            m_ManagerContext = new LogContextNetworkManager(true);
            m_Object = inspectorObject;
            m_LoggerContext = GenericContext.Create();
        }

        public ContextualLogger(Object inspectorObject, [NotNull] NetworkManager networkManager)
        {
            m_ManagerContext = new LogContextNetworkManager(networkManager);
            m_Object = inspectorObject;
            m_LoggerContext = GenericContext.Create();
        }

        /// Used for the NetworkLog
        internal ContextualLogger(NetworkManager networkManager, bool useCompatibilityMode)
        {
            m_UseCompatibilityMode = useCompatibilityMode;
            m_ManagerContext = new LogContextNetworkManager(networkManager);
            m_Object = networkManager;
            m_LoggerContext = GenericContext.Create();
        }

        [Conditional(k_CompilationCondition)]
        internal void AddInfo(string key, object value)
        {
            m_LoggerContext.StoreInfo(key, value);
        }

        /// <summary>
        /// Adds info onto a logger that will be removed once the <see cref="DisposableContext"/> is disposed
        /// </summary>
        /// <param name="key">Key to log</param>
        /// <param name="value">Value to log</param>
        /// <returns>Object to dispose when context is no longer valid</returns>
        internal DisposableContext AddDisposableInfo(string key, object value)
        {
            m_LoggerContext.StoreInfo(key, value);
            return new DisposableContext(this, key);
        }

        [Conditional(k_CompilationCondition)]
        internal void RemoveInfo(string key)
        {
            m_LoggerContext.ClearInfo(key);
        }

        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CaptureFunctionCall([CallerMemberName] string memberName = "")
        {
            Log(LogType.Log, new Context(LogLevel.Developer, memberName, true));
        }

        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        public void Info(Context context) => Log(LogType.Log, context);
        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        public void Warning(Context context) => Log(LogType.Warning, context);
        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        public void Error(Context context) => Log(LogType.Error, context);

        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        public void InfoServer(Context context) => LogServer(LogType.Log, context);
        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        public void WarningServer(Context context) => LogServer(LogType.Warning, context);
        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        public void ErrorServer(Context context) => LogServer(LogType.Error, context);

        [HideInCallstack]
        public void Exception(Exception exception)
        {
            Debug.unityLogger.LogException(exception, m_Object);
        }

        [HideInCallstack]
        private void Log(LogType logType, Context context)
        {
            // Don't act if the LogLevel is higher than the level of this log
            if (m_ManagerContext.LogLevel > context.Level)
            {
                return;
            }

            var message = BuildLog(context);
            Debug.unityLogger.Log(logType, (object)message, context.RelevantObjectOverride ?? m_Object);
        }

        [HideInCallstack]
        private void LogServer(LogType logType, Context context)
        {
            // Don't act if the configured logging level is higher than the level of this log
            if (m_ManagerContext.LogLevel > context.Level)
            {
                return;
            }

            var message = BuildLog(context);
            Debug.unityLogger.Log(logType, (object)message, context.RelevantObjectOverride ?? m_Object);

            m_ManagerContext.TrySendMessage(logType, message.Remove(0, k_NetcodeHeader.Length));
        }

        private string BuildLog(Context context)
        {
            m_Builder.Reset();

            // Add the Netcode prefix
            m_Builder.Append(k_NetcodeHeader);

            if (m_UseCompatibilityMode)
            {
                m_Builder.Append(context.Message);
            }
            else
            {
                // Add the system context
                m_ManagerContext.AppendTo(m_Builder);
                m_LoggerContext.AppendTo(m_Builder);

                // Add the context for this log
                context.AppendTo(m_Builder);
            }

            return m_Builder.Build();
        }

        /// <summary>
        /// Removes the configured context from the logger when this object is disposed.
        /// </summary>
        public readonly struct DisposableContext : IDisposable
        {
            private readonly ContextualLogger m_Logger;
            private readonly string m_ToClear;

            internal DisposableContext(ContextualLogger logger, string toClear)
            {
                m_Logger = logger;
                m_ToClear = toClear;
            }

            public void Dispose()
            {
                m_Logger.RemoveInfo(m_ToClear);
            }
        }
    }
}
