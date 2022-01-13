using UnityEngine;
using System;
using System.Diagnostics;
using System.Net.Http;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Collections.Generic;

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
        private static HttpClient s_HttpClient = new HttpClient();
        public static List<Task> AllTasks;

        static MultiprocessLogHandler()
        {
            AllTasks = new List<Task>();
        }

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
            string method2 = "2";
            string method3 = "3";
            if (st.FrameCount > 3)
            {
                method2 = st.GetFrame(2).GetMethod().Name;
                method3 = st.GetFrame(3).GetMethod().Name;
            }
            UnityEngine.Debug.LogFormat(logType, LogOption.NoStacktrace, context, $"MPLOG ({DateTime.Now:T}) : {method3} : {method2} : {method1} : {testName} : {format}", args);
            var webLog = new WebLog();
            UnityEngine.Debug.Log($"args is {args.Length} {args[0]}");
            webLog.Message = args[0].ToString();
            
            string json = JsonUtility.ToJson(webLog);
            UnityEngine.Debug.Log($"JSON version of {webLog} is {json}");
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            Task t = PostBasicAsync(webLog, cancelAfterDelay.Token);
            AllTasks.Add(t);
        }

        private static async Task PostBasicAsync(WebLog content, CancellationToken cancellationToken)
        {
            UnityEngine.Debug.Log("test");
            UnityEngine.Debug.Log("Trying to post to endpoint");
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://multiprocess-log-event-manager.test.ds.unity3d.com/api/MultiprocessLogEvent");
            var json = JsonUtility.ToJson(content);
            UnityEngine.Debug.Log($"JSON version of {content} is {json}");
            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = stringContent;
            
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            UnityEngine.Debug.Log(response.StatusCode);
        }
    }

    [Serializable]
    public struct WebLog
    {
        public string Message;        

        public override string ToString()
        {
            return base.ToString() + " " + Message;
        }
    }
}
