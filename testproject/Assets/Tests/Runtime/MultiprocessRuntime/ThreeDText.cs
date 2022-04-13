using UnityEngine;
#if UNITY_UNET_PRESENT
using Unity.Netcode.Transports.UNET;
#endif

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class ThreeDText : MonoBehaviour
    {
        public bool IsTestCoordinatorActiveAndEnabled = false;
        public string CommandLineArguments = "";
        private long m_UpdateCounter;
        private bool m_HasFired;
        private string m_TransportString;
        public static bool IsPerformanceTest = false;

        public void Awake()
        {
            Debug.Log("ThreeDText - Awake - Start");
            if (IsPerformanceTest)
            {
                Debug.Log("Setting Active to false as this is a performance test");
                gameObject.SetActive(false);
            }
        }

        // Start is called before the first frame update
        public void Start()
        {
            Debug.Log("ThreeDText - Start");
            m_HasFired = false;
            m_UpdateCounter = 0;
            m_TransportString = "null";
            var jsonTextFile = Resources.Load<TextAsset>("Text/multiprocess_tests");
            Debug.Log(jsonTextFile);

            var t = GetComponent<TextMesh>();
            t.text = "On Start";
            CommandLineArguments = System.Environment.CommandLine;

            string[] args = System.Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                Debug.Log(arg);
                CommandLineArguments += " " + arg;
            }
            Debug.Log($"CommandLineArguments {CommandLineArguments}");
        }

        // Update is called once per frame
        public void Update()
        {
            var t = GetComponent<TextMesh>();
            t.text = "On Update";
            m_UpdateCounter++;
            var testCoordinator = TestCoordinator.Instance;
            if (testCoordinator == null)
            {
                t.text = t.text + " testCoordinator is null";
                return;
            }

            var transport = NetworkManager.Singleton != null ? NetworkManager.Singleton.NetworkConfig.NetworkTransport : null;
            var transportString = "";
            if (transport == null)
            {
                transportString = "null";
            }
            else
            {
                transportString = transport.ToString();
            }

            t.text += " " + transportString + "\n" +
                testCoordinator.isActiveAndEnabled + "\n" +
                m_TransportString;

            if (IsTestCoordinatorActiveAndEnabled != testCoordinator.isActiveAndEnabled ||
                !m_HasFired ||
                !m_TransportString.Equals(transportString))
            {
                m_HasFired = true;
                m_TransportString = transportString;
                IsTestCoordinatorActiveAndEnabled = testCoordinator.isActiveAndEnabled;
                t.text = $"On Update -\ntestCoordinator.isActiveAndEnabled:{testCoordinator.isActiveAndEnabled}\n" +
                    $"Transport: {transportString}\n" +
                    $"Remote Address: {TestCoordinator.Instance.GetConnectionAddress()}\n" +
                    $"{CommandLineArguments}\n" +
                    $"{m_UpdateCounter}\n";
            }
        }
    }
}
