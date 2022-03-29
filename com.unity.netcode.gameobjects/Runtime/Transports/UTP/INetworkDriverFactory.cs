using System;
using Unity.Networking.Transport;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>
    /// Interface to provide a custom <see cref="NetworkDriver"> to <see cref="UnityTransport">.
    /// </summary>
    /// <remarks>
    /// To provide a custom driver, one should create a class implementing this interface and assign
    /// an instance of it to the <see cref="UnityTransport.NetworkDriverFactory"> field before
    /// establishing connections.
    ///
    /// The default constructor is provided as <see cref="DefaultNetworkDriverFactory">. It is
    /// recommended to subclass it when creating custom driver constructors.
    /// </remarks>
    public interface INetworkDriverFactory
    {
        /// <summary>Create a new <see cref="NetworkDriver"> from the given settings.</summary>
        NetworkDriver CreateDriver(NetworkSettings settings);

        /// <summary>Dispose of a previously-created <see cref="NetworkDriver">.</summary>
        void DisposeDriver(NetworkDriver driver);

        /// <summary>Get the pipeline stages for the unreliable pipeline stage.</summary>
        /// <remarks><see cref="UnityTransport"> expects this pipeline to be fragmented.</remarks>
        Type[] GetUnreliablePipelineStages();

        /// <summary>Get the pipeline stages for the unreliable sequenced pipeline stage.</summary>
        /// <remarks><see cref="UnityTransport"> expects this pipeline to be fragmented.</remarks>
        Type[] GetUnreliableSequencedPipelineStages();

        /// <summary>Get the pipeline stages for the reliable pipeline stage.</summary>
        /// <remarks><see cref="UnityTransport"> expects this pipeline to be unfragmented.</remarks>
        Type[] GetReliablePipelineStages();
    }
}
