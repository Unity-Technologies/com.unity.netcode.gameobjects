---
title: BitReaderPool
name: BitReaderPool
permalink: /api/bit-reader-pool/
---

<div style="line-height: 1;">
	<h2 markdown="1">BitReaderPool ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization.Pooled</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Static class containing PooledBitReaders</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``PooledBitReader``](/api/pooled-bit-reader/) GetReader(``Stream`` stream);</b></h4>
		<p>Retrieves a PooledBitReader</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream the reader should read from</p>
		</div>
		<h5 markdown="1"><b>Returns [``PooledBitReader``](/api/pooled-bit-reader/)</b></h5>
		<div>
			<p>A PooledBitReader</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` PutBackInPool([``PooledBitReader``](/api/pooled-bit-reader/) reader);</b></h4>
		<p>Puts a PooledBitReader back into the pool</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``PooledBitReader``](/api/pooled-bit-reader/) reader</p>
			<p>The reader to put in the pool</p>
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
