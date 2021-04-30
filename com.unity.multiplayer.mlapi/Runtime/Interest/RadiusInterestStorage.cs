using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.Connection;
using UnityEngine;

namespace MLAPI.Interest
{
    [CreateAssetMenu(fileName = "RadiusStorage", menuName = "Interest/Storage/Radius", order = 1)]
    [Serializable]
    public class RadiusInterestStorage : BasicInterestStorage
    {
        public float radius = 0.0f;
        public override void Query(in NetworkClient client, HashSet<NetworkObject> results)
        {
            foreach (var obj in ManagedObjects)
            {
                if (Vector3.Distance(obj.transform.position, client.PlayerObject.transform.position) < radius)
                {
                    results.Add(obj.GetComponent<NetworkObject>());
                }
            }
        }
    }
}
