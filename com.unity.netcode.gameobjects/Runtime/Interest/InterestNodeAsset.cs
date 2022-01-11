using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    [CreateAssetMenu(menuName = "Netcode/Interest Node Asset")]
    public class InterestNodeAsset : ScriptableObject
    {
        public static class PropertyNames
        {
            public const string Kernels = nameof(m_Kernels);
            public const string KernelDataKernel = nameof(InterestKernelData.kernel);
            public const string KernelDataMode = nameof(InterestKernelData.mode);
        }

        public enum InterestKernelMode
        {
            Additive,
            Subtractive
        }

        [Serializable]
        public class InterestKernelData
        {
            [SerializeField]
            public InterestKernelMode mode;

            [SerializeReference, AssetBasedKernelInstanceProperty]
            public object kernel;
        }

        [SerializeField]
        List<InterestKernelData> m_Kernels = new List<InterestKernelData>();

        public IInterestNode<NetworkObject> ConstructNode()
        {
            var node = new AssetBasedInterestNode();

            foreach (var kernel in m_Kernels)
            {
                if (kernel.mode == InterestKernelMode.Additive)
                {
                    node.AddAdditiveKernel((IInterestKernel<NetworkObject>)kernel.kernel);
                }
                else if(kernel.mode == InterestKernelMode.Subtractive)
                {
                    node.AddSubtractiveKernel((IInterestKernel<NetworkObject>)kernel.kernel);
                }
            }

            return node;
        }
    }
}
