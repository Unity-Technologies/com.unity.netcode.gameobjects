# Boss Room actions walkthrough

Boss Room's actions each use a different pattern for good multiplayer quality. This doc walks you through each of them and goes into details about how we implemented each and what lessons you can learn from each. You can get their implementations [here](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/tree/v2.1.0/Assets/Scripts/Gameplay/Action).

![Playing an anticipatory animation on the owner client to appear reactive and hide Network Latency.](../../../images/sequence_diagrams/BossRoomExamples/HidingLatency_AnimationAnticipation.png)

![Boss Room's mage's bolt doesn't use NetworkVariables and NetworkObjects to track the bolt's state. Since its movement is fast, we only send an RPC to trigger VFX on clients. No need to waste CPU and bandwidth on a NetworkObject over time for this. Even if the FX passes through a wall on some clients (due to network discrepancies in target positions), the FX is so quick it wouldn't be visible to players.](../../../images/sequence_diagrams/BossRoomExamples/RPCFlowExample_MageMagicBolt.png)

![Boss Room's archer's arrows use fully replicated NetworkObjects + NetworkTransform. Since they're slow moving, we want them to be seen by late joining players. We also want to make sure their behaviour is the same for all clients (so they don't go through walls for example). This will use more bandwidth, but will be more accurate.](../../../images/sequence_diagrams/BossRoomExamples/RPCFlowExample_ArcherRangedShot.png)

![The archer's volley takes multiple steps. First on click, the client will show a circle VFX tracking the mouse. That FX isn't networked and client side only. Then on click, the client will send an RPC to the server. The server will then apply it's gameplay logic, update the imp's health (tracked by NetworkVariables) and trigger a Client RPC to play the volley animation on all clients.](../../../images/sequence_diagrams/BossRoomExamples/RPCFlowExample_ArcherVolley.png)

![Emotes only use RPCs. We don't care if a late joining player doesn't see other player's emotes.](../../../images/sequence_diagrams/BossRoomExamples/RPCFlowExample_PlayerEmote.png)

![Most of the rogue sneak's logic happens server side. Since Boss Room is a coop game, we're not using Network invisibility here, only client side effects. Clients will still receive that invisible player's information and imp AIs will ignore invisible players.](../../../images/sequence_diagrams/BossRoomExamples/RPCFlowExample_RogueSneak.png)
