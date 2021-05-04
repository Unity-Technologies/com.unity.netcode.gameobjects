using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// TestCoordinator
/// Used for coordinating multiprocess end to end tests. Used to call RPCs on other nodes and gather results
/// /// Needed since the current remote player test runner hardcodes test to run in a bootstrap scene before launching the player.
/// There's not per-test communication to run these tests, only to get the results per test as they are running
/// </summary>
internal class TestCoordinator : NetworkBehaviour
{
    public const float maxWaitTimeout = 20;
    public const char methodFullNameSplitChar = '.';
    public const string buildPath = "Builds/MultiprocessTestBuild";
    public const string buildName = "unity-unit-test";

    private NetworkDictionary<ulong, float> m_TestResults = new NetworkDictionary<ulong, float>(new NetworkVariableSettings()
    {
        WritePermission = NetworkVariablePermission.Everyone,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    private NetworkDictionary<ulong, List<string>> m_ErrorMessages = new NetworkDictionary<ulong, List<string>>(new NetworkVariableSettings()
    {
        ReadPermission = NetworkVariablePermission.Everyone,
        WritePermission = NetworkVariablePermission.Everyone
    });

    [NonSerialized]
    public List<ulong> AllClientResults = new List<ulong>();
    public ulong CurrentResultClient { get; set; }

    public static TestCoordinator Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static string GetMethodInfo(Action method)
    {
        return $"{method.Method.DeclaringType.Name}{methodFullNameSplitChar}{method.Method.Name}";
    }

    public static void WriteResults(float result)
    {
        Instance.m_TestResults[NetworkManager.Singleton.LocalClientId] = result;
    }

    public static void WriteError(string errorMessage)
    {
        Instance.m_ErrorMessages[NetworkManager.Singleton.LocalClientId].Add(errorMessage);
    }

    public static float GetCurrentResult()
    {
        var clientID = Instance.CurrentResultClient; // we're singlethreaded here right? there's no risk of calling this here?
        return Instance.m_TestResults[clientID];
    }

    public static Func<bool> SetResults()
    {
        var startWaitTime = Time.time;
        return () =>
        {
            if (Instance.m_ErrorMessages.Count != 0)
            {
                foreach (var error in Instance.m_ErrorMessages)
                {
                    Debug.LogError($"client {error.Key} got error message {error.Value}");
                }
            }
            if (Time.time - startWaitTime > maxWaitTimeout)
            {
                Assert.Fail($"timeout while waiting for results, didn't results for {maxWaitTimeout} seconds");
            }

            if (Instance.AllClientResults.Count > 0)
            {
                Instance.CurrentResultClient = Instance.AllClientResults[0];
                Instance.AllClientResults.RemoveAt(0);
                return true;
            }

            return false;
        };
    }

    [ClientRpc]
    public void TriggerTestClientRpc(string methodInfoString)
    {
        try
        {
            var split = methodInfoString.Split(methodFullNameSplitChar);
            (string classToExecute, string staticMethodToExecute) info = (split[0], split[1]);

            var foundType = Type.GetType(info.classToExecute);
            if (foundType == null)
            {
                throw new Exception($"couldn't find {info.classToExecute}");
            }

            var foundMethod = foundType.GetMethod(info.staticMethodToExecute);
            if (foundMethod == null)
            {
                throw new Exception($"couldn't find method {info.staticMethodToExecute}");
            }

            foundMethod.Invoke(null, null);
        }
        catch (Exception e)
        {
            WriteError(e.ToString());
        }
    }

    [ClientRpc]
    public void CloseRemoteClientRpc()
    {
        Application.Quit();
    }

    private float m_TimeSinceLastConnected;
    public void Update()
    {
        // Make sure we don't have zombie processes
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            m_TimeSinceLastConnected = Time.time;
        }
        else if (Time.time - m_TimeSinceLastConnected > maxWaitTimeout)
        {
            Application.Quit(1);
        }
    }

    public void TestRunTeardown()
    {
        m_TestResults.Clear();
        m_ErrorMessages.Clear();
        AllClientResults.Clear();
        CurrentResultClient = 0;
    }

    #if UNITY_EDITOR
    public static void StartWorkerNode()
    {
        var workerNode = new Process();

        //TODO this should be replaced eventually by proper orchestration
#if UNITY_EDITOR_OSX
        workerNode.StartInfo.FileName = $"{buildPath}.app/Contents/MacOS/{buildName}";
#elif UNITY_EDITOR_WIN
        workerNode.StartInfo.FileName = $"{buildPath}/buildName.exe";
#else
        throw new NotImplementedException("Current platform not supported");
#endif

        workerNode.StartInfo.UseShellExecute = false;
        workerNode.StartInfo.RedirectStandardError = true;
        workerNode.StartInfo.RedirectStandardOutput = true;
        workerNode.StartInfo.Arguments = "-popupwindow -screen-width 100 -screen-height 100";

        // workerNode.StartInfo.Arguments = $"-startAsServer";
        try
        {
            var newProcessStarted = workerNode.Start();
            Debug.Log($"new process started? {newProcessStarted}");
            if (!newProcessStarted)
            {
                throw new Exception("Process not started!");
            }
        }
        catch (Win32Exception e)
        {
            Debug.LogError($"Error starting process, {e.Message} {e.Data} {e.ErrorCode}");
            throw e;
        }
    }
    #endif
}
