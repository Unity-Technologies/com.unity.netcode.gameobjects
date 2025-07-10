# Tricks and patterns to deal with latency

## TL;DRs

As mentioned in [latency](lagandpacketloss.md) and [tick](ticks-and-update-rates.md), waiting for your server's response makes your game feel unresponsive.

Latency destroys your gameplay experience and makes your game frustrating. It doesn't take much to make a multiplayer game unplayable. Add 200ms between your inputs and what you see on your screen and you'll want to throw your computer through the window.

Trying to deal with lag naively can introduce security and consistency issues. This page will help you get started with these techniques.

## Security

Players can mess with any client-side code and do things such as forge false network messages sent to the server.

Even if you specify in your client logic that you can't kill an Imp (for example) if it's more than 10 meters away, if the "kill Imp" message is a server RPC and there's no range check server-side, players can forge that network message to bypass your client-side logic. The ways a player might forge that kind of message is beyond the scope of this document, however, you should always keep in mind you can't trust clients. Your server must have logic to validate player actions coming from clients.

## Consistency

In addition to responsiveness, the accuracy of your simulation is important. Not only can it break your player's immersion, competitive games often have prize pools of multiple millions of dollars where you want to make sure when your user is targeting something on their screen, it actually hits.

With latency, what a client receives from the server is RTT/2 ms late. The information available client-side isn't the "live" information, it's the information sent over the internet some time earlier (for example, 200ms ago). This means if a local player collides with a server-driven networked object, the player sees the Collider overlap with it before the server reacts and tells it to move.

This also means if a player shoots a target's head, the shot targets a head that's RTT/2 ms behind and reaches the server RTT/2 ms later (meaning you have a full RTT ms of desynchronization between your shot and the actual hit). This means any interactions I'm making on that object will see a delayed effect.

In summary, you want accuracy, security, and responsiveness for your game.

## Authority

### Server authoritative games

The authority is who has the right to make final gameplay decisions over objects. A server authoritative game has all its final gameplay decisions executed by the server.

![The server gets to make the final gameplay decisions](../../images/sequence_diagrams/dealing_with_latency/Example_CharPos_ServerAuthority.png)

#### Good for world consistency

An advantage of server authoritative games is consistency. Since the server (a single node on the network) makes all gameplay decisions (such as a player opening a door or a bot shooting the player), you can be sure the decisions are made at the same time. A client authoritative game would have decisions made on client A and other decisions on client B, with both separated by RTT ms of Internet lag. Player A killing player B while player B was already hiding behind cover would cause consistency issues. Having all this gameplay logic on one single node (the server) makes these kinds of considerations irrelevant, since everything happens in the same execution context.

#### Good for security

Critical data (like your character health or position) can be **server authoritative**, so cheaters can't mess with it. In that instance, the server has the final say on that data's value. You wouldn't want players on their clients to set their health (or even worst, other player's health) at will.

> [!NOTE]
> Games that use the **Host** model still have this risk because one of the clients acts as a client and a server. Pure servers hosted by the game developer (dedicated servers) won't run into this issue.

Netcode for GameObjects is server authoritative, which means it only allows the server to write to `NetworkVariable`s. However, when accepting RPCs coming from clients, you must add validation code, since those RPCs are coming from *never to be trusted* sources.

Note here an input from a client can be anything, from a user interacting with another player to revive them, to a player sending keys and clicks for position changes.

> [!NOTE]
> The rest of the document assumes your game is server authoritative.

#### Issue: Reactivity

An issue with server authority is you're waiting for your server to tell you to update your world. This means that if you send an input to the server and wait for the server to tell you your position change, you'll need to wait for a full **RTT** before you see the effect. There are [patterns](#patterns-to-solve-these-issues) you can use to solve this issue while still remaining server authoritative.

### Client authority

In a client authoritative game using Netcode for GameObjects, you still have a server that's used to share world state, but clients will own and impose their reality.

![The client gets to make the final gameplay decisions](../../images/sequence_diagrams/dealing_with_latency/Example_CharPos_ClientAuthority.png)

#### Good for reactivity

You can often use this when you trust your users or their devices. For example, you might have a client tell the server "I killed player X" instead of "I clicked in that direction" and have the server simulate that action to return the result. This way, the client shows the death animation for the enemy as soon as the player clicks (since the death is already confirmed and owned by the client). The server would only relay that information back to other users.

#### Issue: World consistency

There are possible synchronizations issues with client authoritative games. If your character moves client-side thinking everything is okay and an enemy has stuns it in the meantime, that enemy will have stunned your character earlier on a different version of the world from the one you're seeing. See [latency page](lagandpacketloss.md). If you let the client make an authoritative decision using outdated information, you'll run into synchronization issues, overlapping physics objects, and similar issues.

#### Owner authority vs All clients authority

Multiple clients with the ability to affect the same shared object can quickly become a mess.

![Multiple clients trying to impose their reality on a shared object.](../../images/sequence_diagrams/dealing_with_latency/Example_CaptureFlagPart1_ClientAuthorityIssue.png)

To avoid this, it's recommended to use client **owner** authority, which allows only the owner of an object to interact with it. Since the server controls ownership in Netcode, there's no possibility of two clients running into a [race condition](https://en.wikipedia.org/wiki/Race_condition#In_software). To allow two clients to affect the same object, you must ask the server for ownership, wait for it, then execute the client authoritative logic you want.

![Multiple clients ASKING to interact with a shared object.](../../images/sequence_diagrams/dealing_with_latency/Example_CaptureFlagPart2_ServerAuthorityFix.png)

#### Issue: Security

Client authority is a pretty dangerous door to leave open on your server because any malicious player can forge messages to say "kill player a, b, c, d, e, f, g" and win the game. It's pretty useful though for reactivity. Since the client is making all the important gameplay decisions, it can display the result of user inputs as soon as they happen instead of waiting a few hundred milliseconds.

When you don't think there's any reason for your players to cheat, client authority can be a great way to have reactivity without the complexity added with techniques like [input prediction](#prediction).

Another way of solving this issue in a client authoritative game is using soft validation server side. Instead of doing all simulation server side, the server only does basic validation. The server would, for example, do range checks to make sure a player isn't teleporting to places it shouldn't. Doing so is acceptable for most [PvE](https://en.wikipedia.org/wiki/Player_versus_environment) games. However, [PvP](https://en.wikipedia.org/wiki/Player_versus_player) games usually require server authority.

 Summary | -
 --- | ---
 Server authority | More secure. Less reactive. No sync issues.
 Client authority | Less secure. More reactive. Possible sync issues.

### Boss Room's authority

Boss Room's [actions](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/tree/v2.1.0/Assets/BossRoom/Scripts/Server/Game/Action) uses a server authoritative model. The client sends inputs (mouse clicks for the character's destination) and the server sends back positions. This way, every features in the world are on the same world time. If the Boss charges and bumps you, you'll see your character fly away as soon as the boss touches you, not pass through you and then see you fly away.

While developing Boss Room, the team knew that everything is synced and secure by default, which made designing gameplay simpler. Boss Room then added reactive gameplay exceptions on a case-by-case basis (see the following sections on [patterns to solve latency issues](#patterns-to-solve-latency-issues-in-a-server-authoritative-game).

Having only one source of truth makes debugging your game easier. You know you can trust the information you're seeing server-side is the "truth" and that the information relayed to clients is also the "truth" (since it comes from the server).

Boss Room, being a coop game, it might have benefited from using client authority. However, players would have seen issues with syncing with the Boss charge, for example. The server driven Boss would charge your client driven client. Mixing authority with server driven AIs and client driven players can easily have become a mess.

> [!NOTE]
> Rule of thumb: A good way to think about your game architecture at first is to have your game server authoritative by default and make exceptions for reactivity when security and consistency allows it.

A pattern we've seen that helped when not sure about using client or server authority is to implement your game behaviour not by server/client, but authoritative/non-authoritative. By abstracting this to authority instead of isServer/isClient, your code can easily be swapped to client or server authority without too many refactors.

```csharp
// before
if (isServer)
{
    // take an authoritative decision
    // ...
}
if (isClient) // each of these need to be swapped if switching authority
{
    // ...
}

// after
bool HasAuthority => isServer; // can be set for your whole class or even project
// ...
if (HasAuthority)
{
    // take an authoritative decision
    // ...
}
if (!HasAuthority)
{
    // ...
}
```

## Patterns to solve latency issues in a server authoritative game

Summary |
--- | ---
Client authority | Less secure. More reactive. Possible sync issues.
Action anticipation | More secure. Somewhat reactive. Possible visual sync issues.
Prediction | More secure. More reactive. Correction on sync issues. Complex and tentacular.
Server side rewind | More secure. More accurate by favoring the attacker. Shot behind a wall issue.

### Allow low impact client authority

When designing your feature, keep in mind using server authority as the default. Then identify which feature needs reactivity and doesn't have a big impact on security/world consistency.
User inputs are usually a good example. Since users already own their inputs (I own my key press, the server can't tell me "no you haven't pressed the w key") user input data can easily be client driven. In FPS games, your look direction can easily be client driven without much impact. The client will send to the server its look direction instead of mouse movements. Having a correction on where you look would feel weird and security for this has challenges anyway (how do you differentiate an aim bot vs someone flicking their mouse super fast).

If a player selects an imp, the selection circle will be client driven, it won't wait for the server to tell us we've selected the imp.

[Click](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/Input/ClientInputSender.cs) is client driven, [AOE selection](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/Action/AoeActionInput.cs) is client driven. AOE's distance check is client driven. However the distance check is done [server side too](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/Action/AOEAction.cs). This way if there's too much latency between a client click and the server side position, the server will do a sanity check to make sure that for its own state, the click is within the allowed range.

The above examples are atomic actions. They happen on click.
To do continuous client driven actions, there's a few more considerations to take.

- You need to keep a local variable to keep track of your client authoritative data.
- You then need to make sure you don't send RPCs to the server (containing your authoritative state) when no data has changed and do dirty checks.
- You'd need to send it on tick or at worst on FixedUpdate. Sending on Update() would spam your connection.

A sample for a [ClientNetworkTransform](../components/networktransform.md#clientnetworktransform) has been created, so you don't have to reimplement this yourself for transform updates. A [sample](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Basic/ClientDriven) has been created on how to use it. See [movement script](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/ClientDriven/Assets/Scripts/ClientPlayerMove.cs).

> [!NOTE]
> A rule of thumb here is to ask yourself: "Can the server correct me on this?". If it can, use server authority.

### Client Side Prediction

Predicting what the server will send you.

Prediction is a common way of making an educated "guess" as to what the server will send you. Your game can stay server authoritative, but instead of waiting a full RTT for your action results, your client can simulate and run gameplay code of what it thinks will happen as soon as your players trigger inputs. For example, instead of waiting a full RTT for the server to tell me where I moved, I can directly update my movements according to my inputs. This is close to client authority, except with this technique you can be corrected:

The world (and especially the internet) is messy. A client can guess wrong. An event produced by another player can come and mess your own local guess or your physic simulation can be non-deterministic.

With the movement example, I can have an enemy come and stun me while I thought I can still move. 200 ms latency is enough time for a stun to happen and create a discrepancy between the move I "predicted" client side and what happened server side. This is where "reconciliation" (or "correction") comes in play. The client keeps a history of the positions it predicted. Being still server authoritative, the client still receives (outdated by x ms of latency) positions coming from the server. The client will validate whether the positions it predicted in the past fits with the old positions coming from the server. The client can then detect discrepancies and "correct" its position according to the server's authoritative position.
This way, clients can stay server authoritative while still be reactive.

#### Input Prediction vs World Prediction vs Extrapolation

Local input prediction will predict your state using your local player's inputs.

You can also predict other objects in your world by predicting from the last server data your received.
Knowing an AI's state at frame i, we can predict its state at time i+1 assuming it's deterministic enough to run the same both client side and server side.

Extrapolation is an attempt to estimate a future game state, without taking into account latency. On receipt of a packet from the server, the position of an object is updated to the new position. Awaiting the next update, the next position is extrapolated based on the current position and the movement at the time of the update.

The client will normally assume that a moving object will continue in the same direction. When a new packet is received, the position may be updated.

For Netcode for gameobjects (Netcode), a basic extrapolation implementation has been provided in [`NetworkTransform`](../components/networktransform.md) and is estimated between the time a tick advances in server-side animation and the update of the frame on the client-side. The game object extrapolates the next frame's values based on the ratio.

> [!NOTE]
> While Netcode for GameObjects doesn't have a full implementation of client-side prediction and reconciliation, you can build such a system on top of the existing client-side anticipation building-blocks, `AnticipatedNetworkVariable` and `AnticipatedNetworkTransform`. These components allow differentiating between the "authoritative" value and the value that is shown to the players. These components provide most of the information needed to implement prediction, but do require you to implement certain aspects yourself. Because of the complexity inherent in building a full client prediction system, the details of that are left for users, and we recommend only advanced users pursue this option.
> For more information, refer to the [client anticipation](../advanced-topics/client-anticipation.md) documentation.

### Controlled Desyncs

It can be hard to handle misprediction corrections elegantly and without taking your players out of their immersion. Clients also can't always stay in sync tick by tick, frame by frame. You can adapt your gameplay and animations to allow your clients to desync for a few seconds, as long as they reach a common state and resync again to make corrections seamless. By using controlled desyncs, clients can resolve and reinterpret how they reach a common state independently based on their local context. You can consider this technique as a "smart context-aware interpolation" or an "elegant correction" when predicting. It reuses some of [action anticipation](#action-anticipation)'s technique where you use non-gameplay-important animations to hide discrepancies caused by lag and reinterpret how the client will reach the target authoritative state.

For example, you could have an assassination mechanic that you want to seamlessly handle misprediction correction. You could have your client tell the server "I killed that target," have the server validate to prevent cheating (like with server rewind), and still give advantage to the attacker.

In this example, _Alice the Assassin_ is chasing _Bob the Victim_. _Bob_ enters a room and quickly closes the door behind them at time _t=0_ seconds. Simultaneously at _t=0_, _Alice_ hits the **kill** button. Both actions are client-driven and reach the server at the same time, so the server would tell all clients "Alice killed Bob" at _t=0.1_. _Bob_ receives the "You got killed" message at _t=0.2_. However, _Bob_ locally closed their door at _t=0_, it would appear that _Alice_ killed _Bob_ through the door.

One solution is to use server authoritative gameplay with prediction and correction on both clients as described in other sections on this page. The door is corrected by teleporting back open, and the killing animations would play as usual. That correction would remove your players from their immersion and frustrate _Bob_.

Another solution is to use action anticipation on the door closing and have the door be server driven. You remove the correction and desync problems altogether. However, it still adds lag to each door opening and closing as an instant action without animation to hide the lag.

For the best user experience, you can use controlled desyncs. You could add windows in your doors and reinterpret the kill action depending on local client state. _Alice_ could see a knife kill while the door is still open (they haven't received the "door closed" update from the server yet), and _Bob_ could see a gun kill through the window of the locally closed door instead. Ultimately, clients only care about the final "Bob is dead and killed by Alice" state. You let clients reinterpret how they reach that final state according to their own different local context.

Some other examples include:
- Physics based doors swinging in different direction based on local collision when there's contention
- IK solving locally for your feet when walking on stairs
- Death ragdolls where a dead character's body doesn't influence your gameplay anymore and can desync from one client to another. The server syncs all clients that "this character is now dead" and send it's death position. From that point, clients can apply ragdoll physics how they want without syncing between clients. Since they have no gameplay impact, it doesn't matter if the ragdoll is in a tree for one person and a ditch for another. Eventually, the players respawn or the ragdolls disappear, and your game state is in sync again.

In each situation, you need to ensure the reinterpretation doesn't affect gameplay critical systems. In the first example, if the number of gun kills and knife kills are important to your player's stats, this solution might not be doable.

There are situations when clients and servers need to be in constant sync. For example, high precision tactical shooters try to ensure the game is fair for both attackers and targets. Outside of those specific situations, controlled desyncs can maintain player immersion while dealing with lag. As long as the clients end in the same state and doesn't accumulate discrepencies, your players will be fooled. The goal is to give your players the _illusion_ they are playing in the same world, when they actually may not.

### Action Anticipation

There's multiple reasons for not having server authoritative gameplay code run both client side (with [prediction](#client-side-prediction)) and server side. For example, your simulation can be not deterministic enough to trust that the same action client side would happen the same server side. If I throw a grenade client side, I want to make sure the grenade's trajectory is the same server side. This often happens with world objects with a longer life duration, with greater chances of desyncing. In this case, the safest approach would be a server authoritative grenade, to make sure everyone has the same trajectory. But how do you make sure the throw feels responsive and that your client doesn't have to wait for a full RTT before seeing anything react to their input?

For a lot of games, when triggering an action, you'll see an animation/VFX/sound trigger before the action is actually executed. A trick often used for lag hiding is to trigger a non-gameplay impacting animation/sound/VFX on player input (immediately), but still wait for the server authoritative gameplay elements to drive the rest of the action. If the server has a different state (your action was cancelled server side for some reason), the worst that happens client side is you've played a useless quick animation. It's easy to just let the animation finish or cancel it.

This is referenced as action casting or action anticipation. You're "casting" your action client side while waiting for the server to send the gameplay information you need.

For your grenade, a client side "arm throw" animation can run, but the client would wait for the grenade to be spawned by the server. With normal latencies, this usually feels responsive. With higher abnormal latencies, you can run into the arm animating and no grenade appearing yet, but it would still feel responsive to users. It would feel strange, but at least it would feel responsive and less frustrating.

In Boss Room for example, our movements use a small "jump" animation as soon as you click somewhere to make your character move.
The client then waits for the server to send position updates. The game still feels reactive, even though the character's movements are server driven.

> [!NOTE]
> For example, Boss Room plays an animation on [Melee action](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/Action/MeleeActionFX.cs) client side while waiting for the server to confirm the swing. If the server doesn't confirm, worst comes to worst we've played an animation for nothing and nothing else is desynced. Your players will be none the wiser.

This is also useful for any action that needs to interract with the world. An ability that makes you invulnerable would need to be on the same time as other server events. If I predict my invulnerability, but a sniper headshots me before my input has reached the server, I'll see my invulnerable animation, but will still get killed. This is pretty frustrating for users. Instead, I can play a "getting invulnerable" animation with the character playing an animation, wait for the server to tell me "you're invulnerable now" and then display my invulnerable status. This way, if a sniper shoots me, the client will receive both the sniper shot and the invulnerability messages on the same timeline, without any desync.

Top: player with zero artificial latency, doing a jump and moving almost instantly.
Bottom: player with 1000ms RTT, doing the same. A jump (action anticipation) and moving when the server tells it too, a full RTT later.

Players don't have to wait for their mouse movements to be synced for AOE. They're independant. The click will trigger a server RPC (you can see the added delay on the bottom video)

Action anticipation can also be used to set the value of a network variable or network transform on the assumption that an action succeeds while waiting for the server to respond. This is the first building block of client-side prediction mentioned above, with the most simple form being to set a value and let the server overwrite it later. This is done in Netcode for GameObjects using  `AnticipatedNetworkVariable<T>` and   `AnticipatedNetworkTransform`. For more information, refer to the [client anticipation](../advanced-topics/client-anticipation.md) documentation.

### Server Side Rewind (also called Lag Compensation)

Server rewind is a security check on a client driven feature to make sure we stay server authoritative. A common usecase is snipers.
If I aim at an enemy, I'm actually aiming at a ghost representation of that enemy that's RTT/2 ms late. If I click its head, the input sent to the server will take another RTT/2 ms to get to the server. That's a full RTT to miss my shot and is frustrating.
The solution for this is to use server rewind by "favoring the attacker". Psychology 101: it's way more frustrating for an attacker to always miss their shots than for a target to get shot behind a wall once in a while. The client sends along with its input a message telling the server "I have hit my target at time t". The server when receiving this at time t+RTT/2 will rewind its simulation at time t-RTT, validate the shot and correct the world at the latest time (ie kill the target). This allows for the player to feel like the world is consistent (my shots are hitting what they're supposed to hit) while still remaining secure and server authoritative.

**Note**: the server rewind of the game's state is done all in the same frame, this is invisible to players.
This is a server side check that allows validating a client telling you what to do.

> [!NOTE]
> There's no server side rewind implementation right now in Netcode for GameObjects, but you can implement your own. See our [roadmap](https://unity.com/roadmap/unity-platform/multiplayer-networking) for more information.

**Other syncing techniques not relevant to Netcode for GameObjects**
Deterministic lockstep/

A method of networking a system from one computer to another by sending only the inputs that control that system, rather than the state of that system. This allows for a big amount of entities to be synced (think RTS with hundreds of units)

Deterministic rollback/

An enhancement of deterministic lockstep where clients forward-predict inputs while waiting for updates. This setup enables a more responsive game than lockstep. It's relatively inexpensive.
