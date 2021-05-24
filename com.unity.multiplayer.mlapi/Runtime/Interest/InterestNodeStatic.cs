using UnityEngine;

namespace MLAPI.Interest
{
    public class InterestNodeStatic : InterestNode
    {
        public void OnEnable()
        {
            InterestObjectStorage = CreateInstance<BasicInterestStorage>();
        }
    }
}
