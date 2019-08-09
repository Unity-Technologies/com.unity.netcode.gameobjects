---
title: SceneSwitchProgress
name: SceneSwitchProgress
permalink: /api/scene-switch-progress/
---

<div style="line-height: 1;">
	<h2 markdown="1">SceneSwitchProgress ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.SceneManagement</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Class for tracking scene switching progress by server and clients.</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<ulong>`` DoneClients { get; }</b></h4>
		<p>List of clientIds of those clients that is done loading the scene.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` TimeAtInitiation { get; }</b></h4>
		<p>The NetworkTime time at the moment the scene switch was initiated by the server.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsCompleted { get; set; }</b></h4>
		<p>Is this scene switch progresses completed, all clients are done loading the scene or a timeout has occured.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isCompleted { get; }</b> <small><span class="label label-warning" title="Use IsCompleted instead">Obsolete</span></small></h4>
		<p>Is this scene switch progresses completed, all clients are done loading the scene or a timeout has occured.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsAllClientsDoneLoading { get; set; }</b></h4>
		<p>If all clients are done loading the scene, at the moment of completed.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isAllClientsDoneLoading { get; }</b> <small><span class="label label-warning" title="Use IsCompleted instead">Obsolete</span></small></h4>
		<p>If all clients are done loading the scene, at the moment of completed.</p>
	</div>
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
