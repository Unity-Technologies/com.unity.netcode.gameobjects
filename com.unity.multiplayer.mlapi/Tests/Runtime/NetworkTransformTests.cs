using System;
using System.Collections;
using System.Text.RegularExpressions;
using MLAPI.Prototyping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests
{
    public class NetworkTransformTests : BaseMultiInstanceTest
    {
        private NetworkObject m_ClientSideClientPlayer;
        private NetworkObject m_ServerSideClientPlayer;

        [UnitySetUp]
        public new IEnumerator Setup()
        {
            base.Setup();

            yield return StartSomeClientAndServer(nbClients: 1, updatePlayerPrefab: playerPrefab =>
            {
                var networkTransform = playerPrefab.AddComponent<NetworkTransform>();
            });

            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));

            m_ServerSideClientPlayer = serverClientPlayerResult.Result;
            m_ClientSideClientPlayer = clientClientPlayerResult.Result;
        }

        [UnityTest]
        [TestCase(true, ExpectedResult = null)]
        [TestCase(false, ExpectedResult = null)]
        public IEnumerator TestClientAuthoritativeTransformChangeOneAtATime(bool useLocal)
        {
            var clientNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            clientNetworkTransform.UseLocal = useLocal;
            clientNetworkTransform.SetAuthority(NetworkTransform.Authority.Client);

            var serverNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
            serverNetworkTransform.UseLocal = useLocal;
            serverNetworkTransform.SetAuthority(NetworkTransform.Authority.Client);

            // test position
            var playerTransform = m_ClientSideClientPlayer.transform;
            playerTransform.position = new Vector3(10, 20, 30);
            Assert.AreEqual(Vector3.zero, m_ServerSideClientPlayer.transform.position, "server side pos should be zero at first"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_ServerSideClientPlayer.transform.position.x != 0 ));

            Assert.AreEqual(new Vector3(10, 20, 30), m_ServerSideClientPlayer.transform.position, "wrong position on ghost");

            // test rotation
            playerTransform.rotation = Quaternion.Euler(45, 40, 35);
            Assert.AreEqual(Quaternion.identity, m_ServerSideClientPlayer.transform.rotation, "wrong initial value for rotation"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_ServerSideClientPlayer.transform.rotation.eulerAngles.x != 0 ));

            Assert.LessOrEqual(Math.Abs(45 - m_ServerSideClientPlayer.transform.rotation.eulerAngles.x), 0.05f, $"wrong rotation on ghost on x, got {m_ServerSideClientPlayer.transform.rotation.eulerAngles.x}");
            Assert.LessOrEqual(Math.Abs(40 - m_ServerSideClientPlayer.transform.rotation.eulerAngles.y), 0.05f, $"wrong rotation on ghost on y, got {m_ServerSideClientPlayer.transform.rotation.eulerAngles.y}");
            Assert.LessOrEqual(Math.Abs(35 - m_ServerSideClientPlayer.transform.rotation.eulerAngles.z), 0.05f, $"wrong rotation on ghost on z, got {m_ServerSideClientPlayer.transform.rotation.eulerAngles.z}");

            // test scale
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, m_ServerSideClientPlayer.transform.lossyScale.x, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, m_ServerSideClientPlayer.transform.lossyScale.y, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, m_ServerSideClientPlayer.transform.lossyScale.z, "wrong initial value for scale"); // sanity check
            playerTransform.localScale = new Vector3(2, 3, 4);
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_ServerSideClientPlayer.transform.lossyScale.x > 1f ));

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(2f, m_ServerSideClientPlayer.transform.lossyScale.x, "wrong scale on ghost"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(3f, m_ServerSideClientPlayer.transform.lossyScale.y, "wrong scale on ghost"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(4f, m_ServerSideClientPlayer.transform.lossyScale.z, "wrong scale on ghost"); // sanity check

            // test can't change transform with wrong authority
            // todo reparent and test
            // todo add tests for authority
            // todo test all public API
            // test pos and rot change at once
            // test with server vs with host
        }

        [UnityTest]
        [TestCase(NetworkTransform.Authority.Client, ExpectedResult = null)]
        [TestCase(NetworkTransform.Authority.Server, ExpectedResult = null)]
        public IEnumerator TestCantChangeTransformFromOtherSideAuthority(NetworkTransform.Authority authorityToTest)
        {
            // test server can't change client authoritative transform

            var networkTransform = (authorityToTest == NetworkTransform.Authority.Client ? m_ClientSideClientPlayer : m_ServerSideClientPlayer).GetComponent<NetworkTransform>();
            networkTransform.SetAuthority(authorityToTest);

            var otherSideNetworkTransform = (authorityToTest == NetworkTransform.Authority.Client ? m_ServerSideClientPlayer : m_ClientSideClientPlayer).GetComponent<NetworkTransform>();
            otherSideNetworkTransform.SetAuthority(authorityToTest);

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "other side pos should be zero at first"); // sanity check
            otherSideNetworkTransform.transform.position = new Vector3(4, 5, 6);

            yield return new WaitForFixedUpdate(); // wait one frame

            LogAssert.Expect(LogType.Error, new Regex(".*authority.*"));
            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "got authority error, but other side still moved!");
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            yield return base.Teardown();
            UnityEngine.Object.Destroy(m_PlayerPrefab);
        }
    }
}
