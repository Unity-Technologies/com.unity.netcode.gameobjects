---
title: NetworkProfiler Window
permalink: /wiki/network-profiler-window/
---

The MLAPI NetworkProfiler is an Editor window for profiling bandwidth usage for a game. It uses the public MLAPI NetworkProfiler API. The Editor Window is included in the Editor unityasset starting with version v1.3.0

![](https://i.imgur.com/VwTLPGB.png)

The first toggle states if the NetworkProfiler should be recording. The second field states how many Ticks of history the profiler should keep track of. The third is the delay between each refresh of the window. Higher refreshes might make it difficult to read the values you want. Real-time can be achieved by setting this to 0. The last AnimationCurve shows time along the X-axis and the number of bytes sent on the Y-axis. It's not to be edited. It's useful for finding where bytes are used in a large capture. The third range specifies the range of the capture to show in the window. Note that this is useful together with the AnimationCurve as they should line up fairly well.


#### Profiler block
Each column represents one (or more ticks). If the tick has no events, multiple ticks may be combined and you will see only a number in an empty box. If there were events, the events are not combined and each column will have every event for that tick. At the very bottom, there is a smaller box with information about the Tick, such as what tick type it is and in what frame it occured. A tick can be of type Receive or Event. In each Tick (column) there can be one or more boxes. Each box represents an event that occurd. Events can be of type Send or Receive. Receive would indicate that the MLAPI received some amount of bytes while Send means that the MLAPI sent off a certain amount of bytes. Each block will state the type of message, the size in bytes, and what channel something was done over.

#### Usage
[YouTube Video](https://youtu.be/-icRrZGg6r8)
