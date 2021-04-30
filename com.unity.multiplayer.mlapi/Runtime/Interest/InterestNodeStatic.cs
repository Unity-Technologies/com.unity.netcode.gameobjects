using System;
using MLAPI;
using UnityEngine;

namespace MLAPI.Interest
{
    [CreateAssetMenu(fileName = "InterestNode", menuName = "Interest/Nodes/ClientObjMapFilterNode", order = 1)]
    [Serializable]
    public class InterestNodeStatic : InterestNode
    {
        public void OnEnable()
        {
            InterestObjectStorage = ScriptableObject.CreateInstance<BasicInterestStorage>();
        }
    }
}
