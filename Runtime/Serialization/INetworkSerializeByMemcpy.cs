
namespace Unity.Netcode
{
    /// <summary>
    /// This interface is a "tag" that can be applied to a struct to mark that struct as being serializable
    /// by memcpy. It's up to the developer of the struct to analyze the struct's contents and ensure it
    /// is actually serializable by memcpy. This requires all of the members of the struct to be
    /// `unmanaged` Plain-Old-Data values - if your struct contains a pointer (or a type that contains a pointer,
    /// like `NativeList&lt;T&gt;`), it should be serialized via `INetworkSerializable` or via
    /// `FastBufferReader`/`FastBufferWriter` extension methods.
    /// </summary>
    public interface INetworkSerializeByMemcpy
    {
    }
}
