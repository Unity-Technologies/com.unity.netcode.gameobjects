---
title: BitStreamPool
name: BitStreamPool
permalink: /api/bit-stream-pool/
---

<div style="line-height: 1;">
	<h2 markdown="1">BitStreamPool ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization.Pooled</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Static class containing PooledBitStreams</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``PooledBitStream``](/api/pooled-bit-stream/) GetStream();</b></h4>
		<p>Retrieves an expandable PooledBitStream from the pool</p>
		<h5 markdown="1"><b>Returns [``PooledBitStream``](/api/pooled-bit-stream/)</b></h5>
		<div>
			<p>An expandable PooledBitStream</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` PutBackInPool([``PooledBitStream``](/api/pooled-bit-stream/) stream);</b></h4>
		<p>Puts a PooledBitStream back into the pool</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``PooledBitStream``](/api/pooled-bit-stream/) stream</p>
			<p>The stream to put in the pool</p>
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
