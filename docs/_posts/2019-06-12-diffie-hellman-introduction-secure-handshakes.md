---
layout: post
title:  "Diffie Hellman Introduction: Secure Handshakes"
date:   2019-06-12 10:00:00
author: TwoTen
---

Let's talk about cryptography, it's really hard, complicated and easy to get wrong. Right?

No. I'm not saying it's always easy, but it's not as hard as most developers think if you understand it and use prebuilt libraries (which you should when possible). In this article, I'm going to introduce you to all the cryptographic topics you need to add cryptography to your networked games. I will be using the C# library MLAPI.Cryptography and .NET as examples for this article. Let's get started.

### Encryption
Let's start with the most common cryptographic operation, encryption. There are two main types of encryption. **Symmetric** and **Asymmetric**. 

Symmetric is the easiest to understand, if you have the text "hello", and encrypt it with our imaginary symmetric encryption algorithm called "symcrypt" using the key "mySecretKey" you might get "jOnMgPiWyK". Just nonsense, if you then decrypt that string with the same key, you would get the string "hello" again. Great, now we know how basic encryption works, but this is not very helpful to us. We have a server and a client, they the same key. How do we share it? We can't just send it in plain text otherwise someone could steal it in transit. Instead, we use a "key exchange" algorithm.


### Key Exchange
A key exchange algorithm is an algorithm for two parties to agree to a shared key without ever sending the key. Which means if a party records all network traffic on a network and thus has all the messages of the key exchange, they cannot recreate the key later on. The most common algorithm is called "Diffie Hellman". It has many variations, one of them being "EC Diffie Hellman", aka "Elliptic Curve Diffie Hellman" which is much faster but operates on the same principle. Let's understand how to use Diffie Hellman. We are going to use the MLAPI.Cryptography implementation.

##### Diffie Hellman Usage
```csharp
public class Server
{
    private ECDiffieHellman serverDiffieHellman;
    private byte[] secretKey;

    // Pretend this method is called when a client connects.
    public void OnClientConnect()
    {
        // Server creates a diffie hellman instance. This will initialize with random keys internally.
        serverDiffieHellman = new ECDiffieHellman();

        // The server will get the "public key". This key has to be sent to the client.
        // The client will then use it to construct a shared key.
        byte[] serverPublic = serverDiffieHellman.GetPublicKey();

        // Send a hail message. This is where you actually send the public key. 
        // How this method looks depends on your network library of course.
        // This is just an example.
        SendBytesToClient("hail", serverPublic);
    }

    // Pretend this is called when the client sends a hail-response.
    public void OnHailResponse(byte[] clientPublic)
    {
        // The server creates the same key as the client. 
        // After this line, both the client and server will have the same key.
        secretKey = serverDiffieHellman.GetSharedSecretRaw(clientPublic);
    }
}

public class Client
{
    private ECDiffieHellman clientDiffieHellman;
    private byte[] secretKey;

    // Pretend this is called when the server sends a hail.
    public void OnHail(byte[] serversPublic)
    {
        // Client creates a diffie hellman instance. This will initialize with random keys internally.
        clientDiffieHellman = new ECDiffieHellman();

        // Now that the client has its own diffie hellman instance AND the servers public. 
        // It can create the key straight away. 
        // In order for the server to construct the key. It needs the clients public.
        secretKey = clientDiffieHellman.GetSharedSecretRaw(serversPublic);

        // Now that we have the key. We just need the server to create the same key.
        // In order for them to do that, they need our public.
        byte[] clientPublic = clientDiffieHellman.GetPublicKey();

        // Send a hail message. This is where you actually send the public key. 
        // How this method looks depends on your network library of course.
        // This is just an example.
        SendBytesToServer("hail-response", clientPublic);
    }
}

```

But Diffie Hellman is not perfect, it can be attacked with a **M**an **I**n **T**he **M**iddle attack. A MITM attack is an attack where there is a party in the middle that is **actively** intercepting. Let's show how that would actually look when the evil attacker can intercept all messages. In this example the attacker gets all the traffic instead of the desired party.

##### MITM Example
```csharp
public class EvilAttacker
{
    private ECDiffieHellman serverDiffieHellman;
    private byte[] serverSecret;
    private ECDiffieHellman clientDiffieHellman;
    private byte[] clientSecret;

    public void OnServerSendHailToClient(byte[] serverPublic)
    {
        // Attacker creates a diffie hellman instance. This will initialize with random keys internally.
        serverDiffieHellman = new ECDiffieHellman();

        // Now the attacker has done a diffie hellman exchange with the server.
        serverSecret = serverDiffieHellman.GetSharedSecretRaw(serverPublic);

        // Now to complete a key exchange with the server, we get the public.
        byte[] serverPublic = serverDiffieHellman.GetPublicKey();

        // Send the public. Now that means that the server thinks it's connected to a client.
        // But it has just exchange a key with us instead of the client. So now we can talk with the server
        // And the server thinks its secure.
        SendBytesToTheServer("hail-response", serverPublic);

        // Now lets do the same to the client.
        // Attacker creates a diffie hellman instance. This will initialize with random keys internally.
        clientDiffieHellman = new ECDiffieHellman();

        // Now to start a key exchange with the client, we get the public.
        byte[] clientPublic = clientDiffieHellman.GetPublicKey();

        // Send the public to the client. The client will think this is the server sending a hail.
        // After this line, the client will send a hail response.
        SendBytesToClient("hail", clientPublic);
    }

    public void OnClientSendHailResponseToServer(byte[] clientPublic)
    {
        // Now complete the key exchange with the client.
        clientSecret = clientDiffieHellman.GetSharedSecretRaw(clientPublic);

        // What we have now is one shared key with the server and one with the client.
        // This means that any future messages that come through.
        // We can encrypt with the senders key, read it, and if we want to, we can re-encrypt with the receiver's key and pass it.
        // This way no one knows that someone is reading all communications.
    }
}

```

Now that we can see what a MITM attack can do. How can we avoid it? This is where the other type of encryption comes in. Asymmetric encryption is a type of encryption where you have a pair of keys. If you encrypt something with one of the keys, **ONLY** the other key would be able to decrypt the content.

### Using Asymmetric Encryption
Now let's look at common use cases for asymmetric encryption. There are two main usages, **encryption** and **signing**. But before we explain them, let's talk about how the keys are used. Usually, you make one of the keys your "private key", and the other one your "public key". That means that you keep one key for yourself while you tell everyone about your public key. Let's see what you can do using that setup.

##### Signing
Signing is used when you want to prove that you sent a message, but not have the message be secret. To do this, you encrypt the message with your private key. This means that anyone with the public key can decrypt it. But it also means that since only you have the private key, no one else could have sent it. This is called signing.

##### Encryption
Encryption is the second common operation. If another party wants to send something encrypted to me, they could simply encrypt it with my public key, which would mean that only I can read it.

These two operations can also be combined to ensure that only the receiver can read the message while also proving the sender.

### Protecting Against MITM Attacks
Let's see how we can use these to secure a key exchange from MITM attacks. At first, you hard code the public key of the server in all your clients, while only the server has the private key. When the server then sends its Diffie Hellman public part, it also includes the same public part but encrypted using its private key. On the client, it will read the public part, but before trusting it, it will decrypt the encrypted version with the servers public key and ensure the two are the same. Then, before the client sends its public part, it will encrypt it with the servers public key, meaning that only the server can read it. This prevents MITM attacks.

### Certificates
Instead of distributing the public key in the client, you would usually want to use certificates. Every modern operating system has a list of **C**ertificate **A**uthorities that it trusts. These are companies that issue certificates, a certificate is just a public-private key pair generated and signed by the CA. This means that if you say own the domain "example.com", you could get a certificate from a CA, which includes the key-pair and also says that those pairs are ONLY valid for the domain "example.com". This allows some neat things. Instead of now hard coding the server public key in the clients. When the client connects it will remember the domain used, after connecting. The first thing the server will do is to send its certificate (with the private key removed). The client will then look at the certificate, see who issued it and make sure that the operating system trusts that CA. If it does, it will also make sure that the correct domain it was issued for was the one it used to connect, this is to ensure that the server does not send someone else's certificate.

I hope this has helped you understand modern cryptography and how it can be used.

Thanks, Albin.
