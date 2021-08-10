using System.Collections.Generic;
using MLAPI.Connection;
using MLAPI.Interest;
using UnityEngine;

namespace MLAPI.RuntimeTests.AOI
{
    public class RandomInterestKernel : InterestKernel
    {
        public float DropRate = 0.1f;

        public readonly List<NetworkObject> VisibleObjects = new List<NetworkObject>();

        public override void QueryFor(in NetworkClient client, in NetworkObject obj, HashSet<NetworkObject> results)
        {
            VisibleObjects.Clear();

            if (Random.value > DropRate)
            {
                VisibleObjects.Add(obj);
                results.Add(obj);
            }
        }
    }
}
