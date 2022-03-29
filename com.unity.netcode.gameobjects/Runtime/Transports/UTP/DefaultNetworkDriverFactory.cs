using System;
using Unity.Networking.Transport;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>Default implementation of <see cref="INetworkDriverFactory">.</summary>
    public class DefaultNetworkDriverFactory : INetworkDriverFactory
    {
        /// <summary>Create a new <see cref="NetworkDriver"> from the given settings.</summary>
        public NetworkDriver CreateDriver(NetworkSettings settings)
        {
            return NetworkDriver.Create(settings);
        }

        /// <summary>Dispose of a previously-created <see cref="NetworkDriver">.</summary>
        public void DisposeDriver(NetworkDriver driver)
        {
            driver.Dispose();
        }

        /// <summary>Get the pipeline stages for the unreliable pipeline stage.</summary>
        /// <remarks>
        /// The stages include the fragmentation stage and all stages required to get the tools and
        /// network simulator working. If subclassing, it is recommended to add any other custom
        /// stages before <see cref="SimulatorPipelineStage">.
        /// </remarks>
        public Type[] GetUnreliablePipelineStages()
        {
            return new Type[]
            {
                typeof(FragmentationPipelineStage)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , typeof(SimulatorPipelineStage)
                , typeof(SimulatorPipelineStageInSend)
#endif
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                , typeof(NetworkMetricsPipelineStage)
#endif
            };
        }

        /// <summary>Get the pipeline stages for the unreliable sequenced pipeline stage.</summary>
        /// <remarks>
        /// The stages include the fragmentation stage and all stages required to get the tools and
        /// network simulator working. If subclassing, it is recommended to add any other custom
        /// stages before <see cref="SimulatorPipelineStage">.
        /// </remarks>
        public Type[] GetUnreliableSequencedPipelineStages()
        {
            return new Type[]
            {
                typeof(FragmentationPipelineStage),
                typeof(UnreliableSequencedPipelineStage)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , typeof(SimulatorPipelineStage)
                , typeof(SimulatorPipelineStageInSend)
#endif
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                , typeof(NetworkMetricsPipelineStage)
#endif
            };
        }

        /// <summary>Get the pipeline stages for the reliable pipeline stage.</summary>
        /// <remarks>
        /// Note that <see cref="UnityTransport"> expects this pipeline to be unfragmented
        /// (fragmentation is implemented directly in <see cref="UnityTransport"> for reliable
        /// traffic). If subclassing, it is recommended to add any other custom stages before
        /// <see cref="SimulatorPipelineStage">.
        /// </remarks>
        public Type[] GetReliablePipelineStages()
        {
            return new Type[]
            {
                typeof(ReliableSequencedPipelineStage)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , typeof(SimulatorPipelineStage)
                , typeof(SimulatorPipelineStageInSend)
#endif
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                , typeof(NetworkMetricsPipelineStage)
#endif
            };
        }
    }
}
