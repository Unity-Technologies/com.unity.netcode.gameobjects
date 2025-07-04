# Understanding latency

Multiplayer games operating over the internet have to manage adverse network factors that don't affect single-player or LAN-only multiplayer games, most notably [network latency](#network-latency). Latency (also known as lag) is the delay between a user taking an action and seeing the expected result. When latency is too high, a game feels unresponsive and slow.

Generally, 200ms of latency is the point at which users notice gameplay degradation, although different types of games can tolerate more or less latency. For example, first-person shooter games perform best with less than 100ms of latency, whereas real-time strategy games can tolerate higher latency values of up to 500ms before gameplay is meaningfully impacted.

In addition to the sources of latency described on this page, [ticks and update rates](ticks-and-update-rates.md) can also affect the user experience of your game.

## Sources of latency

There are a number of factors that contribute to latency, some of which can be minimized, although it's impossible to get rid of latency entirely.

### Non-network latency

Non-network latency refers to latency incurred by processes that happen before there's any involvement with the network. This is an area where you have more control over optimization to reduce latency (whereas network conditions are often beyond your control). Non-network latency is also referred to as input lag: the time it takes user input to be rendered on screen.

Factors that contribute to non-network latency include:

- **Input sampling delay**: The time it takes for an input device, such as a mouse or keyboard, to recognize that it's been activated and the time it takes for the game to detect that change.
- **Render pipeline delay**: The time it takes for the GPU to render the outcome of inputs, which are often batched together to be processed in groups rather than individually.
- **Vertical synchronization (VSync)**: A graphics technology that synchronizes the frame rate of the GPU with the refresh rate of the connected monitor. VSync can reduce undesirable visual behavior known as screen tearing, at the cost of increased latency incurred by the reduced frame rate.
- **Display processing delay**: Display devices typically process incoming signals to some degree (such as deinterlacing and noise cancellation), which adds to latency.
- **Pixel response time**: LCD screen pixels physically take time to change their brightness in response to inputs.

### Network latency

Network latency refers to latency incurred by interactions over the network after local processing is complete.

Factors that contribute to network latency include:

- **Processing delay**: The time it takes for the network router to read packet headers and forward them to the appropriate location. Processing delay also includes any encryption, network address translation (NAT), or network mapping that the router might be doing. This delay is generally small, but can add up when packets move between multiple locations.
- **Transmission delay**: The time it takes for packets to be transmitted across the network. Transmission delay is proportional to the size of the packet (in bits) and is usually greatest in the parts of the network that connect directly to end users, which often have lower relative bandwidth. Sending fewer, larger packets can reduce transmission delay, because more bits are spent on game data rather than packet headers.
- **Queueing delay**: The time packets spend waiting in queues. When a router receives more packets than it can process, those packets are added to a queue, which causes delays. Similarly, network interfaces can only output a limited number of packets at a time, so there can also be transmission queues. These queueing delays can add significantly to latency.
- **Propagation delay**: The time it takes for signals to physically travel across the network. In [client-server topologies](../terms-concepts/network-topologies.md), this can be optimized by having servers located geographically closer to players.

#### Round-trip time and pings

Round-trip time (RTT) is a measure of how long it takes a packet to travel from one location on a network to another and receive a response packet back. You can use RTT to understand how much network latency your game is experiencing in different network conditions.

A ping is a simplified way of measuring RTT that involves sending a very basic message, with no processing at either end of the interaction, to get a general idea of network responsiveness and latency.

![](../images/ping-animation-light.gif)

The time between sending the request and receiving the answer is the value of your ping. Sending and receiving data can take different amounts of time: for example, with a 20ms ping it might take 15ms to send a request from the client to the server and only 5ms to receive a response. Higher ping values indicate a lot of network latency, which can make games feel slow and unresponsive.

#### Jitter and packet loss

RTT can be affected by changing network conditions and is unlikely to remain constant, usually hovering around an average value. The degree to which this average value varies is referred to as jitter. Jitter can affect network latency mitigation and cause packets to arrive out of order.

In addition to being delayed, packets can also be lost, which degrades gameplay experience either by losing key information and causing unpredictable behavior, or requiring it to be sent again, causing delays.
