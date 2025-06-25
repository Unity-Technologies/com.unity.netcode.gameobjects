# Custom NetworkVariables

In addition to the standard [`NetworkVariable`s](networkvariable.md) available in Netcode for GameObjects, you can also create custom `NetworkVariable`s for advanced implementations. The `NetworkVariable` and `NetworkList` classes were created as `NetworkVariableBase` class implementation examples. While the `NetworkVariable<T>` class is considered production ready, you might run into scenarios where you have a more advanced implementation in mind. In this case, you can create your own custom implementation.

To create your own `NetworkVariableBase`-derived container, you should:

1. Create a class deriving from `NetworkVariableBase`.
1. Override the following methods:
    - `void WriteField(FastBufferWriter writer)`
    - `void ReadField(FastBufferReader reader)`
    - `void WriteDelta(FastBufferWriter writer)`
    - `void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)`
1. Depending on your custom `NetworkVariableBase` container, you can look at `NetworkVariable<T>` or `NetworkList` to see how those two examples were implemented.

## NetworkVariable serialization

The way you read and write `NetworkVariable`s changes depending on the type you use.

* Known, non-generic types: Use `FastBufferReader.ReadValue` to read from and `FastBufferWriter.WriteValue` to write to the `NetworkVariable` value.
* Integer types:  This type gives you the option to use `BytePacker` and `ByteUnpacker` to compress the `NetworkVariable` value. This process can save bandwidth but adds CPU processing time.
* Generic types: Use serializers that Unity generates based on types discovered during a compile-time code generation process. This means you need to tell Unity's code generation algorithm which types to generate serializers for. To tell Unity which types to serialize, use the following methods:
    * Use `GenerateSerializationForTypeAttribute` to serialize hard-coded types.
    * Use `GenerateSerializationForGenericParameterAttribute` to serialize generic types.

### Serialize a hard-coded type

The following code example uses `GenerateSerializationForTypeAttribute` to generate serialization for a specific hard-coded type:

```csharp
[GenerateSerializationForType(typeof(Foo))]
public class MyNetworkVariableTypeUsingFoo : NetworkVariableBase {}
```

You can call a type that you know the name of with the `FastBufferReader` or `FastBufferWriter` methods. These methods don't work for generic types because the name of the type is unknown.

### Serialize a generic type

The following code example uses `GenerateSerializationForGenericParameterAttribute` to generate serialization for a specific generic parameter in your `NetworkVariable` type:

```csharp
[GenerateSerializationForGenericParameter(0)]
public class MyNetworkVariableType<T> : NetworkVariableBase {}
```

This attribute accepts an integer that indicates which parameter in the type to generate serialization for. This value is 0-indexed, which means that the first type is 0, the second type is 1, and so on.

The following code example places the attribute more than once on one class to generate serialization for multiple types, in this case,`TFirstType` and `TSecondType:

```csharp
[GenerateSerializationForGenericParameter(0)]
[GenerateSerializationForGenericParameter(1)]
public class MyNetworkVariableType<TFirstType, TSecondType> : NetworkVariableBase {}
```


The  `GenerateSerializationForGenericParameterAttribute` and `GenerateSerializationForTypeAttribute` attributes make Unity's code generation create the following methods:

```csharp
NetworkVariableSerialization<T>.Write(FastBufferWriter writer, ref T value);
NetworkVariableSerialization<T>.Read(FastBufferWriter writer, ref T value);
NetworkVariableSerialization<T>.Duplicate(in T value, ref T duplicatedValue);
NetworkVariableSerialization<T>.AreEqual(in T a, in T b);
```

For dynamically-allocated types with a value that isn't `null` (for example, managed types and collections like `NativeArray` and `NativeList`) call `Read` to read the value in the existing object and write data into it directly (in-place). This avoids more allocations.

You can use `AreEqual` to determine if a value is different from the value that `Duplicate` cached. This avoids sending the same value multiple times. You can also use the previous value that `Duplicate` cached to calculate deltas to use in `ReadDelta` and `WriteDelta`.

The type you use must be serializable according to the [support types list above](#supported-types). Each type needs its own serializer instantiated, so this step tells the codegen which types to create serializers for. Unity's code generator assumes that all `NetworkVariable` types exist as fields inside `NetworkBehaviour` types. This means that Unity only inspects fields inside `NetworkBehaviour` types to identify the types to create serializers for.

## Custom NetworkVariable example

This example shows a custom `NetworkVariable` type to help you understand how you might implement such a type. In the current version of Netcode for GameObjects, this example is possible without using a custom `NetworkVariable` type; however, for more complex situations that aren't natively supported, this basic example should help inform you of how to approach the implementation:

 ```csharp
    /// Using MyCustomNetworkVariable within a NetworkBehaviour
    public class TestMyCustomNetworkVariable : NetworkBehaviour
    {
        public MyCustomNetworkVariable CustomNetworkVariable = new MyCustomNetworkVariable();
        public MyCustomGenericNetworkVariable<int> CustomGenericNetworkVariable = new MyCustomGenericNetworkVariable<int>();
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                for (int i = 0; i < 4; i++)
                {
                    var someData = new SomeData();
                    someData.SomeFloatData = (float)i;
                    someData.SomeIntData = i;
                    someData.SomeListOfValues.Add((ulong)i + 1000000);
                    someData.SomeListOfValues.Add((ulong)i + 2000000);
                    someData.SomeListOfValues.Add((ulong)i + 3000000);
                    CustomNetworkVariable.SomeDataToSynchronize.Add(someData);
                    CustomNetworkVariable.SetDirty(true);

                    CustomGenericNetworkVariable.SomeDataToSynchronize.Add(i);
                    CustomGenericNetworkVariable.SetDirty(true);
                }
            }
        }
    }

    /// Bare minimum example of NetworkVariableBase derived class
    [Serializable]
    public class MyCustomNetworkVariable : NetworkVariableBase
    {
        /// Managed list of class instances
        public List<SomeData> SomeDataToSynchronize = new List<SomeData>();

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            // Serialize the data we need to synchronize
            writer.WriteValueSafe(SomeDataToSynchronize.Count);
            foreach (var dataEntry in SomeDataToSynchronize)
            {
                writer.WriteValueSafe(dataEntry.SomeIntData);
                writer.WriteValueSafe(dataEntry.SomeFloatData);
                writer.WriteValueSafe(dataEntry.SomeListOfValues.Count);
                foreach (var valueItem in dataEntry.SomeListOfValues)
                {
                    writer.WriteValueSafe(valueItem);
                }
            }
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            var itemsToUpdate = (int)0;
            reader.ReadValueSafe(out itemsToUpdate);
            SomeDataToSynchronize.Clear();
            for (int i = 0; i < itemsToUpdate; i++)
            {
                var newEntry = new SomeData();
                reader.ReadValueSafe(out newEntry.SomeIntData);
                reader.ReadValueSafe(out newEntry.SomeFloatData);
                var itemsCount = (int)0;
                var tempValue = (ulong)0;
                reader.ReadValueSafe(out itemsCount);
                newEntry.SomeListOfValues.Clear();
                for (int j = 0; j < itemsCount; j++)
                {
                    reader.ReadValueSafe(out tempValue);
                    newEntry.SomeListOfValues.Add(tempValue);
                }
                SomeDataToSynchronize.Add(newEntry);
            }
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // Do nothing for this example
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            // Do nothing for this example
        }
    }    

    /// Bare minimum example of generic NetworkVariableBase derived class
    [Serializable]
    [GenerateSerializationForGenericParameter(0)]
    public class MyCustomGenericNetworkVariable<T> : NetworkVariableBase
    {
        /// Managed list of class instances
        public List<T> SomeDataToSynchronize = new List<T>();

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            // Serialize the data we need to synchronize
            writer.WriteValueSafe(SomeDataToSynchronize.Count);
            for (var i = 0; i < SomeDataToSynchronize.Count; ++i)
            {
                var dataEntry = SomeDataToSynchronize[i];
                // NetworkVariableSerialization<T> is used for serializing generic types
                NetworkVariableSerialization<T>.Write(writer, ref dataEntry);
            }
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            var itemsToUpdate = (int)0;
            reader.ReadValueSafe(out itemsToUpdate);
            SomeDataToSynchronize.Clear();
            for (int i = 0; i < itemsToUpdate; i++)
            {
                T newEntry = default;
                // NetworkVariableSerialization<T> is used for serializing generic types
                NetworkVariableSerialization<T>.Read(reader, ref newEntry);
                SomeDataToSynchronize.Add(newEntry);
            }
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // Do nothing for this example
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            // Do nothing for this example
        }
    }

    /// Example managed class used as the item type in the
    /// MyCustomNetworkVariable.SomeDataToSynchronize list
    [Serializable]
    public class SomeData
    {
        public int SomeIntData = default;
        public float SomeFloatData = default;
        public List<ulong> SomeListOfValues = new List<ulong>();
    }
 ```

While the above example isn't the recommended way to synchronize a list where the number or order of elements in the list often changes, it's an example of how you can define your own rules using `NetworkVariableBase`.

You can test the code above by:
- Using the above code with a project that includes Netcode for GameObjects v1.0 (or higher).
- Adding the `TestMyCustomNetworkVariable` component to an in-scene placed `NetworkObject`.
- Creating a stand alone build and running that as a host or server.
- Running the same scene within the Editor and connecting as a client.
    - Once connected, you can then select the `GameObject` with the attached `NetworkObject` and `TestMyCustomNetworkVariable` components so it appears in the inspector view. There you can verify the `TestMyCustomNetworkVariable.CustomNetworkVariable` property was synchronized with the client (like in the screenshot below):
    ![ScreenShot](../images/MyCustomNetworkVariableInspectorView.png)

> [!NOTE]
> You can't nest `NetworkVariable`s inside other `NetworkVariable` classes. This is because Netcode for GameObjects performs a code generation step to define serialization callbacks for each type it finds in a `NetworkVariable`. The code generation step looks for variables as fields of `NetworkBehaviour` types; it misses any `NetworkVariable`s declared anywhere else.
> Instead of nesting `NetworkVariable`s inside other `NetworkVariable` classes, declare `NetworkVariable` or `NetworkList` properties within the same `NetworkBehaviour` within which you have declared your custom `NetworkVariableBase` implementation.
