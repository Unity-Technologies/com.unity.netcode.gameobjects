using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    public interface IInterestNode<TClient, TObject>
    {
        public void QueryFor(TClient client, HashSet<TObject> results);
        public void AddObject(TObject obj);
        public void RemoveObject(TObject obj);
        public void UpdateObject(TObject obj);
    };

    public abstract class InterestKernel<TClient, TObject>
    {
        public abstract void QueryFor(TClient client, TObject obj, HashSet<TObject> results);
    }
}
