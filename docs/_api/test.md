---
title: NetworkingManager
permalink: /api/networking-manager/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkingManager ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The main component of the library</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` NetworkTime { get; set; }</b></h4>
		<p>A syncronized time, represents the time in seconds since the server application started. Is replicated across all clients</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``[NetworkingManager](/api/networking-manager/)`` singleton { get; set; }</b></h4>
		<p>The singleton instance of the NetworkingManager</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ServerClientId { get; }</b></h4>
		<p>Gets the networkId of the server</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` LocalClientId { get; set; }</b></h4>
		<p>The clientId the server calls the local client by, only valid for clients</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isServer { get; set; }</b></h4>
		<p>Gets wheter or not a server is running</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isClient { get; set; }</b></h4>
		<p>Gets wheter or not a client is running</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isHost { get; }</b></h4>
		<p>Gets if we are running as host</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isListening { get; set; }</b></h4>
		<p>Gets wheter or not we are listening for connections</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isConnectedClients { get; set; }</b></h4>
		<p>Gets if we are connected as a client</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ConnectedHostname { get; set; }</b></h4>
		<p>The current hostname we are connected to, used to validate certificate</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` useGUILayout { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` runInEditMode { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` enabled { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Behaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isActiveAndEnabled { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Behaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Transform`` transform { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``GameObject`` gameObject { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` tag { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` rigidbody { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` rigidbody2D { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` camera { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` light { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` animation { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` constantForce { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` renderer { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` audio { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiText { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` networkView { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiElement { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiTexture { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` collider { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` collider2D { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` hingeJoint { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` particleEmitter { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` particleSystem { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` name { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``HideFlags`` hideFlags { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
</div>
<br>
### Constructors

#### public NetworkingManager();


<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendCustomMessage(``List<uint>`` clientIds, ``[BitStream](/api/bit-stream/)`` stream, ``string`` channel);</b></h4>
		<p>Sends custom message to a list of clients</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<uint>`` clientIds</p>
			<p>The clients to send to, sends to everyone if null</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``[BitStream](/api/bit-stream/)`` stream</p>
			<p>The message stream containing the data</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` channel</p>
			<p>The channel to send the data on</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendCustomMessage(``uint`` clientId, ``[BitStream](/api/bit-stream/)`` stream, ``string`` channel);</b></h4>
		<p>Sends a custom message to a specific client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` clientId</p>
			<p>The client to send the message to</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``[BitStream](/api/bit-stream/)`` stream</p>
			<p>The message stream containing the data</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` channel</p>
			<p>The channel tos end the data on</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StartServer();</b></h4>
		<p>Starts a server</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StartClient();</b></h4>
		<p>Starts a client</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopServer();</b></h4>
		<p>Stops the running server</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopHost();</b></h4>
		<p>Stops the running host</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopClient();</b></h4>
		<p>Stops the running client</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StartHost(``Nullable<Vector3>`` pos, ``Nullable<Quaternion>`` rot, ``int`` prefabId);</b></h4>
		<p>Starts a Host</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Nullable<Vector3>`` pos</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Nullable<Quaternion>`` rot</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` prefabId</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsInvoking();</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CancelInvoke();</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Invoke(``string`` methodName, ``float`` time);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` time</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` InvokeRepeating(``string`` methodName, ``float`` time, ``float`` repeatRate);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` time</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` repeatRate</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CancelInvoke(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsInvoking(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine(``string`` methodName, ``object`` value);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine(``IEnumerator`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IEnumerator`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine_Auto(``IEnumerator`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IEnumerator`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopCoroutine(``IEnumerator`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IEnumerator`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopCoroutine(``Coroutine`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Coroutine`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopCoroutine(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopAllCoroutines();</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponent(``Type`` type);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponent();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponent(``string`` type);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` type</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponentInChildren(``Type`` t, ``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponentInChildren(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponentInChildren(``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponentInChildren();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInChildren(``Type`` t, ``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInChildren(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInChildren(``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponentsInChildren(``bool`` includeInactive, ``List<T>`` result);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` result</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInChildren();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponentsInChildren(``List<T>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponentInParent(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponentInParent();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInParent(``Type`` t, ``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInParent(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInParent(``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponentsInParent(``bool`` includeInactive, ``List<T>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInParent();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponents(``Type`` type);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponents(``Type`` type, ``List<Component>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<Component>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponents(``List<T>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponents();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CompareTag(``string`` tag);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` tag</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName, ``object`` value, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName, ``object`` value);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName, ``object`` value);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName, ``object`` value, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName, ``object`` parameter, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` parameter</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName, ``object`` parameter);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` parameter</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetInstanceID();</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetHashCode();</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Equals(``object`` other);</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` other</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Type`` GetType();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
	<br>
</div>
<br>
