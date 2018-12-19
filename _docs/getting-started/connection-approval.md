---
title: Connection Approval
permalink: /wiki/connection-approval/
---

During every new connection the MLAPI performs a handshake on top of the one(s) done by the transport. This is to ensure that the NetworkConfig's match up between the Client and Server. In the NetworkConfig you can specify to enable ConnectionApproval. Connection approval will let you decide on a per connection basis if the connection should be allowed.

However, when ConnectionApproval is true you are also required to provide a callback where you put your approval logic inside. (Server only) Example:
```csharp
private void Setup() 
{
    NetworkingManager.singleton.ConnectionApprovalCallback = ApprovalCheck;
    NetworkingManager.singleton.StartHost();
}

private void ApprovalCheck(byte[] connectionData, uint clientId, MLAPI.NetworkingManager.ConnectionApprovedDelegate callback)
{
    //Your logic here
    bool approve = true;

    int prefabId = SpawnManager.GetNetworkedPrefabIndexOfName("myPrefabName"); // The prefab index. Use -1 to use the default player prefab.
    
    //If approve is true, the connection gets added. If it's false. The client gets disconnected
    callback(clientId, prefabId, approve, positionToSpawnAt, rotationToSpawnWith);
}
```
### Connection data
The connectionData parameter is any custom data of your choice that the client should send to the server. Usually, this should be some sort of ticket, room password or similar that will decide if a connection should be approved or not. The connectionData is specified on the Client side in the NetworkingConfig supplied when connecting. Example:
```csharp
NetworkingManager.singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("room password");
NetworkingManager.singleton.StartClient();
```
The ConnectionData will then be passed to the server and it will decide if the client will be approved or not.

#### Security of connection data
If Encryption is enabled, the connection handshake with the buffer will be encrypted AND authenticated. (AES-256 encryption and HMAC-SHA-256 authentication). Please note that if the key exchange is not signed, a man in the middle attack can be done.