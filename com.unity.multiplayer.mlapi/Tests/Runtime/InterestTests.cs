using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using MLAPI.Connection;
using MLAPI.Configuration;
using MLAPI.Interest;

namespace MLAPI.RuntimeTests
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

        public class OddEvenInterestKernel : InterestKernel
        {
            public bool IsOdd = true;
            public override void QueryFor(in NetworkClient client, in NetworkObject obj, HashSet<NetworkObject> results)
            {
                if (obj.NetworkObjectId % 2 == 0 ^ IsOdd)
                {
                    results.Add(obj);
                }
            }
        }

        public class OddsEvensNode : InterestNode
        {
            public void OnEnable()
            {
                m_Odds = ScriptableObject.CreateInstance<InterestNodeStatic>();
                m_Evens = ScriptableObject.CreateInstance<InterestNodeStatic>();
            }

            public override void AddObject(in NetworkObject obj)
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

            public override void RemoveObject(in NetworkObject obj)
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

            public override void QueryFor(in NetworkClient client, HashSet<NetworkObject> results)
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

            public override void UpdateObject(in NetworkObject obj)
            {
                m_Odds.RemoveObject(obj);
                m_Evens.RemoveObject(obj);
                AddObject(obj);
            }

            private InterestNodeStatic m_Odds;
            private InterestNodeStatic m_Evens;
        }

//        private NetworkObject MakeGameObjectHelper()
//        {
//            var o = new GameObject();
//            var no = (NetworkObject)o.AddComponent(typeof(NetworkObject));
//            return no;
//        }

        private (NetworkObject, Guid) MakeGameInternalObjectHelper(Vector3 coords, InterestNode comn, string name = "")
        {
            Guid objGuid = NetworkManagerHelper.AddGameNetworkObject(name);
            NetworkObject no = (NetworkObject)NetworkManagerHelper.InstantiatedNetworkObjects[objGuid];

            if (comn != null)
            {
                no.InterestNodes.Add(comn);
                no.transform.position = coords;
            }

            return (no, objGuid);
        }

        private NetworkObject MakeGameInterestObjectHelper(Vector3 coords, InterestNode comn, string name = "")
        {
            Guid objGuid = NetworkManagerHelper.AddGameNetworkObject(name);
            NetworkObject no = (NetworkObject)NetworkManagerHelper.InstantiatedNetworkObjects[objGuid];

            if (comn != null)
            {
                no.InterestNodes.Add(comn);
                no.transform.position = coords;
            }

            return no;
        }

        private NetworkManager SetUpNetworkingManager()
        {
            if (!MultiInstanceHelpers.Create(0, out NetworkManager server, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            server.SetSingleton();
            server.StartServer();
            return server;
        }

        [Test]
        // Start is called before the first frame update
        public void InterestCustomStorageTests()
        {
            var oddsEvensNode = ScriptableObject.CreateInstance<OddsEvensNode>();

            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            var objectsBeforeAdd = results.Count;

            var numNodes = 4;
            var nodes = new NetworkObject[4];
            for (var i = 0; i < numNodes; i++)
            {
                var (thisObj, thisGuid)  = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), oddsEvensNode);
                nodes[i] = thisObj;
                nodes[i].NetworkObjectId = (ulong)(i + 100);
                NetworkManagerHelper.SpawnNetworkObject(thisGuid);
            }

            var (obj, guid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), null);
            nc.PlayerObject = obj;

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2);

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2);

            // now re-number the nodes
            for (var i = 0; i < numNodes; i++)
            {
                nodes[i].NetworkObjectId = (ulong)(2 * i + 101); // 101, 103, 105, 107
            }

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2); // should be unchanged

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2); // should be unchanged

            // now update the nodes
            for (var i = 0; i < numNodes; i++)
            {
                nodes[i].UpdateInterest();
            }

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == 0); // now zero, all are odd

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == nodes.Length); // now 4, all are odd
        }


        [Test]
        // Start is called before the first frame update
        public void InterestRadiusCheck()
        {
            InterestNodeStatic naiveRadiusNode = ScriptableObject.CreateInstance<InterestNodeStatic>();
            var naiveRadiusKernel = ScriptableObject.CreateInstance<RadiusInterestKernel>();
            naiveRadiusKernel.Radius = 1.5f;
            naiveRadiusNode.InterestKernel.Add(naiveRadiusKernel);
            var staticNode = ScriptableObject.CreateInstance<InterestNodeStatic>();

            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };

            var (playerObj, playerGuid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), null);
            nc.PlayerObject = playerObj;

            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            int objectsBeforeAdd = results.Count;

            var (ok1Obj, ok1Guid) = MakeGameInternalObjectHelper(new Vector3(0.5f, 0.0f, 0.0f), naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok1Guid);

            var (ok2Obj, ok2Guid) = MakeGameInternalObjectHelper(new Vector3(1.0f, 0.0f, 0.0f), naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok2Guid);

            var (tooFarObj, tooFarGuid) = MakeGameInternalObjectHelper(new Vector3(3.0f, 0.0f, 0.0f), naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(tooFarGuid);

            var (alwaysObj, alwaysGuid)  = MakeGameInternalObjectHelper(new Vector3(99.0f, 99.0f, 99.0f), staticNode);
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
            NetworkManagerHelper.DespawnNetworkObject(alwaysGuid);
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
            SetUpNetworkingManager();

            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };

            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            var objectsBeforeAdd = results.Count;

            var dualNode = ScriptableObject.CreateInstance<InterestNodeStatic>();
            var oddKernel = ScriptableObject.CreateInstance<OddEvenInterestKernel>();
            oddKernel.IsOdd = true;
            var evenKernel = ScriptableObject.CreateInstance<OddEvenInterestKernel>();
            evenKernel.IsOdd = false;
            dualNode.InterestKernel.Add(oddKernel);
            dualNode.InterestKernel.Add(evenKernel);

            var (object1, object1Guid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), dualNode);
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), dualNode);
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            var (playerObject, playerObjGuid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), dualNode);
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

/*
        [Test]
        public void PerfTest()
        {
            var clock = new HRTClock();
//var duration = TimeSpan.FromSeconds(5);
//var distinctValues = new HashSet<DateTime>();
//var stopWatch = Stopwatch.StartNew();
//
//while (stopWatch.Elapsed < duration)
//{
//    distinctValues.Add(clock.UtcNow);
        }
        */

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

            var (object1, object1Guid) = MakeGameInternalObjectHelper(new Vector3(2.0f, 0.0f, 0.0f), null);
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeGameInternalObjectHelper(new Vector3(2.0f, 0.0f, 0.0f), null);
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            var (playerObj, playerObjGuid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), null);
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
            NetworkManagerHelper.DespawnNetworkObject(object1Guid);
            NetworkManagerHelper.DespawnNetworkObject(object2Guid);
            NetworkManagerHelper.DespawnNetworkObject(playerObjGuid);
            results.Clear();
            NetworkManager.Singleton.InterestManager.QueryFor(nc, results);
            hits = results.Count;
            Assert.True(hits == (objectsBeforeAdd));
        }

        [Test]
        public void TestInterestSettings()
        {
            var rootSettings = ScriptableObject.CreateInstance<TestInterestSettings>();
            rootSettings.SomeSetting = 1;
            NetworkManager.Singleton.InterestSettings = rootSettings;

            var objSettings = ScriptableObject.CreateInstance<TestInterestSettings>();
            objSettings.SomeSetting = 2;
            var (object1Obj, object1Guid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), null);
            object1Obj.InterestSettings = objSettings;

            // no override settings, should receive from NetworkManager
            var (object2Obj, object2Guid) = MakeGameInternalObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), null);

            var checkObj1 = (TestInterestSettings)object1Obj.InterestSettings;
            var checkObj2 = (TestInterestSettings)object2Obj.InterestSettings;

            Assert.True(checkObj1.SomeSetting == 2);
            Assert.True(checkObj2.SomeSetting == 1);
        }
    }
}
