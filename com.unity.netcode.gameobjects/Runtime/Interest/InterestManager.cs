using System.Collections.Generic;
namespace Unity.Netcode.Interest
{
    // interest *system* instead of interest node ?
    public class InterestManager<TClient, TObject> //where TClient //where TObject : unmanaged
    {
        private readonly InterestNodeStatic<TClient, TObject> m_DefaultInterestNode;

        // Trigger the Interest system to do an update sweep on any Interest nodes
        //  I am associated with
        public void UpdateObject(TObject obj)
        {
            List<IInterestNode<TClient, TObject>> nodes;
            if (m_NodesForObject.TryGetValue(obj, out nodes))
            {
                foreach (var node in nodes)
                {
                    node.UpdateObject(obj);
                }
            }
        }

        public InterestManager()
        {
            m_ChildNodes = new HashSet<IInterestNode<TClient, TObject>>();
            m_NodesForObject = new Dictionary<TObject, List<IInterestNode<TClient, TObject>>>();

            // This is the node objects will be added to if no replication group is
            //  specified, which means they always get replicated
            //??ScriptableObject.CreateInstance<InterestNodeStatic<NetworkClient, NetworkObject>>();
            m_DefaultInterestNode = new InterestNodeStatic<TClient, TObject>();
            m_ChildNodes.Add(m_DefaultInterestNode);
        }

        public void AddObject(TObject obj)
        {
            // If this new object has no associated Interest Nodes, then we put it in the
            //  default node, which all clients will then get.
            //
            // That is, if you don't opt into the system behavior is the same as before
            //  the Interest system was added

            List<IInterestNode<TClient, TObject>> nodes;
            if (m_NodesForObject.TryGetValue(obj, out nodes))
            {
                // I am walking through each of the interest nodes that this object has
                //  I should probably optimize for this later vs. doing this for every add!
                foreach (var node in nodes)
                {
                    // cover the case with an empty list entry
                    if (node != null)
                    {
                       // the Interest Manager lazily adds nodes to itself when it sees
                       //  new nodes that associate with the objects being added
                       m_ChildNodes.Add(node);

                       // tell this node to add this object to itself
                       node.AddObject(obj);
                    }
                }
            }
            else
            {
                // else add myself to whatever Interest Nodes I am associated with
                m_DefaultInterestNode.AddObject(obj);
            }
        }

        public void RemoveObject(in TObject oldObject)
        {
            // if the node never had an InterestNode, then it was using the default
            //  interest node
            List<IInterestNode<TClient, TObject>> nodes;
            if (m_NodesForObject.TryGetValue(oldObject, out nodes))
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
            else
            {
                m_DefaultInterestNode.RemoveObject(oldObject);
            }

            RemoveInterestNode(oldObject);
        }

        public void QueryFor(TClient client, HashSet<TObject> results)
        {
            foreach (var c in m_ChildNodes)
            {
                c.QueryFor(client, results);
            }
        }

        public void AddInterestNode(TObject obj, IInterestNode<TClient, TObject> node)
        {
            List<IInterestNode<TClient, TObject>> nodes;
            if (!m_NodesForObject.TryGetValue(obj, out nodes))
            {
                m_NodesForObject[obj] = new List<IInterestNode<TClient, TObject>>();
                m_NodesForObject[obj].Add(node);
            }
        }

        public void RemoveInterestNode(TObject obj)
        {
            if (!m_NodesForObject.ContainsKey(obj))
            {
                m_NodesForObject.Remove(obj);
            }
        }

        public void Dispose()
        {
        }

        private HashSet<IInterestNode<TClient, TObject>> m_ChildNodes;
        private Dictionary<TObject, List<IInterestNode<TClient, TObject>>> m_NodesForObject;
    }
}
