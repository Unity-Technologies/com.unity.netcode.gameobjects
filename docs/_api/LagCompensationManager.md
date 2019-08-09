---
title: LagCompensationManager
name: LagCompensationManager
permalink: /api/lag-compensation-manager/
---

<div style="line-height: 1;">
	<h2 markdown="1">LagCompensationManager ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.LagCompensation</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The main class for controlling lag compensation</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<TrackedObject>`` simulationObjects { get; }</b> <small><span class="label label-warning" title="Use SimulationObjects instead">Obsolete</span></small></h4>
		<p>Simulation objects</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<TrackedObject>`` SimulationObjects;</b></h4>
		<p>Simulation objects</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` Simulate(``float`` secondsAgo, ``Action`` action);</b></h4>
		<p>Turns time back a given amount of seconds, invokes an action and turns it back</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` secondsAgo</p>
			<p>The amount of seconds</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Action`` action</p>
			<p>The action to invoke when time is turned back</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` Simulate(``ulong`` clientId, ``Action`` action);</b></h4>
		<p>Turns time back a given amount of seconds, invokes an action and turns it back. The time is based on the estimated RTT of a clientId</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId's RTT to use</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Action`` action</p>
			<p>The action to invoke when time is turned back</p>
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
