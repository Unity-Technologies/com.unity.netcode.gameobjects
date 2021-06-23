using System;
using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;

namespace MLAPI.Interest
{
    [Serializable]
    [CreateAssetMenu(fileName = "BasicStorage", menuName = "Interest/Storage/Basic", order = 1)]
    public class BasicInterestStorage : InterestObjectStorage
    {
        // these are the objects under my purview
        public HashSet<NetworkObject> ManagedObjects;

        public override void AddObject(in NetworkObject obj)
        {
            ManagedObjects.Add(obj);
        }

        public override void RemoveObject(in NetworkObject obj)
        {
            ManagedObjects.Remove(obj);
        }

        public override void QueryFor(in NetworkClient client, HashSet<NetworkObject> results)
        {
            results.UnionWith(ManagedObjects);
        }

        public override void UpdateObject(in NetworkObject obj)
        {
        }

        public BasicInterestStorage()
        {
            ManagedObjects = new HashSet<NetworkObject>();
        }
    }
}
