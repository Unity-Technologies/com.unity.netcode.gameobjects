using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    public class InterestNodeStatic <TObject> : IInterestNode<TObject>
    {
        public List<IInterestKernel<TObject>> InterestKernels = new List<IInterestKernel<TObject>>();

        // these are the objects under my purview
        protected HashSet<TObject> ManagedObjects;

        public InterestNodeStatic()
        {
            ManagedObjects = new HashSet<TObject>();
        }

        public void AddObject(TObject obj)
        {
            ManagedObjects.Add(obj);
        }

        public void RemoveObject(TObject obj)
        {
            ManagedObjects.Remove(obj);
        }

        public void QueryFor(TObject client, HashSet<TObject> results)
        {
            if (InterestKernels.Count > 0)
            {
                foreach (var obj in ManagedObjects)
                {
                    foreach (var ik in InterestKernels)
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

        public void UpdateObject(TObject obj)
        {
        }
    }
}
