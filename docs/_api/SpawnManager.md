---
title: SpawnManager
name: SpawnManager
permalink: /api/spawn-manager/
---

<div style="line-height: 1;">
	<h2 markdown="1">SpawnManager ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Spawning</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Class that handles object spawning</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Dictionary<ulong,NetworkedObject>`` SpawnedObjects;</b></h4>
		<p>The currently spawned objects</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<NetworkedObject>`` SpawnedObjectsList;</b></h4>
		<p>A list of the spawned objects</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` RegisterSpawnHandler(``ulong`` prefabHash, [``SpawnHandlerDelegate``](/api/spawn-handler-delegate/) handler);</b></h4>
		<p>Registers a delegate for spawning networked prefabs, useful for object pooling</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` prefabHash</p>
			<p>The prefab hash to spawn</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``SpawnHandlerDelegate``](/api/spawn-handler-delegate/) handler</p>
			<p>The delegate handler</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` RegisterCustomDestroyHandler(``ulong`` prefabHash, [``DestroyHandlerDelegate``](/api/destroy-handler-delegate/) handler);</b></h4>
		<p>Registers a delegate for destroying networked objects, useful for object pooling</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` prefabHash</p>
			<p>The prefab hash to destroy</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``DestroyHandlerDelegate``](/api/destroy-handler-delegate/) handler</p>
			<p>The delegate handler</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` RemoveCustomSpawnHandler(``ulong`` prefabHash);</b></h4>
		<p>Removes the custom spawn handler for a specific prefab hash</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` prefabHash</p>
			<p>The prefab hash of the prefab spawn handler that is to be removed</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` RemoveCustomDestroyHandler(``ulong`` prefabHash);</b></h4>
		<p>Removes the custom destroy handler for a specific prefab hash</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` prefabHash</p>
			<p>The prefab hash of the prefab destroy handler that is to be removed</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` GetNetworkedPrefabIndexOfHash(``ulong`` hash);</b></h4>
		<p>Gets the prefab index of a given prefab hash</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` hash</p>
			<p>The hash of the prefab</p>
		</div>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>The index of the prefab</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ulong`` GetPrefabHashFromIndex(``int`` index);</b></h4>
		<p>Returns the prefab hash for the networked prefab with a given index</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` index</p>
			<p>The networked prefab index</p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>The prefab hash for the given prefab index</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ulong`` GetPrefabHashFromGenerator(``string`` generator);</b></h4>
		<p>Returns the prefab hash for a given prefab hash generator</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` generator</p>
			<p>The prefab hash generator</p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>The hash for the given generator</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``NetworkedObject``](/api/networked-object/) GetLocalPlayerObject();</b></h4>
		<p>Returns the local player object or null if one does not exist</p>
		<h5 markdown="1"><b>Returns [``NetworkedObject``](/api/networked-object/)</b></h5>
		<div>
			<p>The local player object or null if one does not exist</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``NetworkedObject``](/api/networked-object/) GetPlayerObject(``ulong`` clientId);</b></h4>
		<p>Returns the player object with a given clientId or null if one does not exist</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
		</div>
		<h5 markdown="1"><b>Returns [``NetworkedObject``](/api/networked-object/)</b></h5>
		<div>
			<p>The player object with a given clientId or null if one does not exist</p>
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
