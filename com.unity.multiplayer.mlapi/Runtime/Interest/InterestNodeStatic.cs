using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;

namespace MLAPI.Interest
{
    public class InterestNodeStatic : InterestNode
    {
        public List<InterestKernel> InterestKernel = new List<InterestKernel>();

        public void OnEnable()
        {
            ManagedObjects = new HashSet<NetworkObject>();
        }

        // these are the objects under my purview
        protected HashSet<NetworkObject> ManagedObjects;

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
            if (InterestKernel.Count > 0)
            {
                foreach (var obj in ManagedObjects)
                {
                    foreach (var ik in InterestKernel)
                    {
                        ik.QueryFor(client, obj, results);
                    }
                }
            }
            else
            {
                results.UnionWith(ManagedObjects);
            }
        }

        public override void UpdateObject(in NetworkObject obj)
        {
        }
    }
}
