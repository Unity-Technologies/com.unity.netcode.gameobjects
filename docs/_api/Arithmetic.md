---
title: Arithmetic
name: Arithmetic
permalink: /api/arithmetic/
---

<div style="line-height: 1;">
	<h2 markdown="1">Arithmetic ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Arithmetic helper class</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ulong`` ZigZagEncode(``long`` value);</b></h4>
		<p>ZigZag encodes a signed integer and maps it to a unsigned integer</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` value</p>
			<p>The signed integer to encode</p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>A ZigZag encoded version of the integer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``long`` ZigZagDecode(``ulong`` value);</b></h4>
		<p>Decides a ZigZag encoded integer back to a signed integer</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` value</p>
			<p>The unsigned integer</p>
		</div>
		<h5 markdown="1"><b>Returns ``long``</b></h5>
		<div>
			<p>The signed version of the integer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` VarIntSize(``ulong`` value);</b></h4>
		<p>Gets the output size in bytes after VarInting a unsigned integer</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` value</p>
			<p>The unsigned integer whose length to get</p>
		</div>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>The amount of bytes</p>
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
