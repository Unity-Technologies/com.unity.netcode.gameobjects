using System;
using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;

namespace MLAPI.Interest
{
    [Serializable]
    [CreateAssetMenu(fileName = "Storable", menuName = "AOI/BasicInterestStorage", order = 1)]
    public class BasicInterestStorage : InterestObjectStorage
    {
        // these are the objects under my purview
        //  so if I have a dynamic query, I will check these.
        //  But if I don't have a dynamic query, I will return these (I think)?
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
