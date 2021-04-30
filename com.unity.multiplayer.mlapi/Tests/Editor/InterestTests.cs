using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using NUnit.Framework;
using MLAPI.Connection;
using MLAPI.Configuration;
using MLAPI.Interest;

// hrm, should we be doing NetworkObject or Object?
namespace MLAPI.EditorTests
{

    public class InterestTests
    {
        public class DelegateTester : InterestNode
        {
            public DelegateTester()
            {
                OnSpawn = delegate(in NetworkObject o)
                {
                    SpawnCalled++;
                };
                OnDespawn = delegate(in NetworkObject o)
                {
                    DespawnCalled++;
                };
            }

            public int SpawnCalled = 0;
            public int DespawnCalled = 0;
        }

        public class OddsEvensStorage : InterestObjectStorage
        {
            public OddsEvensStorage()
            {
                m_Odds = ScriptableObject.CreateInstance<BasicInterestStorage>();
                m_Evens = ScriptableObject.CreateInstance<BasicInterestStorage>();
            }

            public override void Query(in NetworkClient client, HashSet<NetworkObject> results)
            {
                // if a client with an odd NetworkObjectID queries, we return objects with odd NetworkObjectIDs
                if (client.PlayerObject.NetworkObjectId % 2 == 0)
                {
                    m_Evens.Query(client, results);
                }
                else
                {
                    m_Odds.Query(client, results);
                }
            }

            public override void UpdateObject(NetworkObject obj)
            {
                m_Odds.RemoveObject(obj);
                m_Evens.RemoveObject(obj);
                AddObject(obj);
            }

            public override void AddObject(NetworkObject obj)
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

            public override void RemoveObject(NetworkObject obj)
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

            public BasicInterestStorage m_Odds;
            public BasicInterestStorage m_Evens;
        }

        private NetworkObject MakeObjectHelper(Vector3 coords, InterestNode comn)
        {
            GameObject o = new GameObject();
            NetworkObject no = (NetworkObject)o.AddComponent(typeof(NetworkObject));
            if (comn != null)
            {
                no.interestNodes.Add(comn);
                no.transform.position = coords;
            }

            return no;
        }

        private NetworkManager SetUpNetworkingManager()
        {
            GameObject o = new GameObject();
            NetworkManager nm = (NetworkManager)o.AddComponent(typeof(NetworkManager));
            nm.NetworkConfig = new NetworkConfig();
            nm.SetSingleton();
            var transport = o.AddComponent<DummyTransport>();
            nm.NetworkConfig.NetworkTransport = transport;
            nm.StartServer();
            return nm;
        }

        [Test]
        // Start is called before the first frame update
        public void AOIDelegateTests()
        {
            var nm = SetUpNetworkingManager();

            var delegateNode = ScriptableObject.CreateInstance<DelegateTester>();
            var someOtherNode = ScriptableObject.CreateInstance<InterestNodeStatic>();

            HashSet<NetworkObject> results = new HashSet<NetworkObject>();
            NetworkClient nc = new NetworkClient()
            {
                ClientId = 1,
            };

            var delegateYes = MakeObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), delegateNode);
            nm.SpawnManager.SpawnNetworkObjectLocally(delegateYes);

            var delegateNo= MakeObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), someOtherNode);
            nm.SpawnManager.SpawnNetworkObjectLocally(delegateNo);
            Assert.True(delegateNode.SpawnCalled == 1);

            nm.SpawnManager.DespawnObject(delegateYes);
            nm.SpawnManager.DespawnObject(delegateNo);
            Assert.True(delegateNode.DespawnCalled == 1);
        }

        [Test]
        // Start is called before the first frame update
        public void AOICustomStorageTests()
        {
            var nm = SetUpNetworkingManager();

            var oddsEvensNode = ScriptableObject.CreateInstance<InterestNode>();
            oddsEvensNode.InterestObjectStorage = new OddsEvensStorage();

            HashSet<NetworkObject> results = new HashSet<NetworkObject>();
            NetworkClient nc = new NetworkClient()
            {
                ClientId = 1,
            };
            nm.InterestManager.QueryFor(nc, results);
            int objectsBeforeAdd = results.Count;

            int numNodes = 4;
            NetworkObject[] nodes = new NetworkObject[4];
            for (int i = 0; i < numNodes; i++)
            {
                nodes[i] = MakeObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), oddsEvensNode);
                nodes[i].NetworkObjectId = (ulong)(i + 100);
                nm.SpawnManager.SpawnNetworkObjectLocally(nodes[i]);
            }

            nc.PlayerObject = MakeObjectHelper(new Vector3(0.0f, 0.0f, 0.0f), null);

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            nm.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2);

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            nm.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2);

            // now re-number the nodes
            for (int i = 0; i < numNodes; i++)
            {
                nodes[i].NetworkObjectId = (ulong) (2*i + 101); // 101, 103, 105, 107
            }

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            nm.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2); // should be unchanged

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            nm.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == numNodes / 2); // should be unchanged

            // now update the nodes
            for (int i = 0; i < numNodes; i++)
            {
                nodes[i].UpdateInterest();
            }

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 100;
            nm.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == 0); // now zero, all are odd

            results.Clear();
            nc.PlayerObject.NetworkObjectId = 101;
            nm.InterestManager.QueryFor(nc, results);
            Assert.True(results.Count - objectsBeforeAdd == nodes.Length); // now 4, all are odd
        }

        [Test]
        // Start is called before the first frame update
        public void InterestRadiusCheck()
        {
            var nm = SetUpNetworkingManager();

            var naiveRadiusNode = ScriptableObject.CreateInstance<InterestNode>();
            var ris = new RadiusInterestStorage();
            ris.radius = 1.5f;
            naiveRadiusNode.InterestObjectStorage = ris;
            var staticNode = ScriptableObject.CreateInstance<InterestNodeStatic>();

            HashSet<NetworkObject> results = new HashSet<NetworkObject>();
            NetworkClient nc = new NetworkClient()
            {
                ClientId = 1,
            };

            nm.InterestManager.QueryFor(nc, results);
            int objectsBeforeAdd = results.Count;

            var ok1 = MakeObjectHelper(new Vector3(0.5f, 0.0f, 0.0f), naiveRadiusNode);
            nm.SpawnManager.SpawnNetworkObjectLocally(ok1);

            var ok2 = MakeObjectHelper(new Vector3(1.0f, 0.0f, 0.0f), naiveRadiusNode);
            nm.SpawnManager.SpawnNetworkObjectLocally(ok2);

            var tooFar = MakeObjectHelper(new Vector3(3.0f, 0.0f, 0.0f), naiveRadiusNode);
            nm.SpawnManager.SpawnNetworkObjectLocally(tooFar);

            var always = MakeObjectHelper(new Vector3(99.0f, 99.0f, 99.0f), staticNode);
            nm.SpawnManager.SpawnNetworkObjectLocally(always);

            nc.PlayerObject = MakeObjectHelper(new Vector3(0.0f,0.0f,0.0f), null);
            nm.SpawnManager.SpawnNetworkObjectLocally(nc.PlayerObject);

            results.Clear();
            nm.InterestManager.QueryFor(nc, results);
            int hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(ok1));
            Assert.True(results.Contains(ok2));
            Assert.True(results.Contains(nc.PlayerObject));
            Assert.True(results.Contains(always));
            Assert.False(results.Contains(tooFar));
            Assert.True(hits == 4);

            // remove an object, should not be in replication manager
            nm.SpawnManager.DespawnObject(always);
            results.Clear();
            nm.InterestManager.QueryFor(nc, results);
            hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(ok1));
            Assert.True(results.Contains(ok2));
            Assert.True(results.Contains(nc.PlayerObject));
            Assert.False(results.Contains(always));
            Assert.False(results.Contains(tooFar));
            Assert.True(hits == 3);
        }

        [Test]
        public void CheckPlainReplication()
        {
            var nm = SetUpNetworkingManager();

            HashSet<NetworkObject> results = new HashSet<NetworkObject>();
            NetworkClient nc = new NetworkClient()
            {
                ClientId = 1,
            };

            nm.InterestManager.QueryFor(nc, results);
            int objectsBeforeAdd = results.Count;

            var object1 = MakeObjectHelper(new Vector3(2.0f, 0.0f, 0.0f), null);
            nm.SpawnManager.SpawnNetworkObjectLocally(object1);

            var object2 = MakeObjectHelper(new Vector3(2.0f, 0.0f, 0.0f), null);
            nm.SpawnManager.SpawnNetworkObjectLocally(object2);

            nc.PlayerObject = MakeObjectHelper(new Vector3(0.0f,0.0f,0.0f), null);
            nm.SpawnManager.SpawnNetworkObjectLocally(nc.PlayerObject);

            results.Clear();
            nm.InterestManager.QueryFor(nc, results);
            int hits = results.Count;
            Debug.Log("I got: " + hits);
            Assert.True(hits == (3 + objectsBeforeAdd));
            Assert.True(results.Contains(object1));
            Assert.True(results.Contains(object2));
            Assert.True(results.Contains(nc.PlayerObject));

            // remove an object, should not be in replication manager
            nm.SpawnManager.DespawnObject(object1);
            nm.SpawnManager.DespawnObject(object2);
            nm.SpawnManager.DespawnObject(nc.PlayerObject);
            results.Clear();
            nm.InterestManager.QueryFor(nc, results);
            hits = results.Count;
            Assert.True(hits == (objectsBeforeAdd));
        }
    }
}
