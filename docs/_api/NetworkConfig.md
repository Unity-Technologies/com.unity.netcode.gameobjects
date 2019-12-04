---
title: NetworkConfig
name: NetworkConfig
permalink: /api/network-config/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkConfig ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Configuration</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The configuration object used to start server, client and hosts</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``X509Certificate2`` ServerX509Certificate { get; set; }</b></h4>
		<p>Gets the currently in use certificate</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` ServerX509CertificateBytes { get; }</b></h4>
		<p>Gets the cached binary representation of the server certificate that's used for handshaking</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort`` ProtocolVersion;</b></h4>
		<p>The protocol version. Different versions doesn't talk to each other.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``Transport``](/api/transport/) NetworkTransport;</b></h4>
		<p>The transport hosts the sever uses</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<string>`` RegisteredScenes;</b></h4>
		<p>A list of SceneNames that can be used during networked games.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` AllowRuntimeSceneChanges;</b></h4>
		<p>Whether or not runtime scene changes should be allowed and expected.
            If this is true, clients with different initial configurations will not work together.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<NetworkedPrefab>`` NetworkedPrefabs;</b></h4>
		<p>A list of spawnable prefabs</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CreatePlayerPrefab;</b></h4>
		<p>Whether or not a player object should be created by default. This value can be overriden on a case by case basis with ConnectionApproval.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ReceiveTickrate;</b></h4>
		<p>Amount of times per second the receive queue is emptied and all messages inside are processed.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` MaxReceiveEventsPerTickRate;</b></h4>
		<p>The max amount of messages to process per ReceiveTickrate. This is to prevent flooding.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` EventTickrate;</b></h4>
		<p>The amount of times per second internal frame events will occur, examples include SyncedVar send checking.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` MaxObjectUpdatesPerTick;</b></h4>
		<p>The maximum amount of NetworkedObject's to process per tick.
            This is useful to prevent the MLAPI from hanging a frame
            Set this to less than or equal to 0 for unlimited</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ClientConnectionBufferTimeout;</b></h4>
		<p>The amount of seconds to wait for handshake to complete before timing out a client</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` ConnectionApproval;</b></h4>
		<p>Whether or not to use connection approval</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` ConnectionData;</b></h4>
		<p>The data to send during connection which can be used to decide on if a client should get accepted</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` SecondsHistory;</b></h4>
		<p>The amount of seconds to keep a lag compensation position history</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` EnableTimeResync;</b></h4>
		<p>If your logic uses the NetworkedTime, this should probably be turned off. If however it's needed to maximize accuracy, this is recommended to be turned on</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` TimeResyncInterval;</b></h4>
		<p>If time resync is turned on, this specifies the interval between syncs in seconds.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` EnableNetworkedVar;</b></h4>
		<p>Whether or not to enable the NetworkedVar system. This system runs in the Update loop and will degrade performance, but it can be a huge convenience.
            Only turn it off if you have no need for the NetworkedVar system.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` EnsureNetworkedVarLengthSafety;</b></h4>
		<p>Whether or not to ensure that NetworkedVars can be read even if a client accidentally writes where its not allowed to. This costs some CPU and bandwdith.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` EnableSceneManagement;</b></h4>
		<p>Enables scene management. This will allow network scene switches and automatic scene diff corrections upon connect.
            SoftSynced scene objects wont work with this disabled. That means that disabling SceneManagement also enables PrefabSync.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` ForceSamePrefabs;</b></h4>
		<p>Whether or not the MLAPI should check for differences in the prefabs at connection.
            If you dynamically add prefabs at runtime, turn this OFF</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` UsePrefabSync;</b></h4>
		<p>If true, all NetworkedObject's need to be prefabs and all scene objects will be replaced on server side which causes all serialization to be lost. Useful for multi project setups
            If false, Only non scene objects have to be prefabs. Scene objects will be matched using their PrefabInstanceId which can be precomputed globally for a scene at build time. Useful for single projects</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` RecycleNetworkIds;</b></h4>
		<p>If true, NetworkIds will be reused after the NetworkIdRecycleDelay.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` NetworkIdRecycleDelay;</b></h4>
		<p>The amount of seconds a NetworkId has to be unused in order for it to be reused.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``HashSize``](/api/hash-size/) RpcHashSize;</b></h4>
		<p>Decides how many bytes to use for Rpc messaging. Leave this to 2 bytes unless you are facing hash collisions</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` LoadSceneTimeOut;</b></h4>
		<p>The amount of seconds to wait on all clients to load requested scene before the SwitchSceneProgress onComplete callback, that waits for all clients to complete loading, is called anyway.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` EnableEncryption;</b></h4>
		<p>Whether or not to enable the ECDHE key exchange to allow for encryption and authentication of messages</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` SignKeyExchange;</b></h4>
		<p>Whether or not to enable signed diffie hellman key exchange.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ServerBase64PfxCertificate;</b></h4>
		<p>Pfx file in base64 encoding containing private and public key</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkConfig``](/api/network-config/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToBase64();</b></h4>
		<p>Returns a base64 encoded version of the config</p>
		<h5 markdown="1"><b>Returns ``string``</b></h5>
		<div>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` FromBase64(``string`` base64);</b></h4>
		<p>Sets the NetworkConfig data with that from a base64 encoded version</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` base64</p>
			<p>The base64 encoded version</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` GetConfig(``bool`` cache);</b></h4>
		<p>Gets a SHA256 hash of parts of the NetworkingConfiguration instance</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` cache</p>
			<p></p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CompareConfig(``ulong`` hash);</b></h4>
		<p>Compares a SHA256 hash with the current NetworkingConfiguration instances hash</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` hash</p>
			<p></p>
		</div>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p></p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Equals(``object`` obj);</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` obj</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetHashCode();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Type`` GetType();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
</div>
<br>
