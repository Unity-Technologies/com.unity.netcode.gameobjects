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

        public override void AddObject(NetworkObject obj)
        {
            ManagedObjects.Add(obj);
        }

        public override void RemoveObject(NetworkObject obj)
        {
            ManagedObjects.Remove(obj);
        }

        public override void Query(in NetworkClient client, HashSet<NetworkObject> results)
        {
            results.UnionWith(ManagedObjects);
        }

        public override void UpdateObject(NetworkObject obj)
        {
        }

        public BasicInterestStorage()
        {
            ManagedObjects = new HashSet<NetworkObject>();
        }
    }
}
