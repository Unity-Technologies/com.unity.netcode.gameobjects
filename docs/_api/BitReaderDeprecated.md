---
title: BitReaderDeprecated
permalink: /api/bit-reader-deprecated/
---

<div style="line-height: 1;">
	<h2 markdown="1">BitReaderDeprecated ``class`` <small><span class="label label-warning" title="">Obsolete</span></small></h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` Remaining { get; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` BitLength { get; }</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ValueType`` ReadValueType();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` ReadValueTypeOrString();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` ReadBool();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` ReadFloat();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double`` ReadDouble();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` ReadByte();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SkipPadded();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort`` ReadUShort();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadUInt();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``sbyte`` ReadSByte();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short`` ReadShort();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ReadInt();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` ReadLong();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float[]`` ReadFloatArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadFloatArray(``float[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double[]`` ReadDoubleArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadDoubleArray(``double[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` ReadByteArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadByteArray(``byte[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort[]`` ReadUShortArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadUShortArray(``ushort[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint[]`` ReadUIntArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadUIntArray(``uint[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong[]`` ReadULongArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadULongArray(``ulong[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``sbyte[]`` ReadSByteArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadSByteArray(``sbyte[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``sbyte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short[]`` ReadShortArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadShortArray(``short[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int[]`` ReadIntArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadIntArray(``int[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long[]`` ReadLongArray(``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadLongArray(``long[]`` buffer, ``int`` known);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` known</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ReadString();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` ReadBits(``int`` bits);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bits</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` ReadULong();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Dispose();</b></h4>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``BitReaderDeprecated``](/MLAPI/api/bit-reader-deprecated/) Get(``byte[]`` readFrom);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` readFrom</p>
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
