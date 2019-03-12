---
title: NetworkedDictionaryEvent&lt;TKey,TValue&gt;
name: NetworkedDictionaryEvent<TKey,TValue>
permalink: /api/networked-dictionary-event%3C-tkey,-tvalue%3E/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkedDictionaryEvent&lt;TKey,TValue&gt; ``struct``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.NetworkedVar.Collections</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Struct containing event information about changes to a NetworkedDictionary.</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``NetworkedListEventType<TKey,TValue>`` eventType;</b></h4>
		<p>Enum representing the operation made to the dictionary.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``TKey`` key;</b></h4>
		<p>the key changed, added or removed if available.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``TValue`` value;</b></h4>
		<p>The value changed, added or removed if available.</p>
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
