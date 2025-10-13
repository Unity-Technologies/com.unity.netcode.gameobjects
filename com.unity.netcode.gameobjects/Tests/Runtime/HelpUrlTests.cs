

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class HelpUrlTests
    {
        private const string k_PackageName = "com.unity.netcode.gameobjects";
        private static readonly HttpClient k_HttpClient = new();

        private bool m_VerboseLogging = false;

        // IOS platform can't run this test for some reason.
        [UnityTest]
        [UnityPlatform(exclude = new[] { RuntimePlatform.IPhonePlayer })]
        public IEnumerator ValidateUrlsAreValid()
        {
            var names = new List<string>();
            var allUrls = new List<string>();

            // GetFields() can only see public strings. Ensure each HelpUrl is public.
            foreach (var constant in typeof(HelpUrls).GetFields())
            {
                if (constant.IsLiteral && !constant.IsInitOnly)
                {
                    names.Add(constant.Name);
                    allUrls.Add((string)constant.GetValue(null));
                }
            }

            VerboseLog($"Found {allUrls.Count} URLs");

            var tasks = new List<Task<bool>>();
            foreach (var url in allUrls)
            {
                tasks.Add(AreUnityDocsAvailableAt(url));
            }

            while (tasks.Any(task => !task.IsCompleted))
            {
                yield return new WaitForSeconds(0.01f);
            }

            for (int i = 0; i < allUrls.Count; i++)
            {
                Assert.IsTrue(tasks[i].Result, $"HelpUrls.{names[i]} has an invalid path! Path: {allUrls[i]}");
            }
        }

        private async Task<bool> AreUnityDocsAvailableAt(string url)
        {
            try
            {
                var split = url.Split('#');
                url = split[0];

                var stream = await GetContentFromRemoteFile(url);

                var redirectUrl = CalculateRedirectURl(url, stream);
                VerboseLog($"Calculated Redirect URL: {redirectUrl}");

                var content = await GetContentFromRemoteFile(redirectUrl);

                // If original url had an anchor part (e.g. some/url.html#anchor)
                if (split.Length > 1)
                {
                    var anchorString = split[1];

                    // All headings will have an id with the anchorstring (e.g. <h2 id="anchor">)
                    if (!content.Contains($"id=\"{anchorString}\">"))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                VerboseLog(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks if a remote file at the <paramref name="url"/> exists, and if access is not restricted.
        /// </summary>
        /// <param name="url">URL to a remote file.</param>
        /// <returns>True if the file at the <paramref name="url"/> is able to be downloaded, false if the file does not exist, or if the file is restricted.</returns>
        private async Task<string> GetContentFromRemoteFile(string url)
        {
            //Checking if URI is well formed is optional
            var uri = new Uri(url);
            if (!uri.IsWellFormedOriginalString())
            {
                throw new Exception($"URL {url} is not well formed");
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await k_HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength <= 0)
                {
                    throw new Exception($"Failed to get remote file from URL {url}");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                throw new Exception($"URL {url} request failed");
            }
        }

        private string CalculateRedirectURl(string originalRequest, string content)
        {
            var uri = new Uri(originalRequest);
            var baseRequest = $"{uri.Scheme}://{uri.Host}";
            foreach (var segment in uri.Segments)
            {
                if (segment.Contains(k_PackageName))
                {
                    break;
                }
                baseRequest += segment;
            }

            var subfolderRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)").Match(uri.Query);
            var subfolder = "";
            foreach (Group match in subfolderRegex.Groups)
            {
                subfolder = match.Value;
            }

            string pattern = @"com.unity.netcode.gameobjects\@(\d+.\d+)";
            var targetDestination = "";
            foreach (Match match in Regex.Matches(content, pattern))
            {
                targetDestination = match.Value;
                break;
            }

            return baseRequest + targetDestination + subfolder;
        }

        private void VerboseLog(string message)
        {
            if (m_VerboseLogging)
            {
                Debug.unityLogger.Log(message);
            }
        }

    }
}
