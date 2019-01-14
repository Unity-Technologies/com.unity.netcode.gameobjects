---
title: NetworkPoolManager
permalink: /api/network-pool-manager/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkPoolManager ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Components</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Main class for managing network pools</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` CreatePool(``string`` poolName, ``int`` spawnablePrefabIndex, ``uint`` size);</b></h4>
		<p>Creates a networked object pool. Can only be called from the server</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` poolName</p>
			<p>Name of the pool</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` spawnablePrefabIndex</p>
			<p>The index of the prefab to use in the spawnablePrefabs array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` size</p>
			<p>The amount of objects in the pool</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` DestroyPool(``string`` poolName);</b></h4>
		<p>This destroys an object pool and all of it's objects. Can only be called from the server</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` poolName</p>
			<p>The name of the pool</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``NetworkedObject``](/MLAPI/api/networked-object/) SpawnPoolObject(``string`` poolName, ``Vector3`` position, ``Quaternion`` rotation);</b></h4>
		<p>Spawns a object from the pool at a given position and rotation. Can only be called from server.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` poolName</p>
			<p>The name of the pool</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Vector3`` position</p>
			<p>The position to spawn the object at</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Quaternion`` rotation</p>
			<p>The rotation to spawn the object at</p>
		</div>
		<h5 markdown="1"><b>Returns [``NetworkedObject``](/MLAPI/api/networked-object/)</b></h5>
		<div>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` DestroyPoolObject([``NetworkedObject``](/MLAPI/api/networked-object/) netObject);</b></h4>
		<p>Destroys a NetworkedObject if it's part of a pool. Use this instead of the MonoBehaviour Destroy method. Can only be called from Server.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``NetworkedObject``](/MLAPI/api/networked-object/) netObject</p>
			<p>The NetworkedObject instance to destroy</p>
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
