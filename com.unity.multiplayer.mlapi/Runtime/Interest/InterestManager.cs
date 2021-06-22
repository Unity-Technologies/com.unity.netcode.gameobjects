using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;

namespace MLAPI.Interest
{
    public class InterestManager : IInterestHandler
    {
        private readonly InterestNodeStatic m_DefaultInterestNode;

        public InterestManager()
        {
            m_ChildNodes = new HashSet<InterestNode>();

            // This is the node objects will be added to if no replication group is
            //  specified, which means they always get replicated
            m_DefaultInterestNode = ScriptableObject.CreateInstance<InterestNodeStatic>();
            m_ChildNodes.Add(m_DefaultInterestNode);
        }

        public void QueryFor(in NetworkClient client, HashSet<NetworkObject> results)
        {
            foreach (var c in m_ChildNodes)
            {
                c.QueryFor(client, results);
            }
        }

        public void HandleSpawn(in NetworkObject newObject)
        {
            var node = newObject.InterestNode;

            // if an object has no Interest Nodes, add it to the default one.  That is,
            //  if you don't opt into the system behavior is the same as before the
            //  Interest system was added
            if (node == null)
            {
                m_DefaultInterestNode.InterestObjectStorage.AddObject(newObject);
            }
            // else add myself to whatever Interest Nodes I am associated with
            else
            {
                if (!(node is null))
                {
                    m_ChildNodes.Add(node);
                    node.AddObject(newObject);

                    if (node.OnSpawn != null)
                    {
                        node.OnSpawn(newObject);
                    }
                }
            }
        }

        public void HandleDespawn(in NetworkObject oldObject)
        {
            var node = oldObject.InterestNode;

            // if the node never had an InterestNode, then it was using the default
            //  interest node
            if (node == null)
            {
                m_DefaultInterestNode.InterestObjectStorage.RemoveObject(oldObject);
            }
            else
            {
                node.RemoveObject(oldObject);

                if (node.OnDespawn != null)
                {
                    node.OnDespawn(oldObject);
                }
            }
        }

        public void Dispose()
        {
        }

        private HashSet<InterestNode> m_ChildNodes;
    }
}
