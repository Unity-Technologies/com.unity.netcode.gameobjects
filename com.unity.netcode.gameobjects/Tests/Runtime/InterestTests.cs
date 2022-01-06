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
    public class CloakedBehaviour : NetworkBehaviour
    {
        public bool IsCloaked;

        public void Awake()
        {
            IsCloaked = false;
        }
    }

    public class TeamBehaviour : NetworkBehaviour
    {
        public int TeamNumber;
    }

    public class InterestTests
    {
        private NetworkObject m_PlayerNetworkObject;
        private NetworkManager m_NetworkManager;
        private InterestManager<NetworkObject> m_InterestManager;

        [SetUp]
        public void Setup()
        {
            // Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager,
                NetworkManagerHelper.NetworkManagerOperatingMode.Server);

            m_InterestManager = m_NetworkManager.InterestManager;

            Guid thisGuid;
            (m_PlayerNetworkObject, thisGuid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(thisGuid);
        }

        [TearDown]
        public void TearDown()
        {
            // Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        public class TeamKernel : IInterestKernel<NetworkObject>
        {
            public bool QueryFor(NetworkObject client, NetworkObject obj)
            {
                var teamBehaviourMine = client.GetComponent<TeamBehaviour>();
                var teamBehaviourOther = obj.GetComponent<TeamBehaviour>();

                // if the object has a team (and it might; could just be a non-player object) and
                //  is the same team as me, add it
                return teamBehaviourOther && teamBehaviourOther.TeamNumber == teamBehaviourMine.TeamNumber;
            }
        }

        public class CloakedKernel : IInterestKernel<NetworkObject>
        {
            public bool QueryFor(NetworkObject client, NetworkObject obj)
            {
                var cloakedBehaviour = obj.GetComponent<CloakedBehaviour>();
                return cloakedBehaviour.IsCloaked;
            }
        }

        public class OddEvenInterestKernel : IInterestKernel<NetworkObject>
        {
            public bool IsOdd = true;
            public bool QueryFor(NetworkObject client, NetworkObject obj)
            {
                return obj.NetworkObjectId % 2 == 0 ^ IsOdd;
            }
        }

        public class OddsEvensNode : IInterestNode<NetworkObject>
        {
            public OddsEvensNode()
            {
                m_Odds = new InterestNodeStatic<NetworkObject>();
                m_Evens = new InterestNodeStatic<NetworkObject>();
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

            public void AddAdditiveKernel(IInterestKernel<NetworkObject> kernel)
            {
                m_Odds.AddAdditiveKernel(kernel);
                m_Evens.AddAdditiveKernel(kernel);
            }

            public void AddSubtractiveKernel(IInterestKernel<NetworkObject> kernel)
            {
                m_Odds.AddSubtractiveKernel(kernel);
                m_Evens.AddSubtractiveKernel(kernel);
            }

            private InterestNodeStatic<NetworkObject> m_Odds;
            private InterestNodeStatic<NetworkObject> m_Evens;
        }

        // create a game object & add it to the indicated node
        private (NetworkObject, Guid) MakeInterestGameObjectHelper()
        {
            Guid objGuid = NetworkManagerHelper.AddGameNetworkObject("");
            var no = (NetworkObject)NetworkManagerHelper.InstantiatedNetworkObjects[objGuid];
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(no);

            return (no, objGuid);
        }

        // create a game object, initialize its position & add it to the indicated node
        private (NetworkObject, Guid) MakeInterestGameObjectHelper(Vector3 coords)
        {
            Guid objGuid = NetworkManagerHelper.AddGameNetworkObject("");
            var no = (NetworkObject)NetworkManagerHelper.InstantiatedNetworkObjects[objGuid];
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(no);
            no.transform.position = coords;

            return (no, objGuid);
        }

        [Test]
        public void SubtractiveTestBasic()
        {
            var results = new HashSet<NetworkObject>();

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            int objectsBeforeAdd = results.Count;

            var theNode = new InterestNodeStatic<NetworkObject>();
            var radiusKernel = new RadiusInterestKernel(1.5f);
            theNode.AddAdditiveKernel(radiusKernel);
            var cloakKernel = new CloakedKernel();
            theNode.AddSubtractiveKernel(cloakKernel);

            m_PlayerNetworkObject.gameObject.AddComponent<CloakedBehaviour>();
            m_PlayerNetworkObject.gameObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            m_InterestManager.AddInterestNode(ref m_PlayerNetworkObject, theNode);

            var (ok1Obj, ok1Guid) = MakeInterestGameObjectHelper(new Vector3(0.5f, 0.0f, 0.0f));
            ok1Obj.gameObject.AddComponent<CloakedBehaviour>();
            m_InterestManager.AddInterestNode(ref ok1Obj, theNode);
            NetworkManagerHelper.SpawnNetworkObject(ok1Guid);

            var (closeButCloaked, closeButCloakedGuid) = MakeInterestGameObjectHelper(new Vector3(1.0f, 0.0f, 0.0f));
            closeButCloaked.gameObject.AddComponent<CloakedBehaviour>();
            closeButCloaked.gameObject.GetComponent<CloakedBehaviour>().IsCloaked = true;
            m_InterestManager.AddInterestNode(ref closeButCloaked, theNode);
            NetworkManagerHelper.SpawnNetworkObject(closeButCloakedGuid);

            var (tooFarObj, tooFarGuid) = MakeInterestGameObjectHelper(new Vector3(3.0f, 0.0f, 0.0f));
            tooFarObj.gameObject.AddComponent<CloakedBehaviour>();
            m_InterestManager.AddInterestNode(ref tooFarObj, theNode);
            NetworkManagerHelper.SpawnNetworkObject(tooFarGuid);

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(m_PlayerNetworkObject));
            Assert.True(results.Contains(ok1Obj));
            Assert.False(results.Contains(closeButCloaked));
            Assert.False(results.Contains(tooFarObj));
        }

        [Test]
        public void SubtractiveTestOrderMatters()
        {
            var results = new HashSet<NetworkObject>();

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            int objectsBeforeAdd = results.Count;

            // Rule:
            //  add all objects that are in range
            //  but which are not cloaked
            //  except those on my team
            var theNode = new InterestNodeStatic<NetworkObject>();
            var radiusKernel = new RadiusInterestKernel(1.5f);
            theNode.AddAdditiveKernel(radiusKernel);
            theNode.AddSubtractiveKernel(new CloakedKernel());
            theNode.AddAdditiveKernel(new TeamKernel());

            m_PlayerNetworkObject.gameObject.AddComponent<CloakedBehaviour>();
            m_PlayerNetworkObject.gameObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            m_InterestManager.AddInterestNode(ref m_PlayerNetworkObject, theNode);

            var (ok1Obj, ok1Guid) = MakeInterestGameObjectHelper(new Vector3(0.5f, 0.0f, 0.0f));
            ok1Obj.gameObject.AddComponent<CloakedBehaviour>();
            m_InterestManager.AddInterestNode(ref ok1Obj, theNode);
            NetworkManagerHelper.SpawnNetworkObject(ok1Guid);

            var (closeButCloaked, closeButCloakedGuid) = MakeInterestGameObjectHelper(new Vector3(1.0f, 0.0f, 0.0f));
            closeButCloaked.gameObject.AddComponent<CloakedBehaviour>();
            closeButCloaked.gameObject.GetComponent<CloakedBehaviour>().IsCloaked = true;
            m_InterestManager.AddInterestNode(ref closeButCloaked, theNode);
            NetworkManagerHelper.SpawnNetworkObject(closeButCloakedGuid);

            var (tooFarObj, tooFarGuid) = MakeInterestGameObjectHelper(new Vector3(3.0f, 0.0f, 0.0f));
            tooFarObj.gameObject.AddComponent<CloakedBehaviour>();
            m_InterestManager.AddInterestNode(ref tooFarObj, theNode);
            NetworkManagerHelper.SpawnNetworkObject(tooFarGuid);

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var hits = results.Count - objectsBeforeAdd;
            Assert.True(results.Contains(m_PlayerNetworkObject));
            Assert.True(results.Contains(ok1Obj));
            Assert.False(results.Contains(closeButCloaked));
            Assert.False(results.Contains(tooFarObj));
        }

        [Test]
        public void ChangeInterestNodesTest()
        {
            var results = new HashSet<NetworkObject>();

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var objectsBeforeAdd = results.Count;

            var nodeA = new InterestNodeStatic<NetworkObject>();
            nodeA.AddAdditiveKernel(new AddAllInterestKernel());
            nodeA.AddSubtractiveKernel(new CloakedKernel());

            var (objA, objAGuid) = MakeInterestGameObjectHelper();
            objA.gameObject.AddComponent<CloakedBehaviour>();
            m_InterestManager.AddInterestNode(ref objA, nodeA);
            NetworkManagerHelper.SpawnNetworkObject(objAGuid);

            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == 1);

            // cloak nodeA.  It should not show up now
            objA.gameObject.GetComponent<CloakedBehaviour>().IsCloaked = true;
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == 0);

            // uncloak nodeA.  It should be back
            objA.gameObject.GetComponent<CloakedBehaviour>().IsCloaked = false;
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == 1);

            // remove nodeA from objA. now it shouldn't show up; it's orphaned
            m_InterestManager.RemoveInterestNode(ref objA, nodeA);
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == 0);

            // put objA back in the catch-all node.  It should show up unconditionally, even though it's cloaked
            objA.gameObject.GetComponent<CloakedBehaviour>().IsCloaked = true;
            m_InterestManager.AddDefaultInterestNode(objA);
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == 1);
        }

        [Test]
        public void InterestCustomStorageTests()
        {
            var results = new HashSet<NetworkObject>();

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var objectsBeforeAdd = results.Count;
            var oddsEvensNode = new OddsEvensNode();

            var numObjs = 4;
            var objs = new NetworkObject[numObjs];
            for (var i = 0; i < numObjs; i++)
            {
                var (thisObj, thisGuid) = MakeInterestGameObjectHelper();
                thisObj.NetworkObjectId = (ulong)(i + 100);
                m_InterestManager.AddInterestNode(ref thisObj, oddsEvensNode);
                objs[i] = thisObj;
                NetworkManagerHelper.SpawnNetworkObject(thisGuid);
            }

            m_PlayerNetworkObject.NetworkObjectId = 1; // force player to be odd
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2);

            m_PlayerNetworkObject.NetworkObjectId = 2; // force player to be even
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2);

            // now re-number the objects
            for (var i = 0; i < numObjs; i++)
            {
                objs[i].NetworkObjectId = (ulong)(2 * i + 101); // 101, 103, 105, 107
            }

            m_PlayerNetworkObject.NetworkObjectId = 1; // force player to be odd
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);

            // should be unchanged.  Even though now all the objects are odd, because this particular node
            //  just returns pre-sorted objects stored as odd / even, we get the same results before we
            //  re-numbered them
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2);

            // also should be unchanged for the same reason...we still get the pre-computed even results
            m_PlayerNetworkObject.NetworkObjectId = 2; // force player to be even
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            Assert.True(results.Count - objectsBeforeAdd == numObjs / 2); // should be unchanged

            // now update the objs.  We will re-compute which odd / even bucket the objects go into
            for (var i = 0; i < numObjs; i++)
            {
                m_InterestManager.UpdateObject(ref objs[i]);
            }

            m_PlayerNetworkObject.NetworkObjectId = 1; // force player to be odd
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);

            // now that we've re-computed our buckets, a query of 'odd' will return all the elements
            Assert.True(results.Count - objectsBeforeAdd == objs.Length);

            m_PlayerNetworkObject.NetworkObjectId = 2; // force player to be even
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);

            // now that we've re-computed our buckets, a query of 'even ' will return none of the elements
            Assert.True(results.Count - objectsBeforeAdd == 0);
        }

        [Test]
        // Start is called before the first frame update
        public void InterestRadiusCheck()
        {
            var naiveRadiusNode = new InterestNodeStatic<NetworkObject>();
            var naiveRadiusKernel = new RadiusInterestKernel(1.5f);
            naiveRadiusNode.AddAdditiveKernel(naiveRadiusKernel);

            var results = new HashSet<NetworkObject>();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            int objectsBeforeAdd = results.Count - 1; // -1 because we want to count m_PlayerNetworkObject

            m_PlayerNetworkObject.gameObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            m_InterestManager.AddInterestNode(ref m_PlayerNetworkObject, naiveRadiusNode);

            var (ok1Obj, ok1Guid) = MakeInterestGameObjectHelper(new Vector3(0.5f, 0.0f, 0.0f));
            m_InterestManager.AddInterestNode(ref ok1Obj, naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok1Guid);

            var (ok2Obj, ok2Guid) = MakeInterestGameObjectHelper(new Vector3(1.0f, 0.0f, 0.0f));
            m_InterestManager.AddInterestNode(ref ok2Obj, naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(ok2Guid);

            var (tooFarObj, tooFarGuid) = MakeInterestGameObjectHelper(new Vector3(3.0f, 0.0f, 0.0f));
            m_InterestManager.AddInterestNode(ref tooFarObj, naiveRadiusNode);
            NetworkManagerHelper.SpawnNetworkObject(tooFarGuid);

            var (alwaysObj, alwaysGuid) = MakeInterestGameObjectHelper(new Vector3(99.0f, 99.0f, 99.0f));
            NetworkManagerHelper.SpawnNetworkObject(alwaysGuid);

            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
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
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
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

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var objectsBeforeAdd = results.Count;

            var dualNode = new InterestNodeStatic<NetworkObject>();

            var oddKernel = new OddEvenInterestKernel();
            oddKernel.IsOdd = true;

            var evenKernel = new OddEvenInterestKernel();
            evenKernel.IsOdd = false;

            dualNode.AddAdditiveKernel(oddKernel);
            dualNode.AddAdditiveKernel(evenKernel);

            var (object1, object1Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
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
                m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            }

            var tElapsed = clock.UtcNow - t;
            Debug.Log($"Time: {tElapsed} ms");

            var objectsBeforeAdd = results.Count;

            var objsToMakePerNode = 10;
            var nodesToMake = 100;
            var objsToMake = objsToMakePerNode * nodesToMake;
            var nodes = new List<IInterestNode<NetworkObject>>();

            for (var i = 0; i < nodesToMake; ++i)
            {
                var thisNode = new InterestNodeStatic<NetworkObject>();
                nodes.Add(thisNode);
                for (var j = 0; j < objsToMakePerNode; j++)
                {
                    var (obj, guid) = MakeInterestGameObjectHelper();
                    m_InterestManager.AddInterestNode(ref obj, thisNode);
                    NetworkManagerHelper.SpawnNetworkObject(guid);
                }
            }

            var (playerObj, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObj;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);

            // reality check
            var hits = results.Count;
            Debug.Log(hits);
            Assert.True(hits == objsToMake + objectsBeforeAdd + 1);

            // each of the nodes should have returned objsToMakePerNode
            for (var i = 0; i < nodesToMake; ++i)
            {
                var nodeResults = new HashSet<NetworkObject>();
                nodes[i].QueryFor(m_PlayerNetworkObject, nodeResults);  // fix
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

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var objectsBeforeAdd = results.Count;

            var (object1, object1Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeInterestGameObjectHelper();
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            var (playerObj, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObj;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
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
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            hits = results.Count;
            Assert.True(hits == (objectsBeforeAdd));
        }

        [Test]
        public void CheckDisable()
        {
            var results = new HashSet<NetworkObject>();
            var nc = new NetworkClient()
            {
                ClientId = 1,
            };

            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var objectsBeforeAdd = results.Count;

            var node = new InterestNodeStatic<NetworkObject>();
            node.AddAdditiveKernel(new AddAllInterestKernel());
            node.AddSubtractiveKernel(new CloakedKernel());

            var (object1, object1Guid) = MakeInterestGameObjectHelper();
            object1.gameObject.AddComponent<CloakedBehaviour>();
            object1.gameObject.GetComponent<CloakedBehaviour>().IsCloaked = false;
            m_InterestManager.AddInterestNode(ref object1, node);
            NetworkManagerHelper.SpawnNetworkObject(object1Guid);

            var (object2, object2Guid) = MakeInterestGameObjectHelper();
            object2.gameObject.AddComponent<CloakedBehaviour>();
            object2.gameObject.GetComponent<CloakedBehaviour>().IsCloaked = true;
            m_InterestManager.AddInterestNode(ref object2, node);
            NetworkManagerHelper.SpawnNetworkObject(object2Guid);

            var (playerObj, playerObjGuid) = MakeInterestGameObjectHelper();
            nc.PlayerObject = playerObj;
            NetworkManagerHelper.SpawnNetworkObject(playerObjGuid);

            // in normal, "Enabled" node, we should not see node 1 (cloaked), but should see node 2 (uncloaked)
            //  and the player (not in a custom interst node)
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            var hits = results.Count;
//??            Assert.True(hits == (2 + objectsBeforeAdd));
            Assert.True(results.Contains(object1));
            Assert.False(results.Contains(object2));
            Assert.True(results.Contains(nc.PlayerObject));

            // but in "Disabled" node, we should see all the objects
            m_InterestManager.Disable = true;
            results.Clear();
            m_InterestManager.QueryFor(ref m_PlayerNetworkObject, ref results);
            hits = results.Count;
//??            Assert.True(hits == (3 + objectsBeforeAdd));
            Assert.True(results.Contains(object1));
            Assert.True(results.Contains(object2));
            Assert.True(results.Contains(nc.PlayerObject));
        }
    }
}
