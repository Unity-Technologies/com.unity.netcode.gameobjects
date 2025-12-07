using Unity.Networking.Transport;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>
    /// <para>
    /// This interface allows one to override the creation of the <see cref="NetworkDriver"/> object
    /// that will be used under the hood by <see cref="UnityTransport"/>. This can be useful when
    /// implementing a custom <see cref="INetworkInterface"/> or to add custom pipeline stages to
    /// the default pipelines.
    /// </para>
    /// <para>
    /// To use a custom driver constructor, set <see cref="UnityTransport.s_DriverConstructor"/> to
    /// an instance of an implementation of this interface. This must be done before calling
    /// <see cref="UnityTransport.StartClient"/> or <see cref="UnityTransport.StartServer"/>.
    /// </para>
    /// </summary>
    /// <example>
    /// <para>
    /// This example implements a custom driver constructor that uses the IPC network interface from
    /// the Unity Transport package. This network interface is used for intra-process communications
    /// which can be useful for implementing a single-player version of a game. Since the example is
    /// also preserving all the default settings and pipelines, you'd also benefit from all the
    /// existing features of the transport, like integration with the Network Profiler.
    /// </para>
    /// <code>
    ///     public class IPCDriverConstructor : INetworkStreamDriverConstructor
    ///     {
    ///         public void CreateDriver(
    ///             UnityTransport transport,
    ///             out NetworkDriver driver,
    ///             out NetworkPipeline unreliableFragmentedPipeline,
    ///             out NetworkPipeline unreliableSequencedFragmentedPipeline,
    ///             out NetworkPipeline reliableSequencedPipeline)
    ///         {
    ///             var settings = transport.GetDefaultNetworkSettings();
    ///             driver = NetworkDriver.Create(new IPCNetworkInterface(), settings);
    ///
    ///             transport.GetDefaultPipelineConfigurations(
    ///                 out var unreliableFragmentedPipelineStages,
    ///                 out var unreliableSequencedFragmentedPipelineStages,
    ///                 out var reliableSequencedPipelineStages);
    ///
    ///             unreliableFragmentedPipeline = driver.CreatePipeline(unreliableFragmentedPipelineStages);
    ///             unreliableSequencedFragmentedPipeline = driver.CreatePipeline(unreliableSequencedFragmentedPipelineStages);
    ///             reliableSequencedPipeline = driver.CreatePipeline(reliableSequencedPipelineStages);
    ///         }
    ///     }
    /// </code>
    /// </example>
    public interface INetworkStreamDriverConstructor
    {
        /// <summary>
        /// Creates the <see cref="NetworkDriver"/> instance to be used by the transport.
        /// </summary>
        /// <param name="transport">The transport for which the driver is created.</param>
        /// <param name="driver">The newly-created <see cref="NetworkDriver"/>.</param>
        /// <param name="unreliableFragmentedPipeline">
        /// The driver's pipeline on which to send unreliable traffic. This pipeline must also
        /// support fragmentation (payloads larger than the MTU).
        /// </param>
        /// <param name="unreliableSequencedFragmentedPipeline">
        /// The driver's pipeline on which to send unreliable but sequenced traffic. Traffic sent
        /// on this pipeline must be delivered in the right order, although packet loss is okay.
        /// This pipeline must also support fragmentation (payloads larger than the MTU).
        /// </param>
        /// <param name="reliableSequencedPipeline">
        /// The driver's pipeline on which to send reliable traffic. This pipeline must ensure that
        /// all of its traffic is delivered, and in the correct order too. There is no need for that
        /// pipeline to support fragmentation (<see cref="UnityTransport"/> will handle that).
        /// </param>
        void CreateDriver(
            UnityTransport transport,
            out NetworkDriver driver,
            out NetworkPipeline unreliableFragmentedPipeline,
            out NetworkPipeline unreliableSequencedFragmentedPipeline,
            out NetworkPipeline reliableSequencedPipeline);
    }
}
