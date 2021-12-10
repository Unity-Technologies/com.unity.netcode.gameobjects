using UnityEngine;
using System;
using System.Diagnostics;
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
            UnityEngine.Debug.unityLogger.LogException(exception, context);
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

            var st = new StackTrace(true);
            string method1 = st.GetFrame(1).GetMethod().Name;
            string method3 = st.GetFrame(3).GetMethod().Name;
            string method5 = st.GetFrame(5).GetMethod().Name;

            UnityEngine.Debug.LogFormat(logType, LogOption.NoStacktrace, context,$"MPLOG ({DateTime.Now:T}) : {method5} : {method3} : {method1} : {testName} : {format}", args);
            
        }
    }
}
