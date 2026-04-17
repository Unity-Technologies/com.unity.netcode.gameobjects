using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using LogType = UnityEngine.LogType;

namespace Unity.Netcode
{
    internal class ContextualLogger
    {
        private const string k_NetcodeHeader = "[Netcode] ";
        private bool m_UseCompatibilityMode;
        private readonly GameObject m_GameObject;
        private readonly ContextBuilder m_Builder = new();

        private LogContextNetworkManager m_ManagerContext;
        private readonly GenericContext m_LoggerContext;

        private const string k_CompilationCondition = "UNITY_ASSERTIONS";

        public ContextualLogger(bool useCompatibilityMode = false)
        {
            m_UseCompatibilityMode = useCompatibilityMode;
            m_ManagerContext = new LogContextNetworkManager(true);
            m_GameObject = null;
            m_LoggerContext = GenericContext.Create();
        }

        public ContextualLogger([NotNull] NetworkManager networkManager, GameObject gameObject)
        {
            m_ManagerContext = new LogContextNetworkManager(networkManager);
            m_GameObject = gameObject;
            m_LoggerContext = GenericContext.Create();
        }

        [Conditional(k_CompilationCondition)]
        internal void UpdateNetworkManagerContext(NetworkManager manager)
        {
            m_ManagerContext.Dispose();
            m_ManagerContext = new LogContextNetworkManager(manager);
        }

        [Conditional(k_CompilationCondition)]
        internal void PushContext(string key, object value)
        {
            m_LoggerContext.StoreInfo(key, value);
        }

        [Conditional(k_CompilationCondition)]
        internal void PushContext(string key)
        {
            m_LoggerContext.StoreContext(key);
        }

        [Conditional(k_CompilationCondition)]
        internal void PopContext(string key)
        {
            m_LoggerContext.ClearInfo(key);
        }


        [HideInCallstack]
        [Conditional(k_CompilationCondition)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CaptureFunctionCall([CallerMemberName] string memberName = "")
        {
            Log(LogType.Log, new Context(LogLevel.Developer, memberName,  true));
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
            Debug.unityLogger.LogException(exception, m_GameObject);
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
            Debug.unityLogger.Log(logType, (object)message, context.GameObjectOverride ?? m_GameObject);
        }

        [HideInCallstack]
        private void LogServer(LogType logType, Context context)
        {
            // Don't act if the configured logging level is higher than the level of this log
            if (m_ManagerContext.LogLevel <= context.Level)
            {
                return;
            }

            var message = BuildLog(context);
            Debug.unityLogger.Log(logType, (object)message, context.GameObjectOverride ?? m_GameObject);

            m_ManagerContext.TrySendMessage(logType, message);
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
    }

    internal class ContextBuilder
    {
        private readonly StringBuilder m_Builder = new();
        private const string k_OpenBracket = "[";
        private const string k_CloseBracket = "]";
        private const string k_Separator = ":";

        public void Reset()
        {
            m_Builder.Clear();
        }

        public void AppendContext(string context)
        {
            m_Builder.Append(k_OpenBracket);
            m_Builder.Append(context);
            m_Builder.Append(k_CloseBracket);
        }

        public void AppendContext(object key, object value)
        {
            m_Builder.Append(k_OpenBracket);
            m_Builder.Append(key);
            m_Builder.Append(k_Separator);
            m_Builder.Append(value);
            m_Builder.Append(k_CloseBracket);
        }

        public void Append(string value) => m_Builder.Append(value);

        public string Build() => m_Builder.ToString();
    }
}
