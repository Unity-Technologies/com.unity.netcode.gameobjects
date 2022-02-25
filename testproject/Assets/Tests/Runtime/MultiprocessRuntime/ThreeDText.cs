using UnityEngine;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class ThreeDText : MonoBehaviour
    {
        public bool IsTestCoordinatorActiveAndEnabled = false;
        public string CommandLineArguments = "";
        // Start is called before the first frame update
        public void Start()
        {
            Debug.Log("ThreeDText - Start");

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
            var testCoordinator = TestCoordinator.Instance;
            var t = GetComponent<TextMesh>();
            if (IsTestCoordinatorActiveAndEnabled != testCoordinator.isActiveAndEnabled)
            {
                t.text = $"On Update -\ntestCoordinator.isActiveAndEnabled:{testCoordinator.isActiveAndEnabled}\n{CommandLineArguments}\n{MultiprocessLogHandler.TestEndpoint()}";
                Debug.Log(t.text);
                MultiprocessLogger.Log(t.text);
                IsTestCoordinatorActiveAndEnabled = testCoordinator.isActiveAndEnabled;
                Debug.Log(MultiprocessLogHandler.ReportQueue());
                Debug.Log(MultiprocessLogHandler.Flush());
            }
        }
    }
}

