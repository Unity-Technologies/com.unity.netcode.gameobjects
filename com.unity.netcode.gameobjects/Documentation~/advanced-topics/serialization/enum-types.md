# Enum types

A user-defined enum type will be serialized by built-in serialization code (with underlying integer type).

```csharp
enum SmallEnum : byte
{
    A,
    B,
    C
}

enum NormalEnum // default -> int
{
    X,
    Y,
    Z
}

[Rpc(SendTo.Server)]
void ConfigServerRpc(SmallEnum smallEnum, NormalEnum normalEnum) { /* ... */ }

void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        ConfigServerRpc(SmallEnum.A, NormalEnum.X); // Client -> Server
    }
}
```
