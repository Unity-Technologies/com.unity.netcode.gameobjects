# C# primitives

C# primitive types are serialized by built-in serialization code. These types include `bool`, `char`, `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, and `string`.

You can also serialize [arrays](serialization-arrays.md) of C# primitive types, with the exception of arrays of `strings` (`string[]`) for [performance reasons](serialization-arrays.md#performance-considerations).

```csharp
[Rpc(SendTo.Server)]
void FooServerRpc(int somenumber, string sometext) { /* ... */ }

void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        FooServerRpc(Time.frameCount, "hello, world"); // Client -> Server
    }
}
```
