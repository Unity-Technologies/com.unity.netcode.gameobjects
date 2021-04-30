// Although this is (currently) inside the MLAPI package, it is intentionally
//  totally decoupled from MLAPI with the intention of allowing it to live
//  in its own package

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MLAPI.Connection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

// every single node has 1 routing group (?)
//  so why not require each node to specify its routing 'thing' at init time
//  and guarantee uniqueness?
//  and this is also where the parent ReplicationGraph owner goes in
//  so long as we don't let you change this after setup (I think)

namespace MLAPI.Interest
{

    public class ReplicationSettings
    {
        public long LastReplTime = 0;
        public int PriortyScale = 1;
    }



    public class InterestManager
    {
        private readonly InterestNodeStatic m_DefaultInterestNode;

        public InterestManager()
        {
            m_ChildNodes = new HashSet<InterestNode>();

            // This is the node objects will be added to if no replication group is
            //  specified, which means they always get replicated
            m_DefaultInterestNode = ScriptableObject.CreateInstance<InterestNodeStatic>();
            AddNode(m_DefaultInterestNode);
        }

        public void QueryFor(in NetworkClient client, HashSet<NetworkObject> results)
        {
            foreach (var c in m_ChildNodes)
            {
                c.QueryFor(client, results);
            }
        }

        // Add a new child node.  Currently, there is no way to remove a node
        public void AddNode(InterestNode newNode)
        {
            if (m_ChildNodes.Contains(newNode))
            {
                return; // Node already registered
            }
            m_ChildNodes.Add(newNode);
        }

        public void HandleUpdate(NetworkObject newObject)
        {
            // add myself to whatever client object nodes I am in
            var coms = newObject.interestNodes;

            // if an object has no mapping node, add it to the default one.  That is,
            //  if you don't opt into the system you always show in the results
            foreach (var com in coms)
            {
                if (com != null)
                {
                    com.UpdateObject(newObject);
                }
            }
        }

        public void HandleSpawn(NetworkObject newObject)
        {
            // add myself to whatever client object nodes I am in
            var coms = newObject.interestNodes;

            // if an object has no mapping node, add it to the default one.  That is,
            //  if you don't opt into the system you always show in the results
            if (coms.Count == 0)
            {
                m_DefaultInterestNode.InterestObjectStorage.AddObject(newObject);
            }
            else
            {
                foreach (var com in coms)
                {
                    if (com != null)
                    {
                        AddNode(com);
                        com.AddObject(newObject);

                        // hrm, which goes first?
                        if (com.OnSpawn != null)
                        {
                            com.OnSpawn(newObject);
                        }
                    }
                }
            }
        }

        public void HandleDespawn(NetworkObject oldObject)
        {
            var coms = oldObject.interestNodes;


            // if an object has no mapping node, add it to the default one.  That is,
            //  if you don't opt into the system you always show in the results
            if (coms.Count == 0)
            {
                m_DefaultInterestNode.InterestObjectStorage.RemoveObject(oldObject);
            }
            else
            {
                foreach (var com in coms)
                {
                    com.RemoveObject(oldObject);

                    // hrm, which goes first?
                    if (com.OnDespawn != null)
                    {
                        com.OnDespawn(oldObject);
                    }
                }
            }
        }

        public ReplicationSettings GlobalReplicationSettings; // ugh

        private HashSet<InterestNode> m_ChildNodes;
    }
}
