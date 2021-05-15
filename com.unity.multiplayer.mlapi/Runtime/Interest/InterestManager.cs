using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;

namespace MLAPI.Interest
{
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

        public void HandleUpdate(NetworkObject updatedObject)
        {
            // allow all the Interest Nodes I am mapped to to have a chance
            //  to update themselves
            foreach (var com in updatedObject.InterestNodes)
            {
                if (com != null)
                {
                    com.UpdateObject(updatedObject);
                }
            }
        }

        public void HandleSpawn(NetworkObject newObject)
        {
            var coms = newObject.InterestNodes;

            // if an object has no Interest Nodes, add it to the default one.  That is,
            //  if you don't opt into the system behavior is the same as before the
            //  Interest system was added
            if (coms.Count == 0)
            {
                m_DefaultInterestNode.InterestObjectStorage.AddObject(newObject);
            }
            // else add myself to whatever Interest Nodes I am associated with
            else
            {
                foreach (var com in coms)
                {
                    if (!(com is null))
                    {
                        AddNode(com);
                        com.AddObject(newObject);

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
            var coms = oldObject.InterestNodes;


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

        public InterestSettings GlobalInterestSettings;

        private HashSet<InterestNode> m_ChildNodes;
    }
}
