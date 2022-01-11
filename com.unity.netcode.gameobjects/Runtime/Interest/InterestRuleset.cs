using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    [CreateAssetMenu(menuName = "Netcode/Interest Ruleset")]
    public class InterestRuleset : ScriptableObject
    {
        public static class PropertyNames
        {
            public const string Kernels = nameof(m_Kernels);
            public const string KernelDataKernel = nameof(KernelData.kernel);
            public const string KernelDataMode = nameof(KernelData.mode);
        }

        enum KernelMode
        {
            Additive,
            Subtractive
        }

        [Serializable]
        class KernelData
        {
            [SerializeField]
            public KernelMode mode;

            [UsedImplicitly, SerializeReference, AssetBasedKernelInstanceProperty]
            public object kernel;
        }

        [SerializeField]
        List<KernelData> m_Kernels = new List<KernelData>();

        public IInterestNode<NetworkObject> ConstructNode()
        {
            var node = new AssetBasedInterestNode();

            foreach (var kernel in m_Kernels)
            {
                if (kernel.mode == KernelMode.Additive)
                {
                    node.AddAdditiveKernel((IInterestKernel<NetworkObject>)kernel.kernel);
                }
                else if(kernel.mode == KernelMode.Subtractive)
                {
                    node.AddSubtractiveKernel((IInterestKernel<NetworkObject>)kernel.kernel);
                }
            }

            return node;
        }
    }
}
