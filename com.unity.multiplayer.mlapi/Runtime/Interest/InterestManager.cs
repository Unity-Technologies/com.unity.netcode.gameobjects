using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;

namespace MLAPI.Interest
{
   // interest *system* instead of interest node ?
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

        public void AddObject(in NetworkObject newObject)
        {
            var nodes = newObject.InterestNodes;

            // If this new object has no associated Interest Nodes, then we put it in the
            //  default node, which all clients will then get.
            //
            // That is, if you don't opt into the system behavior is the same as before
            //  the Interest system was added
            if (nodes.Count == 0)
            {
                m_DefaultInterestNode.InterestObjectStorage.AddObject(newObject);
            }
            // else add myself to whatever Interest Nodes I am associated with
            else
            {
                foreach (var node in nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }
                    m_ChildNodes.Add(node);
                    node.AddObject(newObject);
                }
            }
        }

        public void RemoveObject(in NetworkObject oldObject)
        {
            var nodes = oldObject.InterestNodes;

            // if the node never had an InterestNode, then it was using the default
            //  interest node
            if (nodes.Count == 0)
            {
                m_DefaultInterestNode.InterestObjectStorage.RemoveObject(oldObject);
            }
            else
            {
                foreach (var node in nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }
                    node.RemoveObject(oldObject);
                }
            }
        }

        public void Dispose()
        {
        }

        private HashSet<InterestNode> m_ChildNodes;
    }
}
