using UnityEngine;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class ThreeDText : MonoBehaviour
    {
        // Start is called before the first frame update
        public void Start()
        {
            Debug.Log("ThreeDText - Start");
            var t = GetComponent<TextMesh>();
            t.text = "On Start";
        }

        // Update is called once per frame
        public void Update()
        {
            var testCoordinator = TestCoordinator.Instance;
            var t = GetComponent<TextMesh>();
            t.text = $"On Update - testCoordinator.isActiveAndEnabled:{testCoordinator.isActiveAndEnabled}";
            Debug.Log(t.text);
        }
    }
}

