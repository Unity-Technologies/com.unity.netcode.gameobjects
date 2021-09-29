using UnityEngine;
using System;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class MultiprocessLogger
    {
        private static Logger s_Logger;

        static MultiprocessLogger() => s_Logger = new Logger(logHandler: new MultiprocessLogHandler());

        public static void Log(string msg)
        {
            s_Logger.Log(msg);
        }
    }

    public class MultiprocessLogHandler : ILogHandler
    {
        public void LogException(Exception exception, UnityEngine.Object context)
        {
            Debug.unityLogger.LogException(exception, context);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            Debug.unityLogger.logHandler.LogFormat(logType, context, format, args);
        }
    }
}
