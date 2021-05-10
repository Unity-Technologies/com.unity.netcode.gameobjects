using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Interest
{
    [CreateAssetMenu(fileName = "ReplicatoinSettings", menuName = "Interest/Settings/ReplicationSettings", order = 1)]
    public class ReplicationSettings : ScriptableObject
    {
        public int distanceScaleFactor;
        public int timeStarvationScaleFactor;
    }
}
