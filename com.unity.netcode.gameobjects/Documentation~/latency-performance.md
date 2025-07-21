# Latency and performance

Manage latency and performance in your Netcode for GameObjects project.

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[Lag and packet loss](learn/lagandpacketloss.md)** | Multiplayer games operating over the internet have to manage adverse network factors that don't affect single-player or LAN-only multiplayer games, most notably network latency. Latency (also known as lag) is the delay between a user taking an action and seeing the expected result. When latency is too high, a game feels unresponsive and slow. |
| **[Ticks and update rates](learn/ticks-and-update-rates.md)** | In addition to the effects of latency, gameplay experience in a multiplayer game is also affected by the server's tick rate and the client's update rate. Low tick and update rates reduce game responsiveness and add to perceived latency for users. |
| **[Client-side interpolation](learn/clientside-interpolation.md)** | You can use client-side interpolation to improve perceived latency for users. |
| **[Client anticipation](advanced-topics/client-anticipation.md)** | Netcode for GameObjects doesn't support full client-side prediction and reconciliation, but it does support client anticipation: a simplified model that lacks the full rollback-and-replay prediction loop, but still provides a mechanism for anticipating the server result of an action and then correcting if you anticipated incorrectly. |
| **[Dealing with latency](learn/dealing-with-latency.md)** | Understand the available methods for dealing with different kinds of latency. |