using System;
using System.Collections.Generic;
using System.Diagnostics;
using MLAPI.RuntimeTests;
using UnityEngine;
using NUnit.Framework;
using Unity.Netcode.Interest;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode.RuntimeTests
{
    public class TestInterestSettings : InterestSettings
    {
        public int SomeSetting;
    }

    public class InterestTests
    {
        private NetworkObject m_PlayerNetworkObject;
        private NetworkManager m_NetworkManager;
        private InterestManager<NetworkObject, NetworkObject> m_InterestManager;

        [SetUp]
        public void Setup()
        {
            // Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager,
                NetworkManagerHelper.NetworkManagerOperatingMode.Server);

            m_InterestManager = m_NetworkManager.InterestManager;

            Guid thisGuid;
            (m_PlayerNetworkObject, thisGuid)  = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(thisGuid);
        }

        [TearDown]
        public void TearDown()
        {
            // Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        public class OddEvenInterestKernel : InterestKernel<NetworkObject, NetworkObject>
        {
            public bool IsOdd = true;
            public override void QueryFor(NetworkObject client, NetworkObject obj, HashSet<NetworkObject> results)
            {
                if (obj.NetworkObjectId % 2 == 0 ^ IsOdd)
                {
                    results.Add(obj);
                }
            }
        }

        public class OddsEvensNode : IInterestNode<NetworkObject, NetworkObject>
        {
            public OddsEvensNode(NetworkManager nm)
            {
                m_NetworkManager = nm;
                m_Odds = new InterestNodeStatic<NetworkObject, NetworkObject>();
                m_Evens = new InterestNodeStatic<NetworkObject, NetworkObject>();
            }

            public void AddObject(NetworkObject obj)
            {
                if (obj.NetworkObjectId % 2 == 0)
                {
                    m_Evens.AddObject(obj);
                }
                else
                {
                    m_Odds.AddObject(obj);
                }
            }

            public void RemoveObject(NetworkObject obj)
            {
                if (obj.NetworkObjectId % 2 == 0)
                {
                    m_Evens.RemoveObject(obj);
                }
                else
                {
                    m_Odds.RemoveObject(obj);
                }
            }

            public void QueryFor(NetworkObject client, HashSet<NetworkObject> results)
            {
                // if a client with an odd NetworkObjectID queries, we return objects with odd NetworkObjectIDs
                var networkObject = client.GetComponent<NetworkObject>();
                if (networkObject.NetworkObjectId % 2 == 0)
                {
                    m_Evens.QueryFor(client, results);
                }
                else
                {
                    m_Odds.QueryFor(client, results);
                }
            }

            public void UpdateObject(NetworkObject obj)
            {
                m_Odds.RemoveObject(obj);
                m_Evens.RemoveObject(obj);
                AddObject(obj);
            }

            private InterestNodeStatic<NetworkObject, NetworkObject> m_Odds;
            private InterestNodeStatic<NetworkObject, NetworkObject> m_Evens;
            private NetworkManager m_NetworkManager;
        }

        // create a game object & add it to the indicated node
        private (NetworkObject, Guid) MakeInterestGameObjectHelper()
        {
            Guid objGuid = NetworkManagerHelper.AddGameNetworkObject("");
            NetworkObject no = (NetworkObject)NetworkManagerHelper.InstantiatedNetworkObjects[objGuid];
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(no);

            return (no, objGuid);
        }

        // create a game object, initialize its position & add it to the indicated node
        private (NetworkObject, Guid) MakeInterestGameObjectHelper(Vector3 coords)
        {
            Guid objGuid = NetworkManagerHelper.AddGameNetworkObject("");
            NetworkObject no = (NetworkObject)NetworkManagerHelper.InstantiatedNetworkObjects[objGuid];
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(no);
            no.transform.position = coords;

            return (no, objGuid);
        }

        [Test]
        public void InterestCustomStorageTests()
        {
            var results = new HashSet<NetworkObject>();

            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            var objectsBeforeAdd = results.Count;
            var oddsEvensNode = new OddsEvensNode(m_NetworkManager);

            var numObjs = 4;
            var objs = new NetworkObject[numObjs];
            for (var i = 0; i < numObjs; i++)
            {
                var (thisObj, thisGuid)  = MakeInterestGameObjectHelper();
                objs[i] = thisObj;
                m_InterestManager.AddInterestNode(objs[i], oddsEvensNode);
                NetworkManagerHelper.SpawnNetworkObject(thisGuid);
                objs[i].NetworkObjectId = (ulong)(i + 100);
            }

            m_PlayerNetworkObject.NetworkObjectId = 1; // force player to be odd
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2);

            m_PlayerNetworkObject.NetworkObjectId = 2; // force player to be even
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2);

            // now re-number the objects
            for (var i = 0; i < numObjs; i++)
            {
                objs[i].NetworkObjectId = (ulong)(2 * i + 101); // 101, 103, 105, 107
            }

            m_PlayerNetworkObject.NetworkObjectId = 1; // force player to be odd
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2); // should be unchanged

            m_PlayerNetworkObject.NetworkObjectId = 2; // force player to be even
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2); // should be unchanged

            // now update the objs
            for (var i = 0; i < numObjs; i++)
            {
                m_InterestManager.UpdateObject(objs[i]);
            }

            m_PlayerNetworkObject.NetworkObjectId = 1; // force player to be odd
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            Assert.True(results.Count - objectsBeforeAdd == objs.Length); // now 4, all are odd

            m_PlayerNetworkObject.NetworkObjectId = 2; // force player to be even
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            Assert.True(results.Count - objectsBeforeAdd == 0); // now zero, all are odd

        }

        [Test]
        // Start is called before the first frame update
        public void InterestRadiusCheck()
        {
            InterestNodeStatic<NetworkObject, NetworkObject> naiveRadiusNode = new InterestNodeStatic<NetworkObject, NetworkObject>();
            var naiveRadiusKernel = new RadiusInterestKernel(m_NetworkManager);
            naiveRadiusKernel.Radius = 1.5f;
            naiveRadiusNode.InterestKernels.Add(naiveRadiusKernel);

            var results = new HashSet<NetworkObject>();

            var (playerObj, playerGuid) = MakeInterestGameObjectHelper(new Vector3(0.0f, 0.0f, 0.0f));

            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            int objectsBeforeAdd = results.Count;
            m_InterestManager.AddInterestNode(playerObj, naiveRadiusNode);

            var (ok1Obj, ok1Guid) = MakeInterestGameObjectHelper(new Vector3(0.5f, 0.0f, 0.0f));
            m_InterestManager.AddInterestNode(ok1Obj, naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok1Guid);

            var (ok2Obj, ok2Guid) = MakeInterestGameObjectHelper(new Vector3(1.0f, 0.0f, 0.0f));
            m_InterestManager.AddInterestNode(ok2Obj, naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok2Guid);

            var (tooFarObj, tooFarGuid) = MakeInterestGameObjectHelper(new Vector3(3.0f, 0.0f, 0.0f));
            m_InterestManager.AddInterestNode(tooFarObj, naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(tooFarGuid);

            var (alwaysObj, alwaysGuid)  = MakeInterestGameObjectHelper(new Vector3(99.0f, 99.0f, 99.0f));
            NetworkManagerHelper.SpawnNetworkObject(alwaysGuid);

            NetworkManagerHelper.SpawnNetworkObject(playerGuid);

            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            var hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(ok1Obj));
            Assert.True(results.Contains(ok2Obj));
            Assert.True(results.Contains(m_PlayerNetworkObject));
            Assert.True(results.Contains(alwaysObj));
            Assert.False(results.Contains(tooFarObj));
            Assert.True(hits == 4);

            // remove an object, should not be in replication manager
            alwaysObj.Despawn();
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(ok1Obj));
            Assert.True(results.Contains(ok2Obj));
            Assert.True(results.Contains(m_PlayerNetworkObject));
            Assert.False(results.Contains(alwaysObj));
            Assert.False(results.Contains(tooFarObj));
            Assert.True(hits == 3);
        }

        [Test]
        public void CheckMultipleNodes()
        {
            var results = new HashSet<NetworkObject>();

            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            var objectsBeforeAdd = results.Count;

            var dualNode = new InterestNodeStatic<NetworkObject, NetworkObject>();

            var oddKernel = new OddEvenInterestKernel();
            oddKernel.IsOdd = true;

            var evenKernel = new OddEvenInterestKernel();
            evenKernel.IsOdd = false;

            dualNode.InterestKernels.Add(oddKernel);
            dualNode.InterestKernels.Add(evenKernel);

            var (object1, object1Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            var hits = results.Count;
            Assert.True(hits == (2 + objectsBeforeAdd));
            Assert.True(results.Contains(object1));
            Assert.True(results.Contains(object2));
            Assert.True(results.Contains(m_PlayerNetworkObject));
        }

        [Test]
        public void PerfTest()
        {
            var clock = new HRTClock();
            var duration = TimeSpan.FromSeconds(5);
            var distinctValues = new HashSet<DateTime>();
            var stopWatch = Stopwatch.StartNew();

            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };

            var t = clock.UtcNow;
            for (var z = 0; z < 10000; z++)
            {
                m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            }

            var tElapsed = clock.UtcNow - t;
            Debug.Log($"Time: {tElapsed} ms");

            var objectsBeforeAdd = results.Count;

            var objsToMakePerNode = 10;
            var nodesToMake = 100;
            var objsToMake = objsToMakePerNode * nodesToMake;
            List<IInterestNode<NetworkObject, NetworkObject>> nodes = new List<IInterestNode<NetworkObject, NetworkObject>>();
            List<NetworkObject> objs = new List<NetworkObject>();

            for (var i = 0; i < nodesToMake; ++i)
            {
                var thisNode = new InterestNodeStatic<NetworkObject, NetworkObject>();
                nodes.Add(thisNode);
                for (var j = 0; j < objsToMakePerNode; j++)
                {
                    var (obj, guid) = MakeInterestGameObjectHelper();
                    m_InterestManager.AddInterestNode(obj, thisNode);
                    NetworkManagerHelper.SpawnNetworkObject(guid);
                    objs.Add(obj);
                }
            }

            var (playerObj, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObj;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);

            // reality check
            var hits = results.Count;
            Debug.Log(hits);
            Assert.True(hits == objsToMake + objectsBeforeAdd + 1);

            // each of the nodes should have returned objsToMakePerNode
            for (var i = 0; i < nodesToMake; ++i)
            {
                var nodeResults = new HashSet<NetworkObject>();
                nodes[i].QueryFor(m_PlayerNetworkObject, nodeResults);
                Assert.True(nodeResults.Count == objsToMakePerNode);
            }
        }

        [Test]
        public void CheckPlainReplication()
        {
            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };

            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            var objectsBeforeAdd = results.Count;

            var (object1, object1Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            var (playerObj, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObj;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            var hits = results.Count;
            Assert.True(hits == (3 + objectsBeforeAdd));
            Assert.True(results.Contains(object1));
            Assert.True(results.Contains(object2));
            Assert.True(results.Contains(nc.PlayerObject));

            // remove an object, should not be in replication manager
            object1.Despawn();
            object2.Despawn();
            playerObj.Despawn();
            results.Clear();
            m_InterestManager.QueryFor(m_PlayerNetworkObject, results);
            hits = results.Count;
            Assert.True(hits == (objectsBeforeAdd));
        }

        [Test]
        public void TestInterestSettings()
        {
            var rootSettings = new TestInterestSettings();
            rootSettings.SomeSetting = 1;
            m_NetworkManager.InterestSettings = rootSettings;

            var objSettings = new TestInterestSettings();
            objSettings.SomeSetting = 2;
            var (object1Obj, object1Guid) = MakeInterestGameObjectHelper();
            object1Obj.InterestSettings = objSettings;

            // no override settings, should receive from NetworkManager
            var (object2Obj, object2Guid) = MakeInterestGameObjectHelper();

            var checkObj1 = (TestInterestSettings)object1Obj.InterestSettings;
            var checkObj2 = (TestInterestSettings)object2Obj.InterestSettings;

            Assert.True(checkObj1.SomeSetting == 2);
            Assert.True(checkObj2.SomeSetting == 1);
        }
    }
}
