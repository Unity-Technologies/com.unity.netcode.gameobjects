using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Interest
{
    [CreateAssetMenu(fileName = "ReplicatoinSettings", menuName = "Interest/Settings/InterestSettings", order = 1)]
    public class InterestSettings : ScriptableObject
    {
        public int distanceScaleFactor;
        public int timeStarvationScaleFactor;
    }
}
