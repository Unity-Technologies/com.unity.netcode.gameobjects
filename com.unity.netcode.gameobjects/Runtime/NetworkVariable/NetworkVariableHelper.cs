namespace Unity.Netcode
{
    public class NetworkVariableHelper
    {
        // This is called by ILPP during module initialization for all unmanaged INetworkSerializable types
        // This sets up NetworkVariable so that it properly calls NetworkSerialize() when wrapping an INetworkSerializable value
        //
        // The reason this is done is to avoid runtime reflection and boxing in NetworkVariable - without this,
        // NetworkVariable would need to do a `var is INetworkSerializable` check, and then cast to INetworkSerializable,
        // *both* of which would cause a boxing allocation. Alternatively, NetworkVariable could have been split into
        // NetworkVariable and NetworkSerializableVariable or something like that, which would have caused a poor
        // user experience and an API that's easier to get wrong than right. This is a bit ugly on the implementation
        // side, but it gets the best achievable user experience and performance.
        //
        // RuntimeAccessModifiersILPP will make this `public`
        internal static void InitializeDelegates<T>() where T : unmanaged, INetworkSerializable
        {
            NetworkVariable<T>.Write = NetworkVariable<T>.WriteNetworkSerializable;
            NetworkVariable<T>.Read = NetworkVariable<T>.ReadNetworkSerializable;
        }
    }
}
