---
title: TickEvent
name: TickEvent
permalink: /api/tick-event/
---

<div style="line-height: 1;">
	<h2 markdown="1">TickEvent ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Profiling</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A event that can occur during a Event</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``TickType``](/api/tick-type/) EventType;</b></h4>
		<p>The type of evenmt</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` Bytes;</b></h4>
		<p>The amount of bytes sent or received</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ChannelName;</b></h4>
		<p>The name of the channel</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` MessageType;</b></h4>
		<p>The message type</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Closed;</b></h4>
		<p>Whether or not the event is closed</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``TickEvent``](/api/tick-event/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SerializeToStream(``Stream`` stream);</b></h4>
		<p>Writes the TickEvent data to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream to write the TickEvent data to</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``TickEvent``](/api/tick-event/) FromStream(``Stream`` stream);</b></h4>
		<p>Creates a TickEvent from data in the provided stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream containing the TickEvent data</p>
		</div>
		<h5 markdown="1"><b>Returns [``TickEvent``](/api/tick-event/)</b></h5>
		<div>
			<p>The TickEvent with data read from the stream</p>
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
