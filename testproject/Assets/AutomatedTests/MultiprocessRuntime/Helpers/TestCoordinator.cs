using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(NetworkObject))]
internal class TestCoordinator : NetworkBehaviour
{
    /// <summary>
    /// TestCoordinator
    /// Used for coordinating multiprocess end to end tests. Used to call RPCs on other nodes and gather results
    /// /// Needed since the current remote player test runner hardcodes test to run in a bootstrap scene before launching the player.
    /// There's not per-test communication to run these tests, only to get the results per test as they are running
    /// </summary>
    public const float maxWaitTimeout = 10;
    public const char methodFullNameSplitChar = '@';
    public static string buildPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds/MultiprocessTestBuild");
    private bool m_ShouldShutdown;

    private NetworkDictionary<ulong, float> m_TestResults = new NetworkDictionary<ulong, float>(new NetworkVariableSettings()
    {
        WritePermission = NetworkVariablePermission.Everyone,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    private NetworkList<string> m_ErrorMessages = new NetworkList<string>(new NetworkVariableSettings()
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
        DontDestroyOnLoad(gameObject);
    }

    // private IEnumerator WaitForClientConnected()
    // {
    //     float startTime = Time.time;
    //     while (Time.time - startTime < maxWaitTimeout)
    //     {
    //         yield return new WaitForSeconds(1);
    //         if (NetworkManager.Singleton.IsConnectedClient)
    //         {
    //             yield break;
    //         }
    //     }
    //     // not connected anymore, quitting the player
    //     Application.Quit();
    // }

    public void Start()
    {
#if UNITY_EDITOR
        // Debug.Log("starting MLAPI host");
        // NetworkManager.Singleton.StartHost();
#else
        Debug.Log("starting MLAPI client");
        NetworkManager.Singleton.StartClient();
        // StartCoroutine(WaitForClientConnected()); // in case builds fail, can't have the old builds just stay idle. If they can't connect after a certain amount of time, disconnect
#endif
        m_TestResults.OnDictionaryChanged += OnResultsChanged;
        m_ErrorMessages.OnListChanged += OnErrorMessageChanged;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    public void OnDestroy()
    {
        m_TestResults.OnDictionaryChanged -= OnResultsChanged;
        m_ErrorMessages.OnListChanged -= OnErrorMessageChanged;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    private void OnErrorMessageChanged(NetworkListEvent<string> listChangedEvent)
    {
        switch (listChangedEvent.Type)
        {
            case NetworkListEvent<string>.EventType.Add:
            case NetworkListEvent<string>.EventType.Insert:
            case NetworkListEvent<string>.EventType.Value:
                Debug.LogError($"Got Exception client side {listChangedEvent.Value}, type is {listChangedEvent.Type} and index is {listChangedEvent.Index}");
                m_ErrorMessages.RemoveAt(listChangedEvent.Index); // consume error message
                break;
        }
    }

    private void OnResultsChanged(NetworkDictionaryEvent<ulong, float> dictChangedEvent)
    {
        Instance.AllClientResults.Add(dictChangedEvent.Key);
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        {
            // if disconnect callback is for me or for server, quit, we're done here
            Debug.Log($"received disconnect from {clientId}, quitting");
            Application.Quit();
        }
    }

    public static string GetMethodInfo(Action method)
    {
        return $"{method.Method.DeclaringType.FullName}{methodFullNameSplitChar}{method.Method.Name}";
    }

    public static void WriteResults(float result)
    {
        Instance.m_TestResults[NetworkManager.Singleton.LocalClientId] = result;
    }

    public static void WriteError(string errorMessage)
    {
        var localId = NetworkManager.Singleton.LocalClientId;

        Instance.m_ErrorMessages.Add($"{localId}@{errorMessage}");
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
            // if (Instance.m_ErrorMessages.Count != 0)
            // {
            //     foreach (var error in Instance.m_ErrorMessages)
            //     {
            //         Debug.LogError($"got error message {error}");
            //     }
            // }
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
            WriteError(e.Message);
            // throw e;
        }
    }

    [ClientRpc]
    public void CloseRemoteClientRpc()
    {
        NetworkManager.Singleton.StopClient();
        m_ShouldShutdown = true; // wait until isConnectedClient is false to run Application Quit in next update
        Debug.Log("Quitting player cleanly");
        Application.Quit();
    }

    private float m_TimeSinceLastConnected;
    public void Update()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            m_TimeSinceLastConnected = Time.time;
        }
        else if (Time.time - m_TimeSinceLastConnected > maxWaitTimeout || m_ShouldShutdown)
        {
            // Make sure we don't have zombie processes
            Application.Quit();
        }
    }

    public void TestRunTeardown()
    {
        m_TestResults.Clear();
        m_ErrorMessages.Clear();
        AllClientResults.Clear();
        CurrentResultClient = 0;
    }

    public static void StartWorkerNode()
    {
#if UNITY_EDITOR

        var workerNode = new Process();

        //TODO this should be replaced eventually by proper orchestration
#if UNITY_EDITOR_OSX
        workerNode.StartInfo.FileName = $"{buildPath}.app/Contents/MacOS/{PlayerSettings.productName}";
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
            Debug.LogError($"Error starting player, please make sure you build a player using the '{BuildAndRunMultiprocessTests.BuildMenuName}' menu, {e.Message} {e.Data} {e.ErrorCode}");
            throw e;
        }
#endif
    }
}
