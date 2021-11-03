using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    public interface IInterestNode<TObject>
    {
        public void QueryFor(TObject client, HashSet<TObject> results);
        public void AddObject(TObject obj);
        public void RemoveObject(TObject obj);
        public void UpdateObject(TObject obj);
    };

    public interface IInterestKernel<TObject>
    {
        public void QueryFor(TObject client, TObject obj, HashSet<TObject> results);
    }
}
