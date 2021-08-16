using System.Collections.Generic;
using MLAPI.Connection;
using MLAPI.Interest;
using UnityEngine;

namespace MLAPI.RuntimeTests
{
    public class RandomInterestKernel : InterestKernel
    {
        public float DropRate = 0.1f;

        public override void QueryFor(in NetworkClient _, in NetworkObject obj, HashSet<NetworkObject> results)
        {
            if (Random.value > DropRate)
            {
                results.Add(obj);
            }
        }
    }
}
