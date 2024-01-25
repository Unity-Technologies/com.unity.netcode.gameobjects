using UnityEngine;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class ThreeDText : MonoBehaviour
    {
        public bool IsTestCoordinatorActiveAndEnabled = false;
        public string CommandLineArguments = "";
        private long m_UpdateCounter;
        private bool m_HasFired;
        private string m_TransportString;

        public void Awake()
        {
            if (MultiprocessOrchestration.IsPerformanceTest)
            {
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
                if (arg.Length > 15)
                {
                    CommandLineArguments += " " + arg.Substring(0, 14);
                }
                else
                {
                    CommandLineArguments += "\n" + arg;
                }
            }
        }

        // Update is called once per frame
        public void Update()
        {
            m_UpdateCounter++;
            var testCoordinator = TestCoordinator.Instance;
            if (testCoordinator == null)
            {
                return;
            }

            var transport = NetworkManager.Singleton?.NetworkConfig.NetworkTransport;
            var transportString = "";
            if (transport == null)
            {
                transportString = "null";
            }
            else
            {
                transportString = transport.ToString();
            }

            var t = GetComponent<TextMesh>();

            if (IsTestCoordinatorActiveAndEnabled != testCoordinator.isActiveAndEnabled ||
                !m_HasFired ||
                m_UpdateCounter % 25 == 0 ||
                !m_TransportString.Equals(transportString))
            {
                m_HasFired = true;
                m_TransportString = transportString;
                IsTestCoordinatorActiveAndEnabled = testCoordinator.isActiveAndEnabled;
                t.text = $"On Update -\ntestCoordinator.isActiveAndEnabled:{testCoordinator.isActiveAndEnabled} {testCoordinator.ConfigurationType}\n" +
                    $"Transport: {transportString}\n" +
                    $"{CommandLineArguments}\n" +
                    $"IsHost: {NetworkManager.Singleton.IsHost} IsClient: {NetworkManager.Singleton.IsClient} {NetworkManager.Singleton.IsConnectedClient}\n" +
                    $"{m_UpdateCounter}\n";
            }
        }
    }
}
