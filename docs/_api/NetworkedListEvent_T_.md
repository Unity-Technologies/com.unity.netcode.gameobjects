---
title: NetworkedListEvent&lt;T&gt;
name: NetworkedListEvent<T>
permalink: /api/networked-list-event%3C-t%3E/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkedListEvent&lt;T&gt; ``struct``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.NetworkedVar.Collections</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Struct containing event information about changes to a NetworkedList.</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``EventType<T>`` eventType;</b></h4>
		<p>Enum representing the operation made to the list.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` value;</b></h4>
		<p>The value changed, added or removed if available.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` index;</b></h4>
		<p>the index changed, added or removed if available</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Equals(``object`` obj);</b></h4>
		<h5 markdown="1">Inherited from: ``ValueType``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` obj</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetHashCode();</b></h4>
		<h5 markdown="1">Inherited from: ``ValueType``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: ``ValueType``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Type`` GetType();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
</div>
<br>
