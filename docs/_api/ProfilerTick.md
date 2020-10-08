---
title: ProfilerTick
name: ProfilerTick
permalink: /api/profiler-tick/
---

<div style="line-height: 1;">
	<h2 markdown="1">ProfilerTick ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Profiling</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A tick in used for the Profiler</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` Bytes { get; }</b></h4>
		<p>The amount of bytes that were sent and / or received during this tick</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<TickEvent>`` Events;</b></h4>
		<p>The events that occured during this tick</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``TickType``](/api/tick-type/) Type;</b></h4>
		<p>The type of tick</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` Frame;</b></h4>
		<p>The frame the tick executed on</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` EventId;</b></h4>
		<p>The id of the tick</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``ProfilerTick``](/api/profiler-tick/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SerializeToStream(``Stream`` stream);</b></h4>
		<p>Writes the current ProfilerTick to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream containing</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``ProfilerTick``](/api/profiler-tick/) FromStream(``Stream`` stream);</b></h4>
		<p>Creates a ProfilerTick from data in the provided stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream containing the ProfilerTick data</p>
		</div>
		<h5 markdown="1"><b>Returns [``ProfilerTick``](/api/profiler-tick/)</b></h5>
		<div>
			<p>The ProfilerTick with data read from the stream</p>
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
