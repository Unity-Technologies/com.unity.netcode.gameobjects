using System;
using System.Collections;
using System.Text.RegularExpressions;
using MLAPI.Prototyping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static MLAPI.Prototyping.NetworkTransform;

namespace MLAPI.RuntimeTests
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class NetworkTransformTests : BaseMultiInstanceTest
    {
        private NetworkObject m_ClientSideClientPlayer;
        private NetworkObject m_ServerSideClientPlayer;

        private bool m_TestWithHost;

        public NetworkTransformTests(bool testWithHost)
        {
            m_TestWithHost = testWithHost;
        }

        [UnitySetUp]
        public new IEnumerator Setup()
        {
            base.Setup();

            yield return StartSomeClientAndServer(useHost: m_TestWithHost, nbClients: 1, updatePlayerPrefab: playerPrefab =>
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
        [TestCase(true, Authority.Client, ExpectedResult = null)]
        [TestCase(true, Authority.Server, ExpectedResult = null)]
        [TestCase(false, Authority.Client, ExpectedResult = null)]
        [TestCase(false, Authority.Server, ExpectedResult = null)]
        public IEnumerator TestClientAuthoritativeTransformChangeOneAtATime(bool useLocal, Authority authorityToTest)
        {
            var networkTransform = (authorityToTest == Authority.Client ? m_ClientSideClientPlayer : m_ServerSideClientPlayer).GetComponent<NetworkTransform>();
            networkTransform.UseLocal = useLocal;
            networkTransform.SetAuthority(authorityToTest);

            var otherSideNetworkTransform = (authorityToTest == Authority.Client ? m_ServerSideClientPlayer : m_ClientSideClientPlayer).GetComponent<NetworkTransform>();
            otherSideNetworkTransform.UseLocal = useLocal;
            otherSideNetworkTransform.SetAuthority(authorityToTest);

            // test position
            var playerTransform = networkTransform.transform;
            playerTransform.position = new Vector3(10, 20, 30);
            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "server side pos should be zero at first"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.position.x != 0 ));

            Assert.AreEqual(new Vector3(10, 20, 30), otherSideNetworkTransform.transform.position, "wrong position on ghost");

            // test rotation
            playerTransform.rotation = Quaternion.Euler(45, 40, 35);
            Assert.AreEqual(Quaternion.identity, otherSideNetworkTransform.transform.rotation, "wrong initial value for rotation"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.rotation.eulerAngles.x != 0 ));

            Assert.LessOrEqual(Math.Abs(45 - otherSideNetworkTransform.transform.rotation.eulerAngles.x), 0.05f, $"wrong rotation on ghost on x, got {otherSideNetworkTransform.transform.rotation.eulerAngles.x}");
            Assert.LessOrEqual(Math.Abs(40 - otherSideNetworkTransform.transform.rotation.eulerAngles.y), 0.05f, $"wrong rotation on ghost on y, got {otherSideNetworkTransform.transform.rotation.eulerAngles.y}");
            Assert.LessOrEqual(Math.Abs(35 - otherSideNetworkTransform.transform.rotation.eulerAngles.z), 0.05f, $"wrong rotation on ghost on z, got {otherSideNetworkTransform.transform.rotation.eulerAngles.z}");

            // test scale
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.x, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.y, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.z, "wrong initial value for scale"); // sanity check
            playerTransform.localScale = new Vector3(2, 3, 4);
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.lossyScale.x > 1f ));

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(2f, otherSideNetworkTransform.transform.lossyScale.x, "wrong scale on ghost"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(3f, otherSideNetworkTransform.transform.lossyScale.y, "wrong scale on ghost"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(4f, otherSideNetworkTransform.transform.lossyScale.z, "wrong scale on ghost"); // sanity check

            // todo reparent and test
            // todo test all public API
            // test pos and rot change at once
            // test with server vs with host
        }

        [UnityTest]
        [TestCase(Authority.Client, ExpectedResult = null)]
        [TestCase(Authority.Server, ExpectedResult = null)]
        public IEnumerator TestCantChangeTransformFromOtherSideAuthority(Authority authorityToTest)
        {
            // test server can't change client authoritative transform

            var networkTransform = (authorityToTest == Authority.Client ? m_ClientSideClientPlayer : m_ServerSideClientPlayer).GetComponent<NetworkTransform>();
            networkTransform.SetAuthority(authorityToTest);

            var otherSideNetworkTransform = (authorityToTest == Authority.Client ? m_ServerSideClientPlayer : m_ClientSideClientPlayer).GetComponent<NetworkTransform>();
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
