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

        [SetUp]
        public void Setup()
        {
            // Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out NetworkManager networkManager,
                NetworkManagerHelper.NetworkManagerOperatingMode.Server);
        }

        [TearDown]
        public void TearDown()
        {
            // Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        public class OddEvenInterestKernel : InterestKernel<NetworkClient, NetworkObject>
        {
            public bool IsOdd = true;
            public override void QueryFor(NetworkClient client, NetworkObject obj, HashSet<NetworkObject> results)
            {
                if (obj.NetworkObjectId % 2 == 0 ^ IsOdd)
                {
                    results.Add(obj);
                }
            }
        }

        public class OddsEvensNode : IInterestNode<NetworkClient, NetworkObject>
        {
            public OddsEvensNode()
            {
                m_Odds = new InterestNodeStatic<NetworkClient, NetworkObject>();
                m_Evens = new InterestNodeStatic<NetworkClient, NetworkObject>();
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

            public void QueryFor(NetworkClient client, HashSet<NetworkObject> results)
            {
                // if a client with an odd NetworkObjectID queries, we return objects with odd NetworkObjectIDs
                if (client.PlayerObject.NetworkObjectId % 2 == 0)
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

            private InterestNodeStatic<NetworkClient, NetworkObject> m_Odds;
            private InterestNodeStatic<NetworkClient, NetworkObject> m_Evens;
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
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };
            var interestManager = NetworkManager.Singleton.InterestManager;
            interestManager.QueryFor(nc, results);
            var objectsBeforeAdd = results.Count;
            var oddsEvensNode = new OddsEvensNode();

            var numObjs = 4;
            var objs = new NetworkObject[numObjs];
            for (var i = 0; i < numObjs; i++)
            {
                var (thisObj, thisGuid)  = MakeInterestGameObjectHelper();
                objs[i] = thisObj;
                objs[i].NetworkObjectId = (ulong)(i + 100);
                objs[i].AddInterestNode(oddsEvensNode);
                NetworkManagerHelper.SpawnNetworkObject(thisGuid);
            }

            var (obj, guid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = obj;

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            interestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2);

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            interestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2);

            // now re-number the objects
            for (var i = 0; i < numObjs; i++)
            {
                objs[i].NetworkObjectId = (ulong)(2 * i + 101); // 101, 103, 105, 107
            }

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            interestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2); // should be unchanged

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            interestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2); // should be unchanged

            // now update the objs
            for (var i = 0; i < numObjs; i++)
            {
                interestManager.UpdateObject(objs[i]);
            }

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            interestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == 0); // now zero, all are odd

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            interestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == objs.Length); // now 4, all are odd
        }

        [Test]
        // Start is called before the first frame update
        public void InterestRadiusCheck()
        {
            InterestNodeStatic<NetworkClient, NetworkObject> naiveRadiusNode = new InterestNodeStatic<NetworkClient, NetworkObject>();
            var naiveRadiusKernel = new RadiusInterestKernel();
            naiveRadiusKernel.Radius = 1.5f;
            naiveRadiusNode.InterestKernels.Add(naiveRadiusKernel);
//            interestManager.RegisterInterestNode(k_Prefab1, naiveRadiusNode);

            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };

            var (playerObj, playerGuid) = MakeInterestGameObjectHelper(new Vector3(0.0f, 0.0f, 0.0f));
            nc.PlayerObject = playerObj;

            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            int objectsBeforeAdd = results.Count;
            playerObj.AddInterestNode(naiveRadiusNode);

            var (ok1Obj, ok1Guid) = MakeInterestGameObjectHelper(new Vector3(0.5f, 0.0f, 0.0f));
            ok1Obj.AddInterestNode(naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok1Guid);

            var (ok2Obj, ok2Guid) = MakeInterestGameObjectHelper(new Vector3(1.0f, 0.0f, 0.0f));
            ok2Obj.AddInterestNode(naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok2Guid);

            var (tooFarObj, tooFarGuid) = MakeInterestGameObjectHelper(new Vector3(3.0f, 0.0f, 0.0f));
            tooFarObj.AddInterestNode(naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(tooFarGuid);

            var (alwaysObj, alwaysGuid)  = MakeInterestGameObjectHelper(new Vector3(99.0f, 99.0f, 99.0f));
            NetworkManagerHelper.SpawnNetworkObject(alwaysGuid);

            NetworkManagerHelper.SpawnNetworkObject(playerGuid);

            results.Clear();
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            var hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(ok1Obj));
            Assert.True(results.Contains(ok2Obj));
            Assert.True(results.Contains(nc.PlayerObject));
            Assert.True(results.Contains(alwaysObj));
            Assert.False(results.Contains(tooFarObj));
            Assert.True(hits == 4);

            // remove an object, should not be in replication manager
            alwaysObj.Despawn();
            results.Clear();
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(ok1Obj));
            Assert.True(results.Contains(ok2Obj));
            Assert.True(results.Contains(nc.PlayerObject));
            Assert.False(results.Contains(alwaysObj));
            Assert.False(results.Contains(tooFarObj));
            Assert.True(hits == 3);
        }

        [Test]
        public void CheckMultipleNodes()
        {
            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };

            var interestManager = NetworkManager.Singleton.InterestManager;
            interestManager.QueryFor(nc, results);
            var objectsBeforeAdd = results.Count;

            var dualNode = new InterestNodeStatic<NetworkClient, NetworkObject>();
//!!            interestManager.RegisterInterestNode(k_Prefab1, dualNode);

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

            var (playerObject, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObject;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            results.Clear();
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            var hits = results.Count;
            Assert.True(hits == (3 + objectsBeforeAdd));
            Assert.True(results.Contains(object1));
            Assert.True(results.Contains(object2));
            Assert.True(results.Contains(nc.PlayerObject));
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
                NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            }

            var tElapsed = clock.UtcNow - t;
            Debug.Log($"Time: {tElapsed} ms");

            var objectsBeforeAdd = results.Count;

            var objsToMakePerNode = 10;
            var nodesToMake = 100;
            var objsToMake = objsToMakePerNode * nodesToMake;
            List<IInterestNode<NetworkClient, NetworkObject>> nodes = new List<IInterestNode<NetworkClient, NetworkObject>>();
            List<NetworkObject> objs = new List<NetworkObject>();

            for (var i = 0; i < nodesToMake; ++i)
            {
                var thisNode = new InterestNodeStatic<NetworkClient, NetworkObject>();
                nodes.Add(thisNode);
                for (var j = 0; j < objsToMakePerNode; j++)
                {
                    var (obj, guid) = MakeInterestGameObjectHelper();
                    obj.AddInterestNode(thisNode);
                    NetworkManagerHelper.SpawnNetworkObject(guid);
                    objs.Add(obj);
                }
            }

            var (playerObj, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObj;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            results.Clear();
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);

            // reality check
            var hits = results.Count;
            Debug.Log(hits);
            Assert.True(hits == objsToMake + objectsBeforeAdd + 1);

            // each of the nodes should have returned objsToMakePerNode
            for (var i = 0; i < nodesToMake; ++i)
            {
                var nodeResults = new HashSet<NetworkObject>();
                nodes[i].QueryFor(nc, nodeResults);
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

            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            var objectsBeforeAdd = results.Count;

            var (object1, object1Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            var (playerObj, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObj;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            results.Clear();
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
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
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            hits = results.Count;
            Assert.True(hits == (objectsBeforeAdd));
        }

        [Test]
        public void TestInterestSettings()
        {
            var rootSettings = new TestInterestSettings();
            rootSettings.SomeSetting = 1;
            NetworkManager.Singleton.InterestSettings = rootSettings;

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
