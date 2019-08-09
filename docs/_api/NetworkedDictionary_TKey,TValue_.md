---
title: NetworkedDictionary&lt;TKey,TValue&gt;
name: NetworkedDictionary<TKey,TValue>
permalink: /api/networked-dictionary%3C-tkey,-tvalue%3E/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkedDictionary&lt;TKey,TValue&gt; ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.NetworkedVar.Collections</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Event based networkedVar container for syncing Dictionaries</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` LastSyncedTime { get; set; }</b></h4>
		<p>Gets the last time the variable was synced</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``TValue`` Item { get; set; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ICollection<TKey>`` Keys { get; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ICollection<TValue>`` Values { get; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` Count { get; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsReadOnly { get; }</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetworkedVarSettings``](/api/networked-var-settings/) Settings;</b></h4>
		<p>The settings for this container</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkedDictionary<TKey,TValue>``](/api/networked-dictionary%3C-tkey,-tvalue%3E/)();</b></h4>
		<p>Creates a NetworkedDictionary with the default value and settings</p>
	</div>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkedDictionary<TKey,TValue>``](/api/networked-dictionary%3C-tkey,-tvalue%3E/)([``NetworkedVarSettings``](/api/networked-var-settings/) settings);</b></h4>
		<p>Creates a NetworkedDictionary with the default value and custom settings</p>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``NetworkedVarSettings``](/api/networked-var-settings/) settings</p>
			<p>The settings to use for the NetworkedDictionary</p>
		</div>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkedDictionary<TKey,TValue>``](/api/networked-dictionary%3C-tkey,-tvalue%3E/)([``NetworkedVarSettings``](/api/networked-var-settings/) settings, ``IDictionary<TKey,TValue>`` value);</b></h4>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``NetworkedVarSettings``](/api/networked-var-settings/) settings</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IDictionary<TKey,TValue>`` value</p>
		</div>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkedDictionary<TKey,TValue>``](/api/networked-dictionary%3C-tkey,-tvalue%3E/)(``IDictionary<TKey,TValue>`` value);</b></h4>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IDictionary<TKey,TValue>`` value</p>
		</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` ResetDirty();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` GetChannel();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` ReadDelta(``Stream`` stream, ``bool`` keepDirtyDelta);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` keepDirtyDelta</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` ReadField(``Stream`` stream);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SetNetworkedBehaviour([``NetworkedBehaviour``](/api/networked-behaviour/) behaviour);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``NetworkedBehaviour``](/api/networked-behaviour/) behaviour</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` TryGetValue(``TKey`` key, ``TValue&`` value);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``TKey`` key</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``TValue&`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteDelta(``Stream`` stream);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteField(``Stream`` stream);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CanClientWrite(``ulong`` clientId);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CanClientRead(``ulong`` clientId);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsDirty();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Add(``TKey`` key, ``TValue`` value);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``TKey`` key</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``TValue`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Add(``KeyValuePair<TKey,TValue>`` item);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``KeyValuePair<TKey,TValue>`` item</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Clear();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Contains(``KeyValuePair<TKey,TValue>`` item);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``KeyValuePair<TKey,TValue>`` item</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` ContainsKey(``TKey`` key);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``TKey`` key</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CopyTo(``KeyValuePair`2[]`` array, ``int`` arrayIndex);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``KeyValuePair`2[]`` array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` arrayIndex</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``IEnumerator<KeyValuePair<TKey,TValue>>`` GetEnumerator();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Remove(``TKey`` key);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``TKey`` key</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Remove(``KeyValuePair<TKey,TValue>`` item);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``KeyValuePair<TKey,TValue>`` item</p>
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
