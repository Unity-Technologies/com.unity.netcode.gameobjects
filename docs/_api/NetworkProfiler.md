---
title: NetworkProfiler
name: NetworkProfiler
permalink: /api/network-profiler/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkProfiler ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Profiling</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>NetworkProfiler for profiling network traffic</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``FixedQueue<ProfilerTick>`` Ticks { get; set; }</b></h4>
		<p>The ticks that has been recorded</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isRunning { get; }</b> <small><span class="label label-warning" title="Use IsRunning instead">Obsolete</span></small></h4>
		<p>Whether or not the profiler is recording data</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsRunning { get; set; }</b></h4>
		<p>Whether or not the profiler is recording data</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` Start(``int`` historyLength);</b></h4>
		<p>Starts recording data for the Profiler</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` historyLength</p>
			<p>The amount of ticks to keep in memory</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` Stop();</b></h4>
		<p>Stops recording data</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` Stop(``ProfilerTick[]&`` tickBuffer);</b></h4>
		<p>Stops recording data and fills the buffer with the recorded ticks and returns the length;</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ProfilerTick[]&`` tickBuffer</p>
			<p>The buffer to fill with the ticks</p>
		</div>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>The number of ticks recorded</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` Stop(``List`1&`` tickBuffer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List`1&`` tickBuffer</p>
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
