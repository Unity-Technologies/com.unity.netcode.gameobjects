using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;
using UnityEngine.Serialization;

namespace MLAPI.Interest
{
    public abstract class InterestObjectStorage : ScriptableObject
    {
        public abstract void Query(in NetworkClient client, HashSet<NetworkObject> results);
        public abstract void AddObject(NetworkObject obj);
        public abstract void RemoveObject(NetworkObject obj);
        public abstract void UpdateObject(NetworkObject obj);
    };

    public interface IInterestHandler
    {
        public void QueryFor(in NetworkClient client, HashSet<NetworkObject> results);
        public void AddObject(in NetworkObject obj);
        public void RemoveObject(in NetworkObject obj);
    }

    [CreateAssetMenu(fileName = "InterestNode", menuName = "Interest/Nodes/InterestNode", order = 1)]
    [Serializable]
    public class InterestNode : ScriptableObject, IInterestHandler
    {
        public InterestNode()
        {
            m_ChildNodes = new HashSet<InterestNode>();
        }

        public InterestObjectStorage InterestObjectStorage;

        public void AddObject(in NetworkObject obj)
        {
            InterestObjectStorage?.AddObject(obj);
        }

        public void RemoveObject(in NetworkObject obj)
        {
            InterestObjectStorage?.RemoveObject(obj);
        }

        public void UpdateObject(in NetworkObject obj)
        {
            InterestObjectStorage?.UpdateObject(obj);
        }

        // externally-called object query function.
        //  The passed-in hash set will contain the results.
        public void QueryFor(in NetworkClient client, HashSet<NetworkObject> results)
        {
            InterestObjectStorage?.Query(client, results);

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
                return;
            }
            m_ChildNodes.Add(newNode);
        }

        private HashSet<InterestNode> m_ChildNodes;
    }
}
