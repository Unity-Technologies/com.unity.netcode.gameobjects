

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class HelpUrlTests
    {
        private static readonly HttpClient k_HttpClient = new HttpClient();

        [UnityTest]
        public IEnumerator ValidateUrlsAreValid()
        {
            var names = new List<string>();
            var allUrls = new List<string>();

            foreach (var constant in typeof(HelpUrls).GetFields())
            {
                if (constant.IsLiteral && !constant.IsInitOnly)
                {
                    names.Add(constant.Name);
                    allUrls.Add((string)constant.GetValue(null));
                }
            }
            Debug.Log($"Found {allUrls.Count} URLs");

            var tasks = new List<Task<bool>>();
            foreach (var url in allUrls)
            {
                tasks.Add(IsRemoteFileAvailable(url));
            }

            while (tasks.Any(task => !task.IsCompleted))
            {
                yield return new WaitForSeconds(0.01f);
            }

            for (int i = 0; i < allUrls.Count; i++)
            {
                Assert.IsTrue(tasks[i].Result, $"HelpUrls.{names[i]} is an invalid path!");
            }
        }

        /// <summary>
        /// Checks if a remote file at the <paramref name="url"/> exists, and if access is not restricted.
        /// </summary>
        /// <param name="url">URL to a remote file.</param>
        /// <returns>True if the file at the <paramref name="url"/> is able to be downloaded, false if the file does not exist, or if the file is restricted.</returns>
        private static async Task<bool> IsRemoteFileAvailable(string url)
        {
            //Checking if URI is well formed is optional
            var uri = new Uri(url);
            if (!uri.IsWellFormedOriginalString())
            {
                Debug.LogError($"URL {url} is not well formed");
                return false;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, uri);
                using var response = await k_HttpClient.SendAsync(request);

                var exists = response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0;
                Debug.Log($"url {url} returned status code {response.StatusCode}");
                return exists;
            }
            catch
            {
                Debug.LogError($"URL {url} request failed");
                return false;
            }
        }

    }
}
