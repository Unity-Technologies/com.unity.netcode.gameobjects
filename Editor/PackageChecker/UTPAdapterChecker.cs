#if COM_UNITY_NETCODE_ADAPTER_UTP
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Unity.Netcode.Editor.PackageChecker
{
    [InitializeOnLoad]
    internal class UTPAdapterChecker
    {
        private const string k_UTPAdapterPackageName = "com.unity.netcode.adapter.utp";

        private static ListRequest s_ListRequest = null;

        static UTPAdapterChecker()
        {
            if (s_ListRequest == null)
            {
                s_ListRequest = Client.List();
                EditorApplication.update += EditorUpdate;
            }
        }

        private static void EditorUpdate()
        {
            if (!s_ListRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= EditorUpdate;

            if (s_ListRequest.Status == StatusCode.Success)
            {
                if (s_ListRequest.Result.Any(p => p.name == k_UTPAdapterPackageName))
                {
                    Debug.Log($"({nameof(UTPAdapterChecker)}) Found UTP Adapter package, it is no longer needed, `UnityTransport` is now directly integrated into the SDK therefore removing it from the project.");
                    Client.Remove(k_UTPAdapterPackageName);
                }
            }
            else
            {
                var error = s_ListRequest.Error;
                Debug.LogError($"({nameof(UTPAdapterChecker)}) Cannot check the list of packages -> error #{error.errorCode}: {error.message}");
            }

            s_ListRequest = null;
        }
    }
}
#endif // COM_UNITY_NETCODE_ADAPTER_UTP
