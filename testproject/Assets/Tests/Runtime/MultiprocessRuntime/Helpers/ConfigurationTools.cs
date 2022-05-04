using UnityEngine;
using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class ConfigurationTools
    {
        public static JobQueueItemArray GetRemoteConfig()
        {
            var theList = new JobQueueItemArray();
            try
            {
                using var client = new HttpClient();
                var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var responseTask = client.GetAsync("https://multiprocess-log-event-manager.cds.internal.unity3d.com/api/JobWithFile",
                    HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token);
                responseTask.Wait();
                var response = responseTask.Result;
                var contentTask = response.Content.ReadAsStringAsync();
                contentTask.Wait();
                MultiprocessLogger.Log($"remoteConfig content is {contentTask.Result}");
                JsonUtility.FromJsonOverwrite(contentTask.Result, theList);
                MultiprocessLogger.Log($"remoteConfig content is {theList.JobQueueItems.Count}");

            }
            catch (Exception e)
            {
                MultiprocessLogger.Log($"GetRemoteConfig - Exception {e.Message}");
            }
            finally
            {
                MultiprocessLogger.Log($"GetRemoteConfig - finally block");
            }
            return theList;
        }

        public static void CompleteJobQueueItem(JobQueueItem item)
        {
            Task t = PostJobQueueItem(item, "/complete");
            t.Wait();
        }

        public static void ClaimJobQueueItem(JobQueueItem item)
        {
            Task t = PostJobQueueItem(item, "/claim");
            t.Wait();
        }

        public static async Task PostJobQueueItem(JobQueueItem item, string path = "")
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://multiprocess-log-event-manager.cds.internal.unity3d.com/api/JobWithFile" + path);
            var json = JsonUtility.ToJson(item);
            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = stringContent;
            MultiprocessLogger.Log($"Posting remoteConfig to server {json}");
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token)
                .ConfigureAwait(false);
            MultiprocessLogger.Log($"remoteConfig posted, checking response {response.StatusCode}");
        }
    }

    [Serializable]
    public class JobQueueItemArray
    {
        public List<JobQueueItem> JobQueueItems;
    }

    [Serializable]
    public class JobQueueItem
    {

        public int Id;
        public long JobId;
        public string GitHash;
        public string HostIp;
        public int PlatformId;
        public int JobStateId;
        public string CreatedBy;
        public string UpdatedBy;
        public string TransportName;

    }
}
