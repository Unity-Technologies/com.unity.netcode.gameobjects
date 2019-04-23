---
title: Message Encryption
permalink: /wiki/message-encryption/
---

Because cryptography is a topic not many developers are familiar with. It will be explained in two parts, one "beginner friendly" and one highly technical explanation.

### The Beginners Version
If you enable Encryption in the NetworkConfig on the NetworkingManager, the server will agree to a key with each client that connects. This key can then be used to encrypt and authenticate messages. If someone is trying to break the encryption, they can do a MITM (man in the middle) attack, and neither party will know that someone is eavesdropping on the conversation.

To prevent MITM attacks, you need to enable the "Sign Key Exchange" option. When this option is enabled, you also need to supply a certificate and a private key. To get started, follow the instructions found [here](https://cert.midlevel.io/) for really easy steps. Note that this only works for dedicated servers, you cannot secure a communication between two clients (Player hosted).

### Advanced Explanation
If you enable Encryption in the NetworkConfig on the NetworkingManager, the server will always do a ECDHE implementation with every client to negotiate a key that is then stretched with PBKDF2.

To prevent MITM attacks, you need to enable "Sign Key Exchange" and supply a private key and a certificate, encoded as a PFX file in base64. The PFX Base64 string has to include the private key. This should only be done on the server. You can then add custom validation code to the clients for your certificate.

The certificate validation method can be changed with a delegate in the CryptographyHelper, but it defaults to the default .NET validation and checks that the hostname is valid or is "127.0.0.1".

If you want to use custom encryption or just need a shared secret between the server and client, you can grab the stretched key that the MLAPI got in the handshake key-exchange from the CryptographyHelper.

For quick development certificates, you can go [here](https://cert.midlevel.io/) to get one.

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

