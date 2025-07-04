# Fixed strings

Netcode's serialization system natively supports Unity's Fixed String types (`FixedString32`, `FixedString64`, `FixedString128`, `FixedString512`, and `FixedString4096`). The serialization system intelligently understands these fixed string types and ensures that only the amount of the string in use is serialized, even for the larger types. This native support ensures Netcode uses no more bandwidth than is necessary.

```csharp
[Rpc(SendTo.Server)]
void SetPlayerNameServerRpc(FixedString32 playerName) { /* ... */ }

void SetPlayerName(string name)
{
    SetPlayerNameServerRpc(new FixedString32(name));
}
```
