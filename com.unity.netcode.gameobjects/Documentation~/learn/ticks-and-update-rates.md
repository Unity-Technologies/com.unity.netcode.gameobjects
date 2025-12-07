# Tick and update rates

In addition to the effects of [latency](lagandpacketloss.md), gameplay experience in a multiplayer game is also affected by the server's [tick rate](#tick-rate) and the client's [update rate](#update-rate). Low tick and update rates reduce game responsiveness and add to perceived latency for users.

## Tick rate

Tick rate, also known as the simulation rate, is a measure of how frequently the server updates the game state. At the beginning of a tick, the server starts to process the data it's received from clients since the last tick and runs the necessary simulations to update the game state. The updated game state is then sent back to the clients. The faster the server finishes a tick, the earlier clients can receive the updated game state (subject to the client's [update rate](#update-rate)).

![Tick rate](../images/tick_rate.png)

Tick rate is measured in hertz (Hz). For example, a tick rate of 60 Hz means the server updates the game state sixty times a second, whereas a tick rate of 30 Hz means the server updates the game states only thirty times a second. Higher tick rates produce a more responsive game experience, at the cost of increased load on the simulating server.

When a server fails to process all incoming client data before the end of the current tick, it produces gameplay issues such as rubber banding, player teleporting, hits getting rejected, and physics failing. As such, there's a balance between having a high tick rate to create responsive gameplay and ensuring that the server has enough time to process all client data before the end of a tick.

## Update rate

Update rate is a measure of how frequently the client sends and receives data to and from the server. It's also measured in hertz and, like tick rate, a higher update rate results in a more responsive game at the cost of increased processing and network demands on the client and server.

![](../../images/update-rates-light.png)

In addition to adding to perceived latency, low update rates can cause their own issues, such as multiple updates being bundled together and arriving at the same time, causing undesirable behavior.

In the example of 'super bullets': if an in-game gun is capable of shooting more times per second than the client can update, then at some point the impact of more than one bullet will be communicated in the same update. For players receiving this update, it will seem as if a single bullet is doing more damage than it should, because they're actually receiving updates from multiple bullets at once.

The diagram below illustrates the difference between a 10 Hz update rate and a 60 Hz update rate. With a 10 Hz update rate, the game sends updates to the server ten times a second, so a gun that can fire 600 rounds per minute (RPM), or ten times a second, will only ever send a single bullet per update. A gun that fires at 750 RPM, more than ten times a second, will end up running into the super bullet problem. A higher update rate of 60 Hz resolves this issue, ensuring that there are enough updates for each bullet to arrive separately in its own update.

![](../../images/rpm_update_rates-light.png)

### Discrepancy between tick rate and update rate

If the update rate of a client is lower than the tick rate of the server, then the client won't see the benefit of the high tick rate, because it will only receive updates at the client update rate, even if multiple ticks have been processed in the interim.
