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
            s_Logger.LogError("ERROR " + msg, msg);
        }

        public static void LogWarning(string msg)
        {
            Log(msg);
            s_Logger.LogWarning("WTAG " + msg, msg);
        }
    }

    public class MultiprocessLogHandler : ILogHandler
    {
        private static HttpClient s_HttpClient = new HttpClient();
        private static List<Task> s_AllTasks;
        public static long JobId;
        public static string TestName;
        private static readonly object k_Tasklock = new object();
        private static long s_EventIdCounter;

        static MultiprocessLogHandler()
        {
            s_AllTasks = new List<Task>();
            if (JobId == 0)
            {
                string sJobId = Environment.GetEnvironmentVariable("YAMATO_JOB_ID");
                if (!long.TryParse(sJobId, out JobId))
                {
                    JobId = -2;
                }
            }
            s_EventIdCounter = 0;
        }

        public static string Flush()
        {
            bool interrupted = false;
            var stopWatch = Stopwatch.StartNew();
            lock (k_Tasklock)
            {
                foreach (var task in s_AllTasks)
                {
                    task.Wait();
                    if (stopWatch.ElapsedMilliseconds > 20000)
                    {
                        interrupted = true;
                        break;
                    }
                }
            }
            stopWatch.Stop();
            if (interrupted)
            {
                return $"Flush Logs took : {stopWatch.Elapsed} ticks: {stopWatch.ElapsedTicks} but was interrupted due to timeout";
            }
            return $"Flush Logs took : {stopWatch.Elapsed} ticks: {stopWatch.ElapsedTicks} ";
        }

        public static string ReportQueue()
        {
            int canceledCount = 0;
            int totalCount = s_AllTasks.Count;
            int ranToCompletionCount = 0;
            int runningCount = 0;
            int waitingForActivation = 0;
            int waitingToRun = 0;
            var stopWatch = Stopwatch.StartNew();
            lock (k_Tasklock)
            {
                foreach (var task in s_AllTasks)
                {
                    if (task.Status == TaskStatus.Canceled)
                    {
                        canceledCount++;
                    }
                    else if (task.Status == TaskStatus.RanToCompletion)
                    {
                        ranToCompletionCount++;
                    }
                    else if (task.Status == TaskStatus.Running)
                    {
                        runningCount++;
                    }
                    else if (task.Status == TaskStatus.WaitingForActivation)
                    {
                        waitingForActivation++;
                    }
                    else if (task.Status == TaskStatus.WaitingToRun)
                    {
                        waitingToRun++;
                    }
                }
            }
            stopWatch.Stop();
            string msg = $"AllTasks.Count {totalCount} canceled: {canceledCount} completed: {ranToCompletionCount} running: {runningCount} waitingToRun: {waitingToRun} waitingForActivation: {waitingForActivation}";
            return msg;
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            UnityEngine.Debug.unityLogger.LogException(exception, context);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            string testName = null;
            string testClass = " - c -";
            try
            {
                testName = TestContext.CurrentContext.Test.Name;
                testClass = TestContext.CurrentContext.Test.ClassName;

            }
            catch (Exception)
            {
                // ignored
            }

            if (string.IsNullOrEmpty(testName))
            {
                if (string.IsNullOrEmpty(TestName))
                {
                    testName = "unknown";
                }
                else
                {
                    testName = TestName;
                }
            }

            var st = new StackTrace(true);

            string methods = "";

            int maxFrame = 5;
            for (int i = 3; i < st.FrameCount; i++)
            {
                string methodName = st.GetFrame(i).GetMethod().Name;
                if (methodName.Contains("MoveNext") || methodName.Contains("Invoke"))
                {
                    maxFrame++;
                }
                else
                {
                    methods += " : " + methodName + "." + i;
                }
                if (i > maxFrame)
                {
                    break;
                }
            }

            UnityEngine.Debug.LogFormat(logType, LogOption.NoStacktrace, context, $"MPLOG ({DateTime.Now:T}) : {methods} : {testName} : {format}", args);
            if (!args[0].ToString().Contains("POST call")) // If we have to log that the logging system is acting up, don't post that as it would just make a never ending cycle
            {
                var webLog = new WebLog();
                webLog.Message = $"{testName} {args[0].ToString()}";
                if (webLog.Message.Length > 1000)
                {
                    webLog.Message = webLog.Message.Substring(0, 999);
                }
                webLog.ReferenceId = JobId;
                webLog.EventId = s_EventIdCounter++;
                webLog.TestMethod = testName;
                webLog.TestClass = testClass;
                webLog.ClientEventDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                // string json = JsonUtility.ToJson(webLog);
                var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                Task t = PostBasicAsync(webLog, cancelAfterDelay.Token);
                lock (k_Tasklock)
                {
                    s_AllTasks.Add(t);
                }
            }
        }

        public static string TestEndpoint()
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://multiprocess-log-event-manager.cds.internal.unity3d.com/");
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var responseTask = client.GetAsync("https://multiprocess-log-event-manager.cds.internal.unity3d.com/",
                HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token);

            responseTask.Wait();
            var response = responseTask.Result;

            return response.StatusCode.ToString();
        }

        private static async Task PostBasicAsync(WebLog content, CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            // TODO: Make this endpoint configurable and share this code across testproject
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://multiprocess-log-event-manager.cds.internal.unity3d.com/api/MultiprocessLogEvent");
            var json = JsonUtility.ToJson(content);
            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = stringContent;
            var logginMetric = LoggingMetric.StartNew();

            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                MultiprocessLogger.LogWarning($"POST called failed with {response.StatusCode} for message with id {content.EventId} and message {content.Message}");
            }
            logginMetric.Stop(content, response);
        }
    }

    public class LoggingMetric
    {
        public static List<LoggingMetric> LoggingMetricsDataArray;

        public Stopwatch Stopwatch;

        static LoggingMetric()
        {
            LoggingMetricsDataArray = new List<LoggingMetric>();
        }

        public LoggingMetric()
        {
            Stopwatch = Stopwatch.StartNew();
        }

        public static LoggingMetric StartNew()
        {
            var lm = new LoggingMetric();
            LoggingMetricsDataArray.Add(lm);
            return lm;
        }

        public void Stop(WebLog content, HttpResponseMessage responseMessage)
        {
            Stopwatch.Stop();
            if (Stopwatch.ElapsedMilliseconds > 5000)
            {
                MultiprocessLogger.Log($"POST call took {Stopwatch.ElapsedMilliseconds} with response {responseMessage.StatusCode} for message: {content.EventId}");
            }
        }
    }

    [Serializable]
    public class WebLog
    {
        public string Message;
        public long ReferenceId;
        public string TestMethod;
        public string TestClass;
        public string ClientEventDate;
        public long EventId;

        public override string ToString()
        {
            return base.ToString() + " " + Message;
        }

        public WebLog()
        {
            Message = "Default message from constructor";
        }
    }
}
