
## Using DTLS encryption

### Generating keys and certificates for DTLS

A potential script to use to generate certificate and keys
```
openssl genrsa -out clientPrivateKeyForRootCA.pem 2048
openssl req -x509 -new -nodes -key clientPrivateKeyForRootCA.pem -sha256 -days 1095 -out myGameClientCA.pem
openssl genrsa -out myGameServerPrivate.pem 2048
openssl req -new -key myGameServerPrivate.pem -out myGameServerCertificateSigningRequest.pem
openssl x509 -req -in myGameServerCertificateSigningRequest.pem -CA myGameClientCA.pem -CAkey clientPrivateKeyForRootCA.pem -CAcreateserial -out myGameServerCertificate.pem -days 365 -sha256
```

Source: https://docs-multiplayer.unity3d.com/transport/current/secure-connection

### Naming

We will refer to
- `myGameServerPrivate.pem` as server key (`ServerSecrets.ServerPrivate`)
- `myGameServerCertificate.pem` as server certificate (`ServerSecrets.ServerCertificate`)
- `myGameClientCA.pem` as client certificate (`ClientSecrets.ClientCertificate`)
- The name of the host we connect to as server common name (`ClientSecrets.ServerCommonName`)

### Setup, on the host or server

(setup)
- Set `UnityTransport.UseEncryption` to true
- Call `SetServerSecrets()` with server certificate and key

(regular flow)
- Call `NetworkManager.StartHost()` or `.StartServer()`

### Setup, on the client
 
(setup)
- Set `UnityTransport.UseEncryption` to true
- Call `SetClientSecrets()` with client certificate and server common name

(regular flow)
- Call `NetworkManager.StartClient()`

### SecureAccessor component

There's a SecureAccessor component that can be use to help the setup. If added to a `NetworkManager`, it will load the certificate from files and set up the Unity Transport during its `Awake()` call. This is optional, but can make the setup easier.

### Security consideration

Client machines should not ship with the Server Certificate and Key. It is the game developers' responsibility to make sure the private server certificates are secured.
