using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrollKing.Core
{
    public enum NetworkLoggingLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Exception,
        None
    }

    public class NetworkLogScope
    {
        private readonly string m_LoggerName;
        private readonly NetworkLoggingLevel m_Level = NetworkLoggingLevel.Info;

        public NetworkLogScope(string logName, NetworkLoggingLevel logLevel = NetworkLoggingLevel.Info)
        {
            m_LoggerName = logName;
            m_Level = logLevel;
        }

        public NetworkLoggingLevel GetLevel()
        {
            return m_Level;
        }

        public void Log(Func<string> stringProvider, NetworkLoggingLevel logLevel = NetworkLoggingLevel.Info)
        {
            if (logLevel >= m_Level)
            {
                string logString = stringProvider.Invoke();
                DateTime time = DateTime.Now;
                var shortTime = time.ToString("T");

                switch (logLevel)
                {
                    case NetworkLoggingLevel.Debug:
                        UnityEngine.Debug.Log($"[{shortTime}][DEBUG][{m_LoggerName}] {logString}");
                        break;
                    case NetworkLoggingLevel.Info:
                        UnityEngine.Debug.Log($"[{shortTime}][INFO][{m_LoggerName}] {logString}");
                        break;
                    case NetworkLoggingLevel.Warn:
                        UnityEngine.Debug.LogWarning($"[{shortTime}][WARN][{m_LoggerName}] {logString}");
                        break;
                    case NetworkLoggingLevel.Error:
                        UnityEngine.Debug.LogError($"[{shortTime}][ERROR][{m_LoggerName}] {logString}");
                        break;
                    case NetworkLoggingLevel.Exception:
                        UnityEngine.Debug.LogError($"[{shortTime}][EXCEPTION][{m_LoggerName}] {logString}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }
            }
        }

        public void Debug(Func<string> logString)
        {
            Log(logString, NetworkLoggingLevel.Debug);
        }

        public void Info(Func<string> logString)
        {
            Log(logString, NetworkLoggingLevel.Info);
        }

        public void Warning(Func<string> logString)
        {
            Log(logString, NetworkLoggingLevel.Warn);
        }

        public void LogWarning(Func<string> logString)
        {
            Log(logString, NetworkLoggingLevel.Warn);
        }

        public void Error(Func<string> logString)
        {
            Log(logString, NetworkLoggingLevel.Error);
        }

        public void LogError(Func<string> logString)
        {
            Log(logString, NetworkLoggingLevel.Error);
        }

        public void LogException(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }

        public void LogError(Exception e)
        {
            UnityEngine.Debug.LogError($"[{m_LoggerName}] {e}");
            UnityEngine.Debug.LogException(e);
        }
    }

    public static class NetworkGameObjectUtility
    {
        private static readonly NetworkLogScope Log = new NetworkLogScope(nameof(NetworkGameObjectUtility));

        private static string NetworkGetScenePathRecursive(Transform go, string path)
        {
            if (go.parent == null)
            {
                return $"{go.gameObject.scene.name}:{go.name}:{path}";
            }

            return NetworkGetScenePathRecursive(go.parent, $"{go.name}:{path}");
        }

        // Depth first, we are going all the way down each leg
        public static void NetworkGetAllHierarchyChildrenRecursive(this GameObject source, ref Queue<GameObject> queue)
        {
            if (source == null)
            {
                return;
            }

            int children = source.transform.childCount;
            for (int i = 0; i < children; i++)
            {
                var child = source.transform.GetChild(i);
                var go = child.gameObject;
                Log.Debug(() => $"AddingHierarchyChild {go}");
                queue.Enqueue(go);
                NetworkGetAllHierarchyChildrenRecursive(go, ref queue);
            }
        }

        public static Queue<GameObject> NetworkGetAllHierarchyChildren(this GameObject root)
        {
            var retVal = new Queue<GameObject>();
            if (root == null)
            {
                return retVal;
            }
            Log.Debug(() => $"AddingHierarchyParent {root}");
            retVal.Enqueue(root);
            root.NetworkGetAllHierarchyChildrenRecursive(ref retVal);
            return retVal;
        }

        public static string NetworkGetScenePath(this GameObject go)
        {
            return NetworkGetScenePath(go.transform);
        }

        public static string NetworkGetScenePath(this Transform go)
        {
            return NetworkGetScenePathRecursive(go, "");
        }

        public static string NetworkGetSceneName(this GameObject go)
        {
            return go.scene.name;
        }

        public static bool NetworkTryGetComponentInParent<T>(this GameObject go, out T comp)
        {
            var parent = go.transform;
            while (parent != null && parent.parent != parent)
            {
                if (parent.TryGetComponent(out comp))
                {
                    return true;
                }
                parent = parent.parent;
            }

            comp = default;
            return false;
        }
    }
}
