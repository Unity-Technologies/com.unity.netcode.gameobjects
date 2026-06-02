# Arrays and native containers

It's recommended to read the [serialization overview](./serialization/serialization-overview.md) to better understand this documentation.

Netcode for GameObjects has built-in serialization code for arrays of [C# value-type primitives](cprimitives.md), like `int[]`, and [Unity primitive types](unity-primitives.md). Any arrays of types that aren't handled by the built-in serialization code, such as custom types, need to be handled using a container class or structure that implements the [`INetworkSerializable`](inetworkserializable.md) interface.

## Performance considerations

Sending arrays and strings over the network has performance implications. An array incurs a garbage collected allocation, and a string also incurs a garbage collected allocation, so an array of strings results in an allocation for every element in the array, plus one more for the array itself.

For this reason, arrays of strings (`string[]`) aren't supported by the built-in serialization code. Instead, it's recommended to use `NativeArray<FixedString*>` or `NativeList<FixedString*>`, because they're more efficient and don't incur garbage collected memory allocation. Refer to [`NativeArray<T>`](#nativearrayt) and [`NativeList<T>`](#nativelistt) below for more details.

## Built-in primitive types example

Using built-in primitive types is fairly straightforward:

```csharp
[Rpc(SendTo.Server)]
void HelloServerRpc(int[] scores, Color[] colors) { /* ... */ }
```

## Native containers

Netcode for GameObjects supports `NativeArray` and `NativeList` native containers with built-in serialization, RPCs, and NetworkVariables. However, you can't nest either of these containers without causing a crash.

A few examples of nesting that will cause a crash:

* `NativeArray<NativeList<T>>`
* `NativeList<NativeArray<T>>`
* `NativeArray<NativeArray<T>>`
* `NativeList<NativeList<T>>`

### `NativeArray<T>`

To serialize a `NativeArray` container, use `serializer.SerializeValue(ref Array)`.

### `NativeList<T>`

To serialize a `NativeList` container, you must:
1. Ensure your assemblies reference `Collections`.
2. You must add `UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT` to your **Scriptiong Define Symbols** list.
   1. From the Unity Editor top bar menu, go to **Edit** > **Project Settings...** > **Player**.
   2. Select the **Other Settings** dropdown.
   3. Scroll to **Script Compilation** > **Scripting Define Symbols**.
   4. Add `UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT`.
3. Use `serializer.SerializeValue(ref List)` as your serialization syntax.

> [!NOTE]
> When using `NativeLists` within `INetworkSerializable`, the list `ref` value must be a valid, initialized `NativeList`.
> NetworkVariables are similar that the value must be initialized before it can receive updates. For example, `public NetworkVariable<NativeList<byte>> ByteListVar = new NetworkVariable<NativeList<byte>>{Value = new NativeList<byte>(Allocator.Persistent)};`. RPCs do this automatically.

## Generic collections

For performance reasons, Netcode for GameObjects does not have built-in serialization code for C# generic collections. However, `NetworkVariable` does support [synchronizing generic collection types](../../basics/networkvariable.md#using-collections-with-networkvariables).
