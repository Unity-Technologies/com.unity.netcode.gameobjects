#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode.Runtime;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    internal class HelpUrlTests
    {
        private const string k_PackageName = "com.unity.netcode.gameobjects";
        private static readonly HttpClient k_HttpClient = new();

        private bool m_VerboseLogging = false;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // This test does not need to run against the Rust server.
            NetcodeIntegrationTestHelpers.IgnoreIfServiceEnviromentVariableSet();

            // Excluding from unified tests. If deemed needed, update test, then  remove.
            NetcodeIntegrationTestHelpers.IgnoreIfUnifiedTestsEnvironmentVariableSet();
        }

        [UnityTest]
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
                var split = (string[])null;
                var hasAnchor = url.Contains("#");
                if (hasAnchor)
                {
                    split = url.Split('#');
                    url = split[0];
                }

                var documentText = ContentExistsInDocumentation(url);
                var docExists = !string.IsNullOrEmpty(documentText);

                var stream = await GetContentFromRemoteFile(url, docExists);

                var redirectUrl = CalculateRedirectURl(url, stream);
                VerboseLog($"Calculated Redirect URL: {redirectUrl}");

                var content = await GetContentFromRemoteFile(redirectUrl, docExists);

                if (string.IsNullOrEmpty(content))
                {
                    content = documentText;
                }
                // If original url had an anchor part (e.g. some/url.html#anchor)
                if (hasAnchor)
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
        /// Checks if the help URI is yet to be published but exists as a document.
        /// </summary>
        /// <param name="url">the help uri</param>
        /// <returns>the contents of the file if it exist otherwise it returns an emtpy string</returns>
        private string ContentExistsInDocumentation(string url)
        {
            var splitFilter = url.Contains(HelpUrls.BaseManualUrl) ? HelpUrls.BaseManualUrl : HelpUrls.BaseApiUrl;
            var split = url.Split(splitFilter);
            var current = Directory.GetCurrentDirectory().Replace("testproject", string.Empty);
            var filePath = $"{current}com.unity.netcode.gameobjects/Documentation~/{split[1].Replace(".html", ".md")}";
            try
            {
                if (File.Exists(filePath))
                {
                    var bytes = File.ReadAllBytes(filePath);
                    return Encoding.UTF8.GetString(bytes);
                }
            }
            catch
            { }
            return string.Empty;
        }

        /// <summary>
        /// Checks if a remote file at the <paramref name="url"/> exists, and if access is not restricted.
        /// </summary>
        /// <param name="url">URL to a remote file.</param>
        /// <returns>True if the file at the <paramref name="url"/> is able to be downloaded, false if the file does not exist, or if the file is restricted.</returns>
        private async Task<string> GetContentFromRemoteFile(string url, bool documentExists)
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
                    if (!documentExists)
                    {
                        throw new Exception($"Failed to get remote file from URL {url}");
                    }
                    return string.Empty;
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
#endif
