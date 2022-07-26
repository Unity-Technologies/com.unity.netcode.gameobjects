using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// Base building block for creating a message. Any struct (or class) that implements INetworkMessage
    /// will automatically be found by the system and all the proper mechanisms for sending and receiving
    /// that message will be hooked up automatically.
    ///
    /// It's generally recommended to implement INetworkMessage types as structs, and define your messages
    /// as close as you can to the network transport format. For messages with no dynamic-length or optional
    /// data, FastBufferWriter allows for serializing the entire struct at once via writer.WriteValue(this)
    ///
    /// In addition to the specified Serialize method, all INetworkMessage types must also have a
    /// static message handler for receiving messages of the following name and signature:
    ///
    /// <code>
    /// public static void Receive(FastBufferReader reader, ref NetworkContext context)
    /// </code>
    ///
    /// It is the responsibility of the Serialize and Receive methods to ensure there is enough buffer space
    /// to perform the serialization/deserialization, either via <see cref="FastBufferWriter.TryBeginWrite"/> and
    /// <see cref="FastBufferReader.TryBeginRead"/>, or via <see cref="FastBufferWriter.WriteValueSafe{T}(T)"/> and
    /// <see cref="FastBufferReader.ReadValueSafe{T}(T)"/>. The former is more efficient when it can be used
    /// for bounds checking for multiple values at once.
    ///
    /// When bandwidth is a bigger concern than CPU usage, values can be packed with <see cref="BytePacker"/>
    /// and <see cref="ByteUnpacker"/>.
    ///
    /// Note that for messages sent using non-fragmenting delivery modes (anything other than
    /// <see cref="NetworkDelivery.ReliableFragmentedSequenced"/>), there is a hard limit of 1300 bytes per message.
    /// With the fragmenting delivery mode, the limit is 64000 bytes per message. If your messages exceed that limit,
    /// you will have to split them into multiple smaller messages.
    ///
    /// Messages are sent with:
    /// <see cref="NetworkManager.SendMessage{T}(T, NetworkDelivery, ulong, bool)"/>
    /// <see cref="NetworkManager.SendMessage{T}(T, NetworkDelivery, ulong*, int, bool)"/>
    /// <see cref="NetworkManager.SendMessage{T, U}(T, NetworkDelivery, U, bool)"/>
    /// <see cref="NetworkManager.SendMessage{T}(T, NetworkDelivery, NativeArray{ulong}, bool)"/>
    /// </summary>
    internal interface INetworkMessage
    {
        void Serialize(FastBufferWriter writer);
        bool Deserialize(FastBufferReader reader, ref NetworkContext context);
        void Handle(ref NetworkContext context);
    }
}
