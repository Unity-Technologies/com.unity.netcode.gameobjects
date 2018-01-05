# MLAPI
MLAPI (Mid level API) is a framework that hopefully simplifies building networked games in Unity. It is built on the LLAPI and is similar to the HLAPI in many ways. It does not however integrate into the compiler and it's ment to offer much greater flexibility than the HLAPI while keeping some of it's simplicity. 

The project is WIP. 
It's licenced under the MIT licence :D

## Features that are planned / done are:
* Object and player spawning (working on atm)
* Connection approval (done)
* Message names (done)
* Replace the integer QOS with names. When you setup the networking you specify names that are assosiated with a channel. This makes it easier to manager. You can thus specify that a message should be sent on the "damage" channel which handles all damage related logic and is running on the AllCostDeliery channel. (done)
* ProtcolVersion to allow making different versions not talk to each other. (done)
* NetworkedBehaviours does not have to be on the root, it's simply just a class that implements the send methods etc. You could switch all your MonoBehaviours to NetworkedBehaviours
* Multiple messages processed every frame with the ability to specify a maximum to prevent freezes in the normal game logic (done)
* Built in lag compensation (going to be worked on when all base functionality is there)
* Area of intrest (not working on ATM but it's on the TODO)
That's all I can think of right now. But there is more to come, especially if people show intrest in the project.



## Indepth
The project shares many similarities with the HLAPI. But here are the major differences:
* The command / rpc system is replaced with a messaging system. You simply call the Send method in the NetworkedBehaviour and specify a name (string), all scripts that are listening to that message name will recieve an update.
