using System.Collections.Generic;

namespace Unity.Netcode.Interest
{
    // interest *system* instead of interest node ?
    internal class InterestManager<TObject>
    {
        private InterestNodeStatic<TObject> m_DefaultInterestNode = new InterestNodeStatic<TObject>();

        // Trigger the Interest system to do an update sweep on any Interest nodes
        //  I am associated with
        public void UpdateObject(ref TObject obj)
        {
            if (m_InterestNodesMap.TryGetValue(obj, out var nodes))
            {
                foreach (var node in nodes)
                {
                    node.UpdateObject(obj);
                }
            }
        }

        public InterestManager()
        {
            // This is the node objects will be added to if no replication group is
            //  specified, which means they always get replicated
            m_ChildNodes = new HashSet<IInterestNode<TObject>> { m_DefaultInterestNode };
        }

        public void AddObject(ref TObject obj)
        {
            // If this new object has no associated Interest Nodes, then we put it in the
            //  default node, which all clients will then get.
            //
            // That is, if you don't opt into the system behavior is the same as before
            //  the Interest system was added

            if (m_InterestNodesMap.TryGetValue(obj, out var nodes))
            {
                // I am walking through each of the interest nodes that this object has
                foreach (var node in nodes)
                {
                    // the Interest Manager lazily adds nodes to itself when it sees
                    //  new nodes that associate with the objects being added
                    m_ChildNodes.Add(node);
                }
            }
            else
            {
                // if the object doesn't have any nodes, we assign it to the default node
                AddDefaultInterestNode(obj);
            }
        }

        public void AddDefaultInterestNode(TObject obj)
        {
            AddInterestNode(ref obj, m_DefaultInterestNode);
        }

        public void RemoveObject(ref TObject obj)
        {
            if (m_InterestNodesMap.TryGetValue(obj, out var nodes))
            {
                foreach (var node in nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    node.RemoveObject(obj);
                }

                m_InterestNodesMap.Remove(obj);
            }
        }

        public void QueryFor(ref TObject client, ref HashSet<TObject> results)
        {
            foreach (var c in m_ChildNodes)
            {
                c.QueryFor(client, results);
            }
        }

        public void AddInterestNode(ref TObject obj, IInterestNode<TObject> node)
        {
            node.AddObject(obj);

            if (!m_InterestNodesMap.TryGetValue(obj, out var nodes))
            {
                m_InterestNodesMap[obj] = new List<IInterestNode<TObject>>();
                m_InterestNodesMap[obj].Add(node);
            }
            else
            {
                if (!nodes.Contains(node))
                {
                    nodes.Add(node);
                }
            }
        }

        public void RemoveInterestNode(ref TObject obj, IInterestNode<TObject> node)
        {
            node.RemoveObject(obj);
            if (m_InterestNodesMap.TryGetValue(obj, out var nodes))
            {
                if (nodes.Contains(node))
                {
                    nodes.Remove(node);
                }
            }
        }

        private HashSet<IInterestNode<TObject>> m_ChildNodes;

        private Dictionary<TObject, List<IInterestNode<TObject>>> m_InterestNodesMap =
            new Dictionary<TObject, List<IInterestNode<TObject>>>();
    }
}
