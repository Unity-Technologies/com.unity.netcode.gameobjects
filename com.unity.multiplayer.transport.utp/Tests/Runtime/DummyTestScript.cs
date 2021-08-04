using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unity.Netcode.UTP.RuntimeTests
{
    public class DummyTestScript
    {
        // A Test behaves as an ordinary method
        [Test]
        public void DummyTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator DummyTestScriptWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
