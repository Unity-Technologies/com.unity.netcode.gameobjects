---
title: Transport
name: Transport
permalink: /api/transport/
---

<div style="line-height: 1;">
	<h2 markdown="1">Transport ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Transports</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A network transport</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` ServerClientId { get; }</b></h4>
		<p>A constant clientId that represents the server.
            When this value is found in methods such as Send, it should be treated as a placeholder that means "the server"</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsSupported { get; }</b></h4>
		<p>Gets a value indicating whether this  is supported in the current runtime context.
            This is used by multiplex adapters.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``TransportChannel[]`` MLAPI_CHANNELS { get; }</b></h4>
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
		<h4 markdown="1"><b>public ``Component`` rigidbody { get; }</b> <small><span class="label label-warning" title="Property rigidbody has been deprecated. Use GetComponent<Rigidbody>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` rigidbody2D { get; }</b> <small><span class="label label-warning" title="Property rigidbody2D has been deprecated. Use GetComponent<Rigidbody2D>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` camera { get; }</b> <small><span class="label label-warning" title="Property camera has been deprecated. Use GetComponent<Camera>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` light { get; }</b> <small><span class="label label-warning" title="Property light has been deprecated. Use GetComponent<Light>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` animation { get; }</b> <small><span class="label label-warning" title="Property animation has been deprecated. Use GetComponent<Animation>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` constantForce { get; }</b> <small><span class="label label-warning" title="Property constantForce has been deprecated. Use GetComponent<ConstantForce>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` renderer { get; }</b> <small><span class="label label-warning" title="Property renderer has been deprecated. Use GetComponent<Renderer>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` audio { get; }</b> <small><span class="label label-warning" title="Property audio has been deprecated. Use GetComponent<AudioSource>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiText { get; }</b> <small><span class="label label-warning" title="Property guiText has been deprecated. Use GetComponent<GUIText>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` networkView { get; }</b> <small><span class="label label-warning" title="Property networkView has been deprecated. Use GetComponent<NetworkView>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiElement { get; }</b> <small><span class="label label-warning" title="Property guiElement has been deprecated. Use GetComponent<GUIElement>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiTexture { get; }</b> <small><span class="label label-warning" title="Property guiTexture has been deprecated. Use GetComponent<GUITexture>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` collider { get; }</b> <small><span class="label label-warning" title="Property collider has been deprecated. Use GetComponent<Collider>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` collider2D { get; }</b> <small><span class="label label-warning" title="Property collider2D has been deprecated. Use GetComponent<Collider2D>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` hingeJoint { get; }</b> <small><span class="label label-warning" title="Property hingeJoint has been deprecated. Use GetComponent<HingeJoint>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` particleEmitter { get; }</b> <small><span class="label label-warning" title="Property particleEmitter has been deprecated. Use GetComponent<ParticleEmitter>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` particleSystem { get; }</b> <small><span class="label label-warning" title="Property particleSystem has been deprecated. Use GetComponent<ParticleSystem>() instead. (UnityUpgradable)">Obsolete</span></small></h4>
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
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Send(``ulong`` clientId, ``ArraySegment<byte>`` data, ``string`` channelName);</b></h4>
		<p>Send a payload to the specified clientId, data and channelName.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId to send to</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ArraySegment<byte>`` data</p>
			<p>The data to send</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` channelName</p>
			<p>The channel to send data to</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetEventType``](/api/net-event-type/) PollEvent(``UInt64&`` clientId, ``String&`` channelName, ``ArraySegment`1&`` payload, ``Single&`` receiveTime);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``UInt64&`` clientId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``String&`` channelName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ArraySegment`1&`` payload</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Single&`` receiveTime</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``SocketTasks``](/api/socket-tasks/) StartClient();</b></h4>
		<p>Connects client to server</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``SocketTasks``](/api/socket-tasks/) StartServer();</b></h4>
		<p>Starts to listen for incoming clients.</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` DisconnectRemoteClient(``ulong`` clientId);</b></h4>
		<p>Disconnects a client from the server</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId to disconnect</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` DisconnectLocalClient();</b></h4>
		<p>Disconnects the local client from the server</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` GetCurrentRtt(``ulong`` clientId);</b></h4>
		<p>Gets the round trip time for a specific client. This method is optional</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId to get the rtt from</p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>Returns the round trip time in milliseconds</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Shutdown();</b></h4>
		<p>Shuts down the transport</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Init();</b></h4>
		<p>Initializes the transport</p>
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
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine_Auto(``IEnumerator`` routine);</b> <small><span class="label label-warning" title="StartCoroutine_Auto has been deprecated. Use StartCoroutine instead (UnityUpgradable) -> StartCoroutine([mscorlib] System.Collections.IEnumerator)">Obsolete</span></small></h4>
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
</div>
<br>
