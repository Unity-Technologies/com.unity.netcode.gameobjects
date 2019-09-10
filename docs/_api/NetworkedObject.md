---
title: NetworkedObject
name: NetworkedObject
permalink: /api/networked-object/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkedObject ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A component used to identify that a GameObject is networked</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` NetworkId { get; set; }</b></h4>
		<p>Gets the unique ID of this object that is synced across the network</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` OwnerClientId { get; set; }</b></h4>
		<p>Gets the clientId of the owner of this NetworkedObject</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isPlayerObject { get; }</b> <small><span class="label label-warning" title="Use IsPlayerObject instead">Obsolete</span></small></h4>
		<p>Gets if this object is a player object</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsPlayerObject { get; set; }</b></h4>
		<p>Gets if this object is a player object</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isLocalPlayer { get; }</b> <small><span class="label label-warning" title="Use IsLocalPlayer instead">Obsolete</span></small></h4>
		<p>Gets if the object is the the personal clients player object</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsLocalPlayer { get; }</b></h4>
		<p>Gets if the object is the the personal clients player object</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isOwner { get; }</b> <small><span class="label label-warning" title="Use IsOwner instead">Obsolete</span></small></h4>
		<p>Gets if the object is owned by the local player or if the object is the local player object</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsOwner { get; }</b></h4>
		<p>Gets if the object is owned by the local player or if the object is the local player object</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isOwnedByServer { get; }</b> <small><span class="label label-warning" title="Use IsOwnedByServer instead">Obsolete</span></small></h4>
		<p>Gets Whether or not the object is owned by anyone</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsOwnedByServer { get; }</b></h4>
		<p>Gets Whether or not the object is owned by anyone</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isSpawned { get; }</b> <small><span class="label label-warning" title="Use IsSpawned instead">Obsolete</span></small></h4>
		<p>Gets if the object has yet been spawned across the network</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsSpawned { get; set; }</b></h4>
		<p>Gets if the object has yet been spawned across the network</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Nullable<bool>`` IsSceneObject { get; set; }</b></h4>
		<p>Gets if the object is a SceneObject, null if it's not yet spawned but is a scene object.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` DestroyWithScene { get; set; }</b></h4>
		<p>Gets whether or not the object should be automatically removed when the scene is unloaded.</p>
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
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` NetworkedInstanceId;</b></h4>
		<p>InstanceId is the id that is unique to the object and scene for a scene object when UsePrefabSync is false.
            If UsePrefabSync is true or if it's used on non scene objects, this has no effect.
            Should not be set manually</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` PrefabHash;</b></h4>
		<p>The Prefab unique hash. This should not be set my the user but rather changed by editing the PrefabHashGenerator.
            It has to be the same for all instances of a prefab</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` PrefabHashGenerator;</b></h4>
		<p>The generator used to change the PrefabHash. This should be set the same for all instances of a prefab.
            It has to be unique in relation to other prefabs</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` AlwaysReplicateAsRoot;</b></h4>
		<p>If true, the object will always be replicated as root on clients and the parent will be ignored.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``VisibilityDelegate``](/api/visibility-delegate/) CheckObjectVisibility;</b></h4>
		<p>Delegate invoked when the MLAPI needs to know if the object should be visible to a client, if null it will assume true</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``SpawnDelegate``](/api/spawn-delegate/) IncludeTransformWhenSpawning;</b></h4>
		<p>Delegate invoked when the MLAPI needs to know if it should include the transform when spawning the object, if null it will assume true</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` DontDestroyWithOwner;</b></h4>
		<p>Whether or not to destroy this object if it's owner is destroyed.
            If false, the objects ownership will be given to the server.</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkedObject``](/api/networked-object/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Enumerator<ulong>`` GetObservers();</b></h4>
		<p>Returns Observers enumerator</p>
		<h5 markdown="1"><b>Returns ``Enumerator<ulong>``</b></h5>
		<div>
			<p>Observers enumerator</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsNetworkVisibleTo(``ulong`` clientId);</b></h4>
		<p>Whether or not this object is visible to a specific client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId of the client</p>
		</div>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>True if the client knows about the object</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` NetworkShow(``ulong`` clientId, ``Stream`` payload);</b></h4>
		<p>Shows a previously hidden object to a client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The client to show the object to</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` payload</p>
			<p>An optional payload to send as part of the spawn</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` NetworkHide(``ulong`` clientId);</b></h4>
		<p>Hides a object from a specific client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The client to hide the object for</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Spawn(``Stream`` spawnPayload, ``bool`` destroyWithScene);</b></h4>
		<p>Spawns this GameObject across the network. Can only be called from the Server</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` spawnPayload</p>
			<p>The writer containing the spawn payload</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` destroyWithScene</p>
			<p>Should the object be destroyd when the scene is changed</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` UnSpawn();</b></h4>
		<p>Unspawns this GameObject and destroys it for other clients. This should be used if the object should be kept on the server</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SpawnWithOwnership(``ulong`` clientId, ``Stream`` spawnPayload, ``bool`` destroyWithScene);</b></h4>
		<p>Spawns an object across the network with a given owner. Can only be called from server</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId to own the object</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` spawnPayload</p>
			<p>The writer containing the spawn payload</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` destroyWithScene</p>
			<p>Should the object be destroyd when the scene is changed</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SpawnAsPlayerObject(``ulong`` clientId, ``Stream`` spawnPayload, ``bool`` destroyWithScene);</b></h4>
		<p>Spawns an object across the network and makes it the player object for the given client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId whos player object this is</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` spawnPayload</p>
			<p>The writer containing the spawn payload</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` destroyWithScene</p>
			<p>Should the object be destroyd when the scene is changed</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` RemoveOwnership();</b></h4>
		<p>Removes all ownership of an object from any client. Can only be called from server</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` ChangeOwnership(``ulong`` newOwnerClientId);</b></h4>
		<p>Changes the owner of the object. Can only be called from server</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` newOwnerClientId</p>
			<p>The new owner clientId</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` NetworkShow(``List<NetworkedObject>`` networkedObjects, ``ulong`` clientId, ``Stream`` payload);</b></h4>
		<p>Shows a list of previously hidden objects to a client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<NetworkedObject>`` networkedObjects</p>
			<p>The objects to show</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The client to show the objects to</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` payload</p>
			<p>An optional payload to send as part of the spawns</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` NetworkHide(``List<NetworkedObject>`` networkedObjects, ``ulong`` clientId);</b></h4>
		<p>Hides a list of objects from a client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<NetworkedObject>`` networkedObjects</p>
			<p>The objects to hide</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The client to hide the objects from</p>
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
