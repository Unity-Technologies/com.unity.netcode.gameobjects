---
title: NetworkSceneManager
name: NetworkSceneManager
permalink: /api/network-scene-manager/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkSceneManager ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.SceneManagement</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Main class for managing network scenes</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` AddRuntimeSceneName(``string`` sceneName, ``uint`` index);</b></h4>
		<p>Adds a scene during runtime.
            The index is REQUIRED to be unique AND the same across all instances.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` sceneName</p>
			<p>Scene name.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` index</p>
			<p>Index.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``SceneSwitchProgress``](/api/scene-switch-progress/) SwitchScene(``string`` sceneName);</b></h4>
		<p>Switches to a scene with a given name. Can only be called from Server</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` sceneName</p>
			<p>The name of the scene to switch to</p>
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
