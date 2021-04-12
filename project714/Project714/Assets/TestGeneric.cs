using MLAPI;
using MLAPI.Messaging;

public class TestGeneric<T> : NetworkBehaviour
{
    [ServerRpc]
    void SomeServerRpc()
    {
        print("-");
    }
}
