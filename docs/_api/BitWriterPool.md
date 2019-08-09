---
title: BitWriterPool
name: BitWriterPool
permalink: /api/bit-writer-pool/
---

<div style="line-height: 1;">
	<h2 markdown="1">BitWriterPool ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization.Pooled</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Static class containing PooledBitWriters</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``PooledBitWriter``](/api/pooled-bit-writer/) GetWriter(``Stream`` stream);</b></h4>
		<p>Retrieves a PooledBitWriter</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream the writer should write to</p>
		</div>
		<h5 markdown="1"><b>Returns [``PooledBitWriter``](/api/pooled-bit-writer/)</b></h5>
		<div>
			<p>A PooledBitWriter</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` PutBackInPool([``PooledBitWriter``](/api/pooled-bit-writer/) writer);</b></h4>
		<p>Puts a PooledBitWriter back into the pool</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``PooledBitWriter``](/api/pooled-bit-writer/) writer</p>
			<p>The writer to put in the pool</p>
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
