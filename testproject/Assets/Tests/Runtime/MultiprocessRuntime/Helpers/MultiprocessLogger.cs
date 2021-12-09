using UnityEngine;
using System;
using NUnit.Framework;

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

        public static void LogError(string msg)
        {
            s_Logger.LogError("ERROR", msg);
        }

        public static void LogWarning(string msg)
        {
            s_Logger.LogWarning("WARN", msg);
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
            string testName = null;
            try
            {
                testName = TestContext.CurrentContext.Test.Name;
            }
            catch (Exception)
            {
                // ignored
            }

            if (string.IsNullOrEmpty(testName))
            {
                testName = "unknown";
            }

            Debug.LogFormat(logType, LogOption.NoStacktrace, context,$"MPLOG ({DateTime.Now:T}) : {testName} : {format}", args);
            
        }
    }
}
