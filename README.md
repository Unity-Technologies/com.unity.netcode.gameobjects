# MLAPI
MLAPI (Mid level API) is a framework that hopefully simplifies building networked games in Unity. It is built on the LLAPI and is similar to the HLAPI in many ways. It does not however integrate into the compiler and it's ment to offer much greater flexibility than the HLAPI while keeping some of it's simplicity. 

The project is WIP. 
It's licenced under the MIT licence :D

## Features that are planned / done are:
* Object and player spawning (done)
* Connection approval (done)
* Message names (done)
* Replace the integer QOS with names. When you setup the networking you specify names that are assosiated with a channel. This makes it easier to manager. You can thus specify that a message should be sent on the "damage" channel which handles all damage related logic and is running on the AllCostDeliery channel. (done)
* ProtcolVersion to allow making different versions not talk to each other. (done)
* NetworkedBehaviours does not have to be on the root, it's simply just a class that implements the send methods etc. (done)
* Multiple messages processed every frame with the ability to specify a maximum to prevent freezes in the normal game logic (done)
* Built in lag compensation (going to be worked on when all base functionality is there)
* Area of intrest (not working on ATM but it's on the TODO)
* Core gameplay components similar to what the HLAPI offers (but hopefully of better quality)
* Encrypted messages / full encryption for all messages. This will only be avalible for dedicated server use as the dedicated servers will need a dedicated RSA keypair that the clients will have the public key of hardcoded. This to exchange AES keys.
* Serializer
That's all I can think of right now. But there is more to come, especially if people show intrest in the project.



## Indepth
The project is not yet very tested. Examples will be created when it's more functional.