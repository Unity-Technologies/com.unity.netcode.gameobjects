# NetworkingConfiguration Fields
 

The <a href="T_MLAPI_NetworkingConfiguration">NetworkingConfiguration</a> type exposes the following members.


## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_Address">Address</a></td><td>
The address to connect to</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_AllowPassthroughMessages">AllowPassthroughMessages</a></td><td>
Wheter or not to allow any type of passthrough messages</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_Channels">Channels</a></td><td>
Channels used by the NetworkedTransport</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_ClientConnectionBufferTimeout">ClientConnectionBufferTimeout</a></td><td>
The amount of seconds to wait for handshake to complete before timing out a client</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_ConnectionApproval">ConnectionApproval</a></td><td>
Wheter or not to use connection approval</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_ConnectionApprovalCallback">ConnectionApprovalCallback</a></td><td>
The callback to invoke when a connection has to be decided if it should get approved</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_ConnectionData">ConnectionData</a></td><td>
The data to send during connection which can be used to decide on if a client should get accepted</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_EnableEncryption">EnableEncryption</a></td><td>
Wheter or not to enable encryption</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_EnableSceneSwitching">EnableSceneSwitching</a></td><td>
Wheter or not to enable scene switching</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_EncryptedChannels">EncryptedChannels</a></td><td>
Set of channels that will have all message contents encrypted when used</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_EventTickrate">EventTickrate</a></td><td>
The amount of times per second internal frame events will occur, examples include SyncedVar send checking.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_HandleObjectSpawning">HandleObjectSpawning</a></td><td>
Wheter or not to make the library handle object spawning</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_MaxConnections">MaxConnections</a></td><td>
The max amount of Clients that can connect.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_MaxReceiveEventsPerTickRate">MaxReceiveEventsPerTickRate</a></td><td>
The max amount of messages to process per ReceiveTickrate. This is to prevent flooding.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_MessageBufferSize">MessageBufferSize</a></td><td>
The size of the receive message buffer. This is the max message size.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_MessageTypes">MessageTypes</a></td><td>
Registered MessageTypes</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_PassthroughMessageTypes">PassthroughMessageTypes</a></td><td>
List of MessageTypes that can be passed through by Server. MessageTypes in this list should thus not be trusted to as great of an extent as normal messages.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_Port">Port</a></td><td>
The port for the NetworkTransport to use</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_ProtocolVersion">ProtocolVersion</a></td><td>
The protocol version. Different versions doesn't talk to each other.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_ReceiveTickrate">ReceiveTickrate</a></td><td>
Amount of times per second the receive queue is emptied and all messages inside are processed.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_RegisteredScenes">RegisteredScenes</a></td><td>
A list of SceneNames that can be used during networked games.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_RSAPrivateKey">RSAPrivateKey</a></td><td>
Private RSA XML key to use for signing key exchange</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_RSAPublicKey">RSAPublicKey</a></td><td>
Public RSA XML key to use for signing key exchange</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_SecondsHistory">SecondsHistory</a></td><td>
The amount of seconds to keep a lag compensation position history</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_SendTickrate">SendTickrate</a></td><td>
The amount of times per second every pending message will be sent away.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_MLAPI_NetworkingConfiguration_SignKeyExchange">SignKeyExchange</a></td><td>
Wheter or not to enable signed diffie hellman key exchange.</td></tr></table>&nbsp;
<a href="#networkingconfiguration-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_MLAPI_NetworkingConfiguration">NetworkingConfiguration Class</a><br /><a href="N_MLAPI">MLAPI Namespace</a><br />