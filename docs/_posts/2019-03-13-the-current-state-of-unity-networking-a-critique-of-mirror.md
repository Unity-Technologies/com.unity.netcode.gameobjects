---
layout: post
title:  "The Current State of Unity Networking: A Critique of Mirror"
date:   2019-03-13 11:04:00
author: TwoTen
---

This is a post to all game developers currently seeking a network library, or if you are currently using one of the high level libraries. I will be giving some critique to the popular networking library UNET and primarily its largest fork Mirror.

##### Disclaimer
_Just want to get something clear before I start. The choice of network library does **NOT** affect me. As of now (and I plan to continue this), there is no way to donate, purchase or give any monetary support directly or indirectly to the MLAPI project or the MidLevel organization. The only goal is to create great tools to allow for great games to be made._

## Background
I am the developer of the MLAPI project. It was created In January 2018 in an AirBnB in Japan when I was bored and has since evolved to a mature project. The goals of the MLAPI has been the same since it was first started. Create a high level networking library that is as passive as possible. Just relaxing, not doing anything on its own. Allowing the user to tell the library to do with great control. As the name suggests. MidLevel API, the MLAPI wants to be a thin layer with all the good and optional abstractions, this to allow the users something else than a high level, abstracted, 0 control library and a raw socket. We want something in between. Something that brings the best of both worlds. Have the library help the developer, but never restrict them.


## Purpose
The purpose of this post is to discuss the current state of UNET and its fork Mirror. Let's get into some history.


In 2014, Unity announced a LowLevel transport and a High Level Library. Built by Sean Riley and team, as demonstrated [Here](https://www.youtube.com/watch?v=ywbdVTRe-aA), it has many similarities to Unreal's networking solution. Its primary focus was to be able to write network code as part of the game code, where the network code is more of an "automatic" side effect of the developer instructing the library to network certain things. Allowing you to only focus on the classical networking concepts and architecture while leaving much of the network code to a library. The HLAPI was, in my opinion, amazing. It changed many developers opinions about the difficulty of networking by providing a concept of abstracted networking that became proven. Unfortunately, it was discontinued in the early stages leaving it riddled with bugs that make it harder to use currently and unable to scale. It served its purpose though of how well a high level system could work where both the network code and the game code were seamlessly integrated with each other. As this was the new official networking solution that provided simple networking many Unity developers moved towards it and still are.


As a result of many bugs, asset developer Vis2k created a fork of UNET. Fixing many bugs and cleaning up a lot of the dirtiness that was the HLAPI. By the looks of things, the HLAPI was still an experimental library and Mirror has made it more production ready than ever. Other forks of UNET was also created but never took off. As Mirror is the only continuation available, it has been widely adopted. I will be providing critique to the library and suggesting alternatives for all the game developers out there.


### The Problem With Mirror
UNET served as an experiment that showed what a good networking library could be. It showed that you can create a tool that truly helps you create network code much easier. Unfortunately, it was buggy, and it was way too early for it to be near production ready. This problem is not one can that should be directed towards Unity _anymore_. Over time, the communication, operation, and maintenance of UNET degraded rapidly. Recently though, Unity has been clear that UNET should be considered obsolete and a technology of the past, with the new 'Unity Transport' and 'Unity Networking Layer' that is also experimental. As Unity has not settled on how networking should be implemented, developers currently need a solution that provides the ideas of HLAPI but with a stronger foundation. There are many libraries out there and based on my experience here is some information to consider when picking one.


It has become apparent that UNET was experimental and evolved into something with no direction. A networking library should be there to support you. Help you get started, make things faster and easier. But not to decide how you should do things nor be limiting you. Sure, right now you might want simple things, easily get movement synchronized, bullet cross spawned and such. But when it comes to expanding on UNET, I believe Mirror is a really poor choice.


While the Mirror project keeps fixing up UNET, there is no evolvement. UNET has many limitations which carry to Mirror. It would appear that Mirror does not care about providing more features or even reworking the existing features. You can read the compiled [comparison](https://midlevel.github.io/MLAPI/features) where we compare features between the libraries. The MLAPI offers more convenience, more choice, better performance, and better control. This does make the Mirror project a poor choice in my opinion.


The development ethos at Mirror is something that concerns me and can affect the future of Unity networking libraries if used as a base. I recently purchased the main developer Vis2k's book [Indie MMORPG Development](https://noobtuts.com/books/indie-mmorpg-development) to try to understand the philosophy. And well, the book was actually something one would expect. Nothing in there made me think it extremely uninformed or wrong, but I still don't understand what's actually going on with the Mirror project. The concepts talked about in the book are not reflected at all in the project. 

#### Specifics
Mirror contains many issues in multiple aspects and the details need to be looked at to understand them.

##### Performance
The Mirror project proudly announces itself as "MMO Scale". When it comes to memory management, it's some of the worst I have seen. Just to send a single message, the message is copied and reallocated multiple times too using  .ToArray() methods. Everything to save a few lines of code while taking a huge hit on performance. Both GC pressure and CPU usage are ignored for the sake of saving a few lines of code.


Another great example is the Telepathy transport which is Mirror's default transport. It spins 2 threads per client just for a read/write loop as opposed to having a general read/write thread that dispatches to each client. By having 2 threads be created per client, at anything close to MMO scale this will cause CPU exhaustion and also require more expensive servers. On a smaller scale, it can impact game performance and require higher requirements than sanely needed. Even Microsoft has said that the ['one thread per client model is well-known to not scale beyond a dozen clients or so'](https://devblogs.microsoft.com/oldnewthing/does-windows-have-a-limit-of-2000-threads-per-process/), so imagine two threads per client.


Looking over the source code you can find many decisions that align with a lack of consideration for performance. LinQ, which is VERY known to be generous with the amount of garbage it generates, is used in many places instead of a simple for loop in order to save lines. This is clearly documented in Unity's guide [Optimizing garbage collection in Unity games](https://unity3d.com/learn/tutorials/topics/performance-optimization/optimizing-garbage-collection-unity-games).


Memory is never reused. It's copied and reallocated multiple times per message and is then at the end thrown away.

##### The "Level" Of Mirror
The MLAPI proudly advertises itself as "MidLevel", that is. High level abstractions with the option to go low level. The MLAPI provides the tools and not the rules. With that in mind, what is Mirror then?

If we assume that Mirror wants to be a High Level library. With many abstractions that is easy to use. Then well, why has nothing been done in that field? I will take the MLAPI as an example of this. The MLAPI has more of these conveniences than the Mirror project, here are a few.

**Spawn Payloads**

In the MLAPI you can send some data along with a spawn call.


**Lag Compensation**

The MLAPI has built-in latency based lag compensation which is good for quickly prototyping lag compensated architectures.


**RPC Return values**

The MLAPI lets you return values from an RPC. This gives you lots of conveniences. No more call Command that then calls ClientRpc's.


**Connection Approval**

The MLAPI lets you decide whether a connection should be approved or not based on data that the client provides when it connects. This can be authentication tokens and such (Of course, this is encrypted and authenticated).


**Bulk RPCs**

The MLAPI lets you invoke an RPC on say, only a few clients. Seems obvious. It increases performance by a **LOT** and offers convenience.


**Encryption & Authentication**

The MLAPI lets you encrypt and authenticate (HMAC) any message. This seems obvious. Especially for sending steam tokens and such. Surely if it's a high level library, it would have this? It's so essential.


**Memory Management**

The MLAPI pools almost all memory. We barely allocate, anything, ever. Especially not on the server. This is important, I have never seen a more wasteful project than Mirror. As shown earlier, there is no care towards the usage of system resources.


<br>
This pattern continues. Refer to the features page for a full list. So if it's not a high level abstraction layer. Is it a thin low level layer? Still no, the MLAPI still offers more in that field.


**Memory Management**

As previously described, memory management is still a relevant point here. It makes Mirror very clunky and unperformant.


**Custom Messaging**

In the MLAPI, you can use something called "custom messaging" to send binary data, without interacting with the MLAPI's high level stuff. It's really thin and allows you to write your own protocol on top. Mirror, not so much. You are still forced to use their serialization, not because they have a poor implementation but because they have not changed anything at all with UNET's implementation.

### Poor Design
The pattern of poor design repeats. In certain areas, it's more apparent than anything. Some things in Mirror are just really poorly implemented. And once again, it seems it's due to the features being carried from UNET.


**SyncVar**

Let's talk about SyncVars as an example. In UNET and Mirror, they can only be chosen to be sent to every client. There is no care for data security. You can see everyone's health, position and etc. Simple network packet inspection reveals everything about other clients and also wastes a ton of bandwidth.


In the MLAPI on the other hand, our equivalent. "NetworekdVars" can be full duplex (that is, read and written by both servers and clients) and permissions can be set up using predefined conditions such as "Only the server can write, and only the owner can read". Or even per client permissions using delegates. We won't limit you, we will simply provide you with the tools to get the best result.


### Final Words
While many of these points seem specific, there are many more. To continue on the point of keeping UNET's limitations. 

Seriously though, I suggest you have a look at the MLAPI's feature page and at least think, why is this not included in Mirror? I seriously can't tell you. MLAPI is a hobby project that has managed to out-perform Mirror and shows more care towards both simplicity and ensuring everything is fast on a low level while staying completely managed.


The MLAPI was written from scratch. Everything in the MLAPI was designed a certain way for a reason. Not because "that's how UNET is". The MLAPI will not stand in your way the same way Mirror and UNET might do, but it will try to help you.


Now, with all these features and options. It must be much harder to use right? No, it's not. The MLAPI has near equal equivalents of the features provided in UNET and Mirror. Minor features that are missing can be implemented easily due to the simplicity and flexibility of the MLAPI. It's performant, easy to use, powerful and gives you a foundation to write your game on top of, rather than defining a set of rules for you to work within.

It's easy to migrate projects from UNET and Mirror to the MLAPI. And I'm sure many of you will find MLAPI's features really useful. There is no dirty weaver magic. And most importantly, things are always evolving. The MLAPI has always been evolving. It's easy to follow the changes as we use [Semantic Versioning](https://semver.org/) and I suggest you look at our [changelog](https://github.com/midlevel/MLAPI/releases).


I spent quite a lot of time writing this and really and I would really love to hear what others think. Do you agree or disagree, and for what reason? Please let me know.


Thanks, Albin