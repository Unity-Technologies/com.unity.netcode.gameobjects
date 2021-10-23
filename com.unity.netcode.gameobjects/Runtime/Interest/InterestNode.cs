using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    public abstract class InterestNode<TClient, TObject>
    {
        public abstract void QueryFor(TClient client, HashSet<TObject> results);
        public abstract void AddObject(TObject obj);
        public abstract void RemoveObject(TObject obj);
        public abstract void UpdateObject(TObject obj);
    };

    public abstract class InterestKernel<TClient, TObject>
    {
        public abstract void QueryFor(TClient client, TObject obj, HashSet<TObject> results);
    }
}
