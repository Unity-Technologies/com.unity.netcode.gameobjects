using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace RuntimeTest
{
    public class RuntimeTestRepro
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log("Before Setup");
            yield return null;
            Debug.Log("After Setup");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log("Before Teardown");
            yield return null;
            Debug.Log("After Teardown");
        }

        [UnityTest]
        public IEnumerator RunTest()
        {
            Debug.Log("Before Test");
            yield return null;
            Debug.Log("After Test");
        }
    }
}
