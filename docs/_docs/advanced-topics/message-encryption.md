---
title: Message Encryption
permalink: /wiki/message-encryption/
---

### Inner Workings
If Encryption is enabled in the NetworkConfig, a ECDHE Keyexchange will take place to establish a shared key, unique for every client and session. If SignKeyExchange is also enabled, the Server will provide it's SSL certificate for the client to use. This is essentially a clone of the TLS handshake and validation.

The certificate validation method can be changed with a delegate in the CryptographyHelper, but it defaults to the default .NET validation and checks that the hostname is valid or is "127.0.0.1", note that the certificate only has to be set on the server and only PFX formats is supported. The PFX Base64 string has to include the private key. 

If you want to use custom encryption or just need a shared secret between the server and client, you can grab the key that the MLAPI got in the handshake key-exchange from the CryptographyHelper.

### Encrypted and/or Authenticated RPC
Since encryption can be quite intimidating for many new programmers. The MLAPI makes it super easy to encrypt and authenticate your rpc messages. This is the most common way of using encryption in the MLAPI.

When sending RPC's you can set an optional security flag. This will decide whether your message is encrypted, authenticated or both.

```csharp
// For the examples below the channel is null, this will use the default channel

// Plain text
InvokeServerRPC(MyRpcMethod, myRpcMethodParam, null, SecuritySendFlags.None);
// Encrypted, IV appended
InvokeServerRPC(MyRpcMethod, myRpcMethodParam, null, SecuritySendFlags.Encrypted);
// Authenticated
InvokeServerRPC(MyRpcMethod, myRpcMethodParam, null, SecuritySendFlags.Authenticated);
// Encrypted, then the encrypted version is authenticated. Including the IV
InvokeServerRPC(MyRpcMethod, myRpcMethodParam, null, SecuritySendFlags.Encrypted | SecuritySendFlags.Authenticated);
```

