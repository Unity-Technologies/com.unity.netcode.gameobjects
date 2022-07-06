using UnityEngine;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class ConfigurationTools
    {
        private static FileInfo s_Localip_fileinfo;

        static ConfigurationTools()
        {
            s_Localip_fileinfo = new FileInfo(Path.Combine(MultiprocessOrchestration.MultiprocessDirInfo.FullName, "localip"));
        }

        public static async Task<JobQueueItemArray> GetRemoteConfig()
        {
            var theList = new JobQueueItemArray();
            using var client = new HttpClient();
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await client.GetAsync("https://multiprocess-log-event-manager.cds.internal.unity3d.com/api/JobWithFile",
                HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync();
            JsonUtility.FromJsonOverwrite(content, theList);
            return theList;
        }

        public static async void CompleteJobQueueItem(JobQueueItem item)
        {
            await PostJobQueueItem(item, "/complete");
        }

        public static async void ClaimJobQueueItem(JobQueueItem item)
        {
            await PostJobQueueItem(item, "/claim");
        }

        public static void PostJobQueueItem(string githash)
        {
            MultiprocessLogger.Log($"Posting remoteConfig to server {githash}");
            var item = new JobQueueItem();
            item.GitHash = githash;
            item.JobId = MultiprocessLogHandler.JobId;
            item.HostIp = GetLocalIPAddress();
            item.CreatedBy = "zmecklai";
            item.UpdatedBy = "zmecklai";
            item.TransportName = "UNET";
            item.JobStateId = 1;
            item.PlatformId = 7;

            Task t = PostJobQueueItem(item);
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
            var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelAfterDelay.Token).ConfigureAwait(false);
            MultiprocessLogger.Log($"remoteConfig posted, checking response {response.StatusCode}");
        }

        public static string GetLocalIPAddress()
        {
            string bOKKEN_HOST_IP = Environment.GetEnvironmentVariable("BOKKEN_HOST_IP");
            if (!string.IsNullOrEmpty(bOKKEN_HOST_IP) && bOKKEN_HOST_IP.Contains("."))
            {
                MultiprocessLogger.Log($"BOKKEN_HOST_IP was found as {bOKKEN_HOST_IP}");
                return bOKKEN_HOST_IP;
            }

            if (s_Localip_fileinfo.Exists)
            {
                string alllines = File.ReadAllText(s_Localip_fileinfo.FullName).Trim();
                MultiprocessLogger.Log($"localIP file was found as {alllines}");
                return alllines;
            }

            string localhostname = Dns.GetHostName();

            try
            {
                if (!localhostname.Equals("Mac-mini.local"))
                {
                    var host = Dns.GetHostEntry(localhostname);
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string localIPAddress = ip.ToString();

                            WriteLocalIP(localIPAddress);
                            return localIPAddress;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MultiprocessLogger.LogError("Error: " + e.Message);
                MultiprocessLogger.LogError("Error Stack: " + e.StackTrace);
            }

            try
            {
                return GetLocalIPAddressFromNetworkInterface();
            }
            catch (Exception e)
            {
                MultiprocessLogger.LogError("Error: " + e.Message);
                MultiprocessLogger.LogError("Error Stack: " + e.StackTrace);
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void WriteLocalIP(string localip)
        {
            using StreamWriter sw = File.CreateText(s_Localip_fileinfo.FullName);
            sw.WriteLine(localip);
        }

        private static string GetLocalIPAddressFromNetworkInterface()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {

                    if (ip.Address.ToString().Contains(".") && !ip.Address.ToString().Equals("127.0.0.1"))
                    {
                        // TODO: Write this to a file so we don't have to keep getting this IP over and over
                        WriteLocalIP(ip.Address.ToString());
                        return ip.Address.ToString();
                    }
                }
            }
            return "";
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
