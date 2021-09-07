using UnityEngine;
using NUnit.Framework;

namespace Unity.Netcode.UTP.EditorTests
{
    public class BasicUTPTest : MonoBehaviour
    {
        [Test]
        public void BasicUTPInitializationTest()
        {
            var o = new GameObject();
            var utpTransport = (UTPTransport)o.AddComponent(typeof(UTPTransport));
            utpTransport.Initialize();

            Assert.True(utpTransport.ServerClientId == 0);

            utpTransport.Shutdown();
        }
    }
}
