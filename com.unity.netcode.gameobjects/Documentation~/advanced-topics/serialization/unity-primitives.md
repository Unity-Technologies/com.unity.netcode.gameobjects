# Unity primitives

Unity Primitive `Color`, `Color32`, `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Ray`, `Ray2D` types will be serialized by built-in serialization code.

```csharp
[Rpc(SendTo.ClientsAndHost)]
void BarClientRpc(Color somecolor) { /* ... */ }

void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        BarClientRpc(Color.red); // Server -> Client
    }
}
```
