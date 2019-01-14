---
title: PooledBitReader
permalink: /api/pooled-bit-reader/
---

<div style="line-height: 1;">
	<h2 markdown="1">PooledBitReader ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Disposable BitReader that returns the Reader to the BitReaderPool when disposed</p>

<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Dispose();</b></h4>
		<p>Returns the PooledBitReader into the static BitReaderPool</p>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``PooledBitReader``](/MLAPI/api/pooled-bit-reader/) Get(``Stream`` stream);</b></h4>
		<p>Gets a PooledBitReader from the static BitReaderPool</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
		</div>
		<h5 markdown="1"><b>Returns [``PooledBitReader``](/MLAPI/api/pooled-bit-reader/)</b></h5>
		<div>
			<p>PooledBitReader</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SetStream(``Stream`` stream);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Changes the underlying stream the reader is reading from</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream to read from</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ReadByte();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Reads a single byte</p>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>The byte read as an integer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` ReadByteDirect();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Reads a byte</p>
		<h5 markdown="1"><b>Returns ``byte``</b></h5>
		<div>
			<p>The byte read</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` ReadBit();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Reads a single bit</p>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>The bit read</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` ReadBool();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Reads a single bit</p>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>The bit read</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SkipPadBits();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Skips pad bits and aligns the position to the next byte</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` ReadObjectPacked(``Type`` type);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Reads a single boxed object of a given type in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
			<p>The type to read</p>
		</div>
		<h5 markdown="1"><b>Returns ``object``</b></h5>
		<div>
			<p>Returns the boxed read object</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` ReadSingle();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a single-precision floating point value from the stream.</p>
		<h5 markdown="1"><b>Returns ``float``</b></h5>
		<div>
			<p>The read value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double`` ReadDouble();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a double-precision floating point value from the stream.</p>
		<h5 markdown="1"><b>Returns ``double``</b></h5>
		<div>
			<p>The read value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` ReadSinglePacked();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a single-precision floating point value from the stream from a varint</p>
		<h5 markdown="1"><b>Returns ``float``</b></h5>
		<div>
			<p>The read value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double`` ReadDoublePacked();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a double-precision floating point value from the stream as a varint</p>
		<h5 markdown="1"><b>Returns ``double``</b></h5>
		<div>
			<p>The read value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Vector2`` ReadVector2();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Vector2 from the stream.</p>
		<h5 markdown="1"><b>Returns ``Vector2``</b></h5>
		<div>
			<p>The Vector2 read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Vector2`` ReadVector2Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Vector2 from the stream.</p>
		<h5 markdown="1"><b>Returns ``Vector2``</b></h5>
		<div>
			<p>The Vector2 read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Vector3`` ReadVector3();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Vector3 from the stream.</p>
		<h5 markdown="1"><b>Returns ``Vector3``</b></h5>
		<div>
			<p>The Vector3 read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Vector3`` ReadVector3Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Vector3 from the stream.</p>
		<h5 markdown="1"><b>Returns ``Vector3``</b></h5>
		<div>
			<p>The Vector3 read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Vector4`` ReadVector4();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Vector4 from the stream.</p>
		<h5 markdown="1"><b>Returns ``Vector4``</b></h5>
		<div>
			<p>The Vector4 read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Vector4`` ReadVector4Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Vector4 from the stream.</p>
		<h5 markdown="1"><b>Returns ``Vector4``</b></h5>
		<div>
			<p>The Vector4 read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Color`` ReadColor();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Color from the stream.</p>
		<h5 markdown="1"><b>Returns ``Color``</b></h5>
		<div>
			<p>The Color read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Color`` ReadColorPacked();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Color from the stream.</p>
		<h5 markdown="1"><b>Returns ``Color``</b></h5>
		<div>
			<p>The Color read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Color32`` ReadColor32();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Color32 from the stream.</p>
		<h5 markdown="1"><b>Returns ``Color32``</b></h5>
		<div>
			<p>The Color32 read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Ray`` ReadRay();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Ray from the stream.</p>
		<h5 markdown="1"><b>Returns ``Ray``</b></h5>
		<div>
			<p>The Ray read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Ray`` ReadRayPacked();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a Ray from the stream.</p>
		<h5 markdown="1"><b>Returns ``Ray``</b></h5>
		<div>
			<p>The Ray read from the stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` ReadRangedSingle(``float`` minValue, ``float`` maxValue, ``int`` bytes);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a single-precision floating point value from the stream. The value is between (inclusive) the minValue and maxValue.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` minValue</p>
			<p>Minimum value that this value could be</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` maxValue</p>
			<p>Maximum possible value that this could be</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bytes</p>
			<p>How many bytes the compressed value occupies. Must be between 1 and 4 (inclusive)</p>
		</div>
		<h5 markdown="1"><b>Returns ``float``</b></h5>
		<div>
			<p>The read value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double`` ReadRangedDouble(``double`` minValue, ``double`` maxValue, ``int`` bytes);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>read a double-precision floating point value from the stream. The value is between (inclusive) the minValue and maxValue.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double`` minValue</p>
			<p>Minimum value that this value could be</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double`` maxValue</p>
			<p>Maximum possible value that this could be</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bytes</p>
			<p>How many bytes the compressed value occupies. Must be between 1 and 8 (inclusive)</p>
		</div>
		<h5 markdown="1"><b>Returns ``double``</b></h5>
		<div>
			<p>The read value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Quaternion`` ReadRotation(``int`` bytesPerAngle);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a rotation from the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bytesPerAngle</p>
			<p>How many bytes each angle occupies. Must be between 1 and 4 (inclusive)</p>
		</div>
		<h5 markdown="1"><b>Returns ``Quaternion``</b></h5>
		<div>
			<p>The rotation read from the stream</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` ReadBits(``int`` bitCount);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a certain amount of bits from the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bitCount</p>
			<p>How many bits to read. Minimum 0, maximum 8.</p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>The bits that were read</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` ReadByteBits(``int`` bitCount);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a certain amount of bits from the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bitCount</p>
			<p>How many bits to read. Minimum 0, maximum 64.</p>
		</div>
		<h5 markdown="1"><b>Returns ``byte``</b></h5>
		<div>
			<p>The bits that were read</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` ReadNibble(``bool`` asUpper);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a nibble (4 bits) from the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` asUpper</p>
			<p>Whether or not the nibble should be left-shifted by 4 bits</p>
		</div>
		<h5 markdown="1"><b>Returns ``byte``</b></h5>
		<div>
			<p>The nibble that was read</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` ReadNibble();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a nibble (4 bits) from the stream.</p>
		<h5 markdown="1"><b>Returns ``byte``</b></h5>
		<div>
			<p>The nibble that was read</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``sbyte`` ReadSByte();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Reads a signed byte</p>
		<h5 markdown="1"><b>Returns ``sbyte``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort`` ReadUInt16();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read an unsigned short (UInt16) from the stream.</p>
		<h5 markdown="1"><b>Returns ``ushort``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short`` ReadInt16();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a signed short (Int16) from the stream.</p>
		<h5 markdown="1"><b>Returns ``short``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Char`` ReadChar();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a single character from the stream</p>
		<h5 markdown="1"><b>Returns ``Char``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadUInt32();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read an unsigned int (UInt32) from the stream.</p>
		<h5 markdown="1"><b>Returns ``uint``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ReadInt32();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a signed int (Int32) from the stream.</p>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` ReadUInt64();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read an unsigned long (UInt64) from the stream.</p>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` ReadInt64();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a signed long (Int64) from the stream.</p>
		<h5 markdown="1"><b>Returns ``long``</b></h5>
		<div>
			<p>Value read from stream.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short`` ReadInt16Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a ZigZag encoded varint signed short (Int16) from the stream.</p>
		<h5 markdown="1"><b>Returns ``short``</b></h5>
		<div>
			<p>Decoded un-varinted value.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort`` ReadUInt16Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a varint unsigned short (UInt16) from the stream.</p>
		<h5 markdown="1"><b>Returns ``ushort``</b></h5>
		<div>
			<p>Un-varinted value.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Char`` ReadCharPacked();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a varint two-byte character from the stream.</p>
		<h5 markdown="1"><b>Returns ``Char``</b></h5>
		<div>
			<p>Un-varinted value.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ReadInt32Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a ZigZag encoded varint signed int (Int32) from the stream.</p>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>Decoded un-varinted value.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ReadUInt32Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a varint unsigned int (UInt32) from the stream.</p>
		<h5 markdown="1"><b>Returns ``uint``</b></h5>
		<div>
			<p>Un-varinted value.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` ReadInt64Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a ZigZag encoded varint signed long(Int64) from the stream.</p>
		<h5 markdown="1"><b>Returns ``long``</b></h5>
		<div>
			<p>Decoded un-varinted value.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` ReadUInt64Packed();</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<p>Read a varint unsigned long (UInt64) from the stream.</p>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>Un-varinted value.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadString(``bool`` oneByteChars);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` oneByteChars</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadString(``StringBuilder`` builder, ``bool`` oneByteChars);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``StringBuilder`` builder</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` oneByteChars</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadStringPacked(``StringBuilder`` builder);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``StringBuilder`` builder</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadStringDiff(``string`` compare, ``bool`` oneByteChars);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` compare</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` oneByteChars</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadStringDiff(``StringBuilder`` builder, ``string`` compare, ``bool`` oneByteChars);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``StringBuilder`` builder</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` compare</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` oneByteChars</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadStringDiff(``StringBuilder`` compareAndBuffer, ``bool`` oneByteChars);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``StringBuilder`` compareAndBuffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` oneByteChars</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadStringPackedDiff(``string`` compare);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` compare</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadStringPackedDiff(``StringBuilder`` builder, ``string`` compare);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``StringBuilder`` builder</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` compare</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``StringBuilder`` ReadStringPackedDiff(``StringBuilder`` compareAndBuffer);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``StringBuilder`` compareAndBuffer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` ReadByteArray(``byte[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` ReadByteArrayDiff(``byte[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short[]`` ReadShortArray(``short[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short[]`` ReadShortArrayPacked(``short[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short[]`` ReadShortArrayDiff(``short[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``short[]`` ReadShortArrayPackedDiff(``short[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort[]`` ReadUShortArray(``ushort[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort[]`` ReadUShortArrayPacked(``ushort[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort[]`` ReadUShortArrayDiff(``ushort[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort[]`` ReadUShortArrayPackedDiff(``ushort[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int[]`` ReadIntArray(``int[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int[]`` ReadIntArrayPacked(``int[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int[]`` ReadIntArrayDiff(``int[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int[]`` ReadIntArrayPackedDiff(``int[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint[]`` ReadUIntArray(``uint[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint[]`` ReadUIntArrayPacked(``uint[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint[]`` ReadUIntArrayDiff(``uint[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long[]`` ReadLongArray(``long[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long[]`` ReadLongArrayPacked(``long[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long[]`` ReadLongArrayDiff(``long[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long[]`` ReadLongArrayPackedDiff(``long[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong[]`` ReadULongArray(``ulong[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong[]`` ReadULongArrayPacked(``ulong[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong[]`` ReadULongArrayDiff(``ulong[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong[]`` ReadULongArrayPackedDiff(``ulong[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float[]`` ReadFloatArray(``float[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float[]`` ReadFloatArrayPacked(``float[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float[]`` ReadFloatArrayDiff(``float[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float[]`` ReadFloatArrayPackedDiff(``float[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double[]`` ReadDoubleArray(``double[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double[]`` ReadDoubleArrayPacked(``double[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double[]`` ReadDoubleArrayDiff(``double[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``double[]`` ReadDoubleArrayPackedDiff(``double[]`` readTo, ``long`` knownLength);</b></h4>
		<h5 markdown="1">Inherited from: [``BitReader``](/MLAPI/api/bit-reader/)</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` readTo</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` knownLength</p>
		</div>
	</div>
	<br>
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
