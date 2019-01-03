---
title: Arithmetic
permalink: /api/arithmetic/
---

<div style="line-height: 1;">
	<h2 markdown="1">Arithmetic ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` SIGN_BIT_64;</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` SIGN_BIT_32;</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short`` SIGN_BIT_16;</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``sbyte`` SIGN_BIT_8;</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ulong`` CeilingExact(``ulong`` u1, ``ulong`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` u2</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``long`` CeilingExact(``long`` u1, ``long`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` u2</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``uint`` CeilingExact(``uint`` u1, ``uint`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` u2</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` CeilingExact(``int`` u1, ``int`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` u2</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ushort`` CeilingExact(``ushort`` u1, ``ushort`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort`` u2</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``short`` CeilingExact(``short`` u1, ``short`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short`` u2</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``byte`` CeilingExact(``byte`` u1, ``byte`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` u2</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``sbyte`` CeilingExact(``sbyte`` u1, ``sbyte`` u2);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``sbyte`` u1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``sbyte`` u2</p>
		</div>
	</div>
	<br>
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
