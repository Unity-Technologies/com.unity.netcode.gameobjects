using Unity.Netcode;
using UnityEngine;

#if !UNITY_SERVER
public class Stripped : NetworkBehaviour
{
    // Adding netvar here is important, no repro without the netvar
    NetworkVariable<int> asdf = new();

    void FixedUpdate()
    {
        if (IsServer)
        {
            asdf.Value += 1;
        }
    }
}
#endif