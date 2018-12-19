---
title: Messaging System
permalink: /wiki/messaging-system/
---

The MLAPI has two parts to it's messaging system. RPC messages and Custom Messages.

### RPC Messages
RPC messages are the most common and easy to use type of message. There are two types of RPC messages. ServerRPC and ClientRPC. ServerRPC methods are invoked by clients (or host if there is no dedicated server) and runs on the Server and ClientRPC methods are invoked by the server but ran on one or more clients.

#### Modes
The RPC methods can be used in two "modes". One is a performance mode where the code is a bit larger but it offers better performance. The other mode is the convenience mode. **The only difference between the two is that the convenience mode boxes all the values on the sender and receiver. If you don't know what that means, use the convenience mode otherwise you are most likley wasting your time.** The performance mode is designed for 100% performance as the MLAPI's goal is to be a general purpose networking library that is not to limit the games capabilities.

#### Ownership Checking
By default, ServerRPC's can only be called if the local client owns the object the ServerRPC sits on. This can be disabled like this:
```csharp
[ServerRPC(RequireOwnership = false)]
void MyMethod(int myInt)
{

}
```

#### Convenience Example
```csharp
private void Update()
{
    if (GUI.Button("SendRandomInt"))
    {
        if (isServer)
        {
            InvokeClientRpcOnEveryone(MyClientRPC, Random.Range(-50, 50));
        }
        else
        {
            InvokeServerRpc(MyServerRpc, Random.Range(-50, 50));
        }
    }
}

[ServerRPC]
private void MyServerRPC(int number)
{
    Debug.Log("The number recieved was: " + number);
    Debug.Log("This method ran on the server upon the request of a client");
}

[ClientRPC]
private void MyClientRPC(int number)
{
    Debug.Log("The number recieved was: " + number);
    Debug.Log("This method ran on the client upon the request of the server");
}
```

#### Custom Type Arguments
Custom types can be sent (Classes or Structs) if they implement the IBitWritable interface.

#### Performance Example
To use the performance mode, the RPC method require the following signature ``void (uint clientId, Stream readStream)`` and the sender is required to use the non generic Stream overload.

```csharp
private void Update()
{
    if (GUI.Button("SendRandomInt"))
    {
        if (isServer)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteInt32Packed(Random.Range(-50, 50));

                InvokeClientRpcOnEveryone(MyClientRPC, stream);
            }
        }
        else
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteInt32Packed(Random.Range(-50, 50));

                InvokeServerRpc(MyServerRpc, stream);
            }
        }
    }
}

[ServerRPC]
private void MyServerRPC(uint clientId, Stream stream) //This signature is REQUIRED for the performance mode
{
    BitReader reader = new BitReader(stream);
    int number = reader.ReadInt32Packed();
    Debug.Log("The number recieved was: " + number);
    Debug.Log("This method ran on the server upon the request of a client");
}

[ClientRPC]
private void MyClientRPC(uint clientId, Stream stream) //This signature is REQUIRED for the performance mode
{
    BitReader reader = new BitReader(stream);
    int number = reader.ReadInt32Packed();
    Debug.Log("The number recieved was: " + number);
    Debug.Log("This method ran on the client upon the request of the server");
}
```


#### Multi project setups
The MLAPI is designed to work with multiple different Unity projects talking to each other. Ex, dedicated server project. One possibility is to create dummy rpc methods on the sender. But it's also possible to replace the method reference with a string containing the name of the method. Like this:

```csharp
InvokeClientRpcOnEveryone(MyClientRPC, Random.Range(-50, 50)); //Instead of this
InvokeClientRpcOnEveryone("MyClientRPC", Random.Range(-50, 50)); //This
```
Both will work the same way, but the bottom one obviously can't do type checking, thus the first one is recommended for single project.

#### Targeting
The Invoke methods can ONLY invoke RPC's that are on the same NetworkedBehaviour instance as the Invoke method. This does not work:
```csharp
InvokeClientRpc(MyOtherNetworkedBehaviour.MyRPCMethod, myInt);
```
Instead, this can be done:
```csharp
MyOtherNetworkedBehaviour.InvokeClientRpc(MyOtherNetworkedBehaviour.MyRPCMethod, myInt);
```

#### Host
When the server has a local client (Host), ServerRPC's can be executed by that client (Even though, technically, he already is the server).

ClientRPC's function the same way. When they are invoked, they are also invoked on the Host. This allows you to write the same code for the host and normal players.

### Custom Messages
If you don't want to use the MLAPI's messaging. You don't have to. You can use a thin layer called "Custom Messages" (these can be used in combinaton with RPC messages aswell). Custom messages allows you to implement your own behaviour and add custom targeting etc.

#### Usage
```csharp
void Start()
{
    //Recieving
    NetworkingManager.singleton.OnIncommingCustomMessage += ((clientId, stream) {
        BitReader reader = new BitReader(stream);
        string message = reader.ReadString(); //Example
    });

    //Sending
    NetworkingManager.singleton.SendCustomMessage(clientId, myStream, "myCustomChannel"); //Channel is optional.
}
```