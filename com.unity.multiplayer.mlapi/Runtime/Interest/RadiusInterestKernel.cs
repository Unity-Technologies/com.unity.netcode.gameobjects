using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.Connection;
using MLAPI.Interest;
using UnityEngine;

namespace MLAPI
{
    [CreateAssetMenu(fileName = "RadiusInterestKernel", menuName = "Interest/Kernels/Radius", order = 1)]
    [Serializable]
    public class RadiusInterestKernel : InterestKernel
    {
        public float Radius = 0.0f;
        public override void QueryFor(in NetworkClient client, in NetworkObject obj, HashSet<NetworkObject> results)
        {
            if (Vector3.Distance(obj.transform.position, client.PlayerObject.transform.position) <= Radius)
            {
                results.Add(obj.GetComponent<NetworkObject>());
            }
        }
    }
}
