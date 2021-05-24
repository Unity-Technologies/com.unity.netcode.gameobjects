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

    // maybe seal this class
    [CreateAssetMenu(fileName = "InterestNode", menuName = "Interest/Nodes/InterestNode", order = 1)]
    [Serializable]
    public class InterestNode : ScriptableObject
    {
        // set this delegate if you want a function called when
        //  object 'obj' is being spawned / de-spawned
        public delegate void SpawnDelegate(in NetworkObject obj);

        public SpawnDelegate OnSpawn;
        public SpawnDelegate OnDespawn;

        public InterestObjectStorage InterestObjectStorage;

        public void AddObject(NetworkObject obj)
        {
            InterestObjectStorage?.AddObject(obj);
        }
        public void RemoveObject(NetworkObject obj)
        {
            InterestObjectStorage?.RemoveObject(obj);
        }
        public void UpdateObject(NetworkObject obj)
        {
            InterestObjectStorage?.UpdateObject(obj);
        }

        public InterestNode()
        {
            ChildNodes = new List<InterestNode>();
        }

        // externally-called object query function.
        //  The passed-in hash set will contain the results.
        public void QueryFor(in NetworkClient client, HashSet<NetworkObject> results)
        {
            InterestObjectStorage?.Query(client, results);

            foreach (var c in ChildNodes)
            {
                c.QueryFor(client, results);
            }
        }

        // Called when a given object is about to be (de)spawned.  The OnDespawn
        //  delegate gives each node a chance to do its own handling (e.g. removing
        //  the object from a cache)
        public void HandleSpawn(in NetworkObject obj)
        {
            if (OnSpawn.Target != null)
            {
                OnSpawn(in obj);
            }

            foreach (var c in ChildNodes)
            {
                c.HandleSpawn(in obj);
            }
        }

        public void HandleDespawn(in NetworkObject obj)
        {
            if (OnDespawn.Target != null)
            {
                OnDespawn(in obj);
            }

            foreach (var c in ChildNodes)
            {
                c.HandleDespawn(in obj);
            }
        }

        // Add a new child node.  Currently, there is no way to remove a node
        public void AddNode(InterestNode newNode)
        {
            if (ChildNodes.Contains(newNode))
            {
                return;
            }
            ChildNodes.Add(newNode);
        }

        public List<InterestNode> ChildNodes;
    }
}
