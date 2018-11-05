---
title: Message Encryption
permalink: /wiki/message-encryption/
---

If Encryption is enabled in the NetworkConfig, a ECDHE Keyexchange will take place to establish a shared key, unique for every client and session. If SignKeyExchange is also enabled, the Server will provide it's SSL certificate for the client to use. This is essentially a clone of the TLS handshake and validation.

The certificate validation method can be changed with a delegate in the CryptographyHelper, but it defaults to the default .NET validation and checks that the hostname is valid or is "127.0.0.1", note that the certificate only has to be set on the server and only PFX formats is supported. The PFX Base64 string has to include the private key. 

If you want to use custom encryption or just need a shared secret between the server and client, you can grab the key that the MLAPI got in the handshake key-exchange from the CryptographyHelper.