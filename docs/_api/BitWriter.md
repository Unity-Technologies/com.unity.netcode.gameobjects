---
title: BitWriter
name: BitWriter
permalink: /api/bit-writer/
---

<div style="line-height: 1;">
	<h2 markdown="1">BitWriter ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A BinaryWriter that can do bit wise manipulation when backed by a BitStream</p>

<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``BitWriter``](/api/bit-writer/)(``Stream`` stream);</b></h4>
		<p>Creates a new BitWriter backed by a given stream</p>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream to use for writing</p>
		</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SetStream(``Stream`` stream);</b></h4>
		<p>Changes the underlying stream the writer is writing to</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream to write to</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteObjectPacked(``object`` value);</b></h4>
		<p>Writes a boxed object in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
			<p>The object to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteSingle(``float`` value);</b></h4>
		<p>Write single-precision floating point value to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteDouble(``double`` value);</b></h4>
		<p>Write double-precision floating point value to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteSinglePacked(``float`` value);</b></h4>
		<p>Write single-precision floating point value to the stream as a varint</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteDoublePacked(``double`` value);</b></h4>
		<p>Write double-precision floating point value to the stream as a varint</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteRay(``Ray`` ray);</b></h4>
		<p>Convenience method that writes two non-packed Vector3 from the ray to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Ray`` ray</p>
			<p>Ray to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteRayPacked(``Ray`` ray);</b></h4>
		<p>Convenience method that writes two packed Vector3 from the ray to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Ray`` ray</p>
			<p>Ray to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteColor(``Color`` color);</b></h4>
		<p>Convenience method that writes four non-varint floats from the color to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Color`` color</p>
			<p>Color to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteColorPacked(``Color`` color);</b></h4>
		<p>Convenience method that writes four varint floats from the color to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Color`` color</p>
			<p>Color to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteColor32(``Color32`` color32);</b></h4>
		<p>Convenience method that writes four non-varint floats from the color to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Color32`` color32</p>
			<p>Color32 to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteVector2(``Vector2`` vector2);</b></h4>
		<p>Convenience method that writes two non-varint floats from the vector to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Vector2`` vector2</p>
			<p>Vector to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteVector2Packed(``Vector2`` vector2);</b></h4>
		<p>Convenience method that writes two varint floats from the vector to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Vector2`` vector2</p>
			<p>Vector to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteVector3(``Vector3`` vector3);</b></h4>
		<p>Convenience method that writes three non-varint floats from the vector to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Vector3`` vector3</p>
			<p>Vector to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteVector3Packed(``Vector3`` vector3);</b></h4>
		<p>Convenience method that writes three varint floats from the vector to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Vector3`` vector3</p>
			<p>Vector to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteVector4(``Vector4`` vector4);</b></h4>
		<p>Convenience method that writes four non-varint floats from the vector to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Vector4`` vector4</p>
			<p>Vector to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteVector4Packed(``Vector4`` vector4);</b></h4>
		<p>Convenience method that writes four varint floats from the vector to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Vector4`` vector4</p>
			<p>Vector to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteRangedSingle(``float`` value, ``float`` minValue, ``float`` maxValue, ``int`` bytes);</b></h4>
		<p>Write a single-precision floating point value to the stream. The value is between (inclusive) the minValue and maxValue.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` value</p>
			<p>Value to write</p>
		</div>
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
			<p>How many bytes the compressed result should occupy. Must be between 1 and 4 (inclusive)</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteRangedDouble(``double`` value, ``double`` minValue, ``double`` maxValue, ``int`` bytes);</b></h4>
		<p>Write a double-precision floating point value to the stream. The value is between (inclusive) the minValue and maxValue.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double`` value</p>
			<p>Value to write</p>
		</div>
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
			<p>How many bytes the compressed result should occupy. Must be between 1 and 8 (inclusive)</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteRotationPacked(``Quaternion`` rotation);</b></h4>
		<p>Writes the rotation to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Quaternion`` rotation</p>
			<p>Rotation to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteRotation(``Quaternion`` rotation, ``int`` bytesPerAngle);</b> <small><span class="label label-warning" title="Use WriteRotationPacked instead">Obsolete</span></small></h4>
		<p>Writes the rotation to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Quaternion`` rotation</p>
			<p>Rotation to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bytesPerAngle</p>
			<p>Unused</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteRotation(``Quaternion`` rotation);</b></h4>
		<p>Writes the rotation to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Quaternion`` rotation</p>
			<p>Rotation to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteBit(``bool`` bit);</b></h4>
		<p>Writes a single bit</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` bit</p>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteBool(``bool`` value);</b></h4>
		<p>Writes a bool as a single bit</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` value</p>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WritePadBits();</b></h4>
		<p>Writes pad bits to make the underlying stream aligned</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteNibble(``byte`` value);</b></h4>
		<p>Write the lower half (lower nibble) of a byte.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` value</p>
			<p>Value containing nibble to write.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteNibble(``byte`` value, ``bool`` upper);</b></h4>
		<p>Write either the upper or lower nibble of a byte to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` value</p>
			<p>Value holding the nibble</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` upper</p>
			<p>Whether or not the upper nibble should be written. True to write the four high bits, else writes the four low bits.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteBits(``ulong`` value, ``int`` bitCount);</b></h4>
		<p>Write s certain amount of bits to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` value</p>
			<p>Value to get bits from.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bitCount</p>
			<p>Amount of bits to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteBits(``byte`` value, ``int`` bitCount);</b></h4>
		<p>Write bits to stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` value</p>
			<p>Value to get bits from.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bitCount</p>
			<p>Amount of bits to write.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteSByte(``sbyte`` value);</b></h4>
		<p>Write a signed byte to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``sbyte`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteChar(``Char`` c);</b></h4>
		<p>Write a single character to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char`` c</p>
			<p>Character to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUInt16(``ushort`` value);</b></h4>
		<p>Write an unsigned short (UInt16) to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteInt16(``short`` value);</b></h4>
		<p>Write a signed short (Int16) to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUInt32(``uint`` value);</b></h4>
		<p>Write an unsigned int (UInt32) to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteInt32(``int`` value);</b></h4>
		<p>Write a signed int (Int32) to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUInt64(``ulong`` value);</b></h4>
		<p>Write an unsigned long (UInt64) to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteInt64(``long`` value);</b></h4>
		<p>Write a signed long (Int64) to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteInt16Packed(``short`` value);</b></h4>
		<p>Write a signed short (Int16) as a ZigZag encoded varint to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUInt16Packed(``ushort`` value);</b></h4>
		<p>Write an unsigned short (UInt16) as a varint to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteCharPacked(``Char`` c);</b></h4>
		<p>Write a two-byte character as a varint to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char`` c</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteInt32Packed(``int`` value);</b></h4>
		<p>Write a signed int (Int32) as a ZigZag encoded varint to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUInt32Packed(``uint`` value);</b></h4>
		<p>Write an unsigned int (UInt32) as a varint to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteInt64Packed(``long`` value);</b></h4>
		<p>Write a signed long (Int64) as a ZigZag encoded varint to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUInt64Packed(``ulong`` value);</b></h4>
		<p>Write an unsigned long (UInt64) as a varint to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteByte(``byte`` value);</b></h4>
		<p>Write a byte to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` value</p>
			<p>Value to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteString(``string`` s, ``bool`` oneByteChars);</b></h4>
		<p>Writes a string</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` s</p>
			<p>The string to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` oneByteChars</p>
			<p>Whether or not to use one byte per character. This will only allow ASCII</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteStringPacked(``string`` s);</b></h4>
		<p>Writes a string in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` s</p>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteStringDiff(``string`` write, ``string`` compare, ``bool`` oneByteChars);</b></h4>
		<p>Writes the diff between two strings</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` oneByteChars</p>
			<p>Whether or not to use single byte chars. This will only allow ASCII characters</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteStringPackedDiff(``string`` write, ``string`` compare);</b></h4>
		<p>Writes the diff between two strings in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` write</p>
			<p>The new string</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` compare</p>
			<p>The previous string to use for diff</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteByteArray(``byte[]`` b, ``long`` count);</b></h4>
		<p>Writes a byte array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteByteArrayDiff(``byte[]`` write, ``byte[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two byte arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteShortArray(``short[]`` b, ``long`` count);</b></h4>
		<p>Writes a short array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteShortArrayDiff(``short[]`` write, ``short[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two short arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUShortArray(``ushort[]`` b, ``long`` count);</b></h4>
		<p>Writes a ushort array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUShortArrayDiff(``ushort[]`` write, ``ushort[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two ushort arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteCharArray(``Char[]`` b, ``long`` count);</b></h4>
		<p>Writes a char array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteCharArrayDiff(``Char[]`` write, ``Char[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two char arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteIntArray(``int[]`` b, ``long`` count);</b></h4>
		<p>Writes a int array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteIntArrayDiff(``int[]`` write, ``int[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two int arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUIntArray(``uint[]`` b, ``long`` count);</b></h4>
		<p>Writes a uint array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUIntArrayDiff(``uint[]`` write, ``uint[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two uint arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteLongArray(``long[]`` b, ``long`` count);</b></h4>
		<p>Writes a long array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteLongArrayDiff(``long[]`` write, ``long[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two long arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteULongArray(``ulong[]`` b, ``long`` count);</b></h4>
		<p>Writes a ulong array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteULongArrayDiff(``ulong[]`` write, ``ulong[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two ulong arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteFloatArray(``float[]`` b, ``long`` count);</b></h4>
		<p>Writes a float array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteFloatArrayDiff(``float[]`` write, ``float[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two float arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteDoubleArray(``double[]`` b, ``long`` count);</b></h4>
		<p>Writes a double array</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteDoubleArrayDiff(``double[]`` write, ``double[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two double arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteArrayPacked(``Array`` a, ``long`` count);</b></h4>
		<p>Writes an array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Array`` a</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteArrayPackedDiff(``Array`` write, ``Array`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Array`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Array`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteShortArrayPacked(``short[]`` b, ``long`` count);</b></h4>
		<p>Writes a short array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteShortArrayPackedDiff(``short[]`` write, ``short[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two short arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``short[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUShortArrayPacked(``ushort[]`` b, ``long`` count);</b></h4>
		<p>Writes a ushort array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUShortArrayPackedDiff(``ushort[]`` write, ``ushort[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two ushort arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteCharArrayPacked(``Char[]`` b, ``long`` count);</b></h4>
		<p>Writes a char array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteCharArrayPackedDiff(``Char[]`` write, ``Char[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two char arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Char[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteIntArrayPacked(``int[]`` b, ``long`` count);</b></h4>
		<p>Writes a int array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteIntArrayPackedDiff(``int[]`` write, ``int[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two int arrays</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUIntArrayPacked(``uint[]`` b, ``long`` count);</b></h4>
		<p>Writes a uint array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteUIntArrayPackedDiff(``uint[]`` write, ``uint[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two uing arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteLongArrayPacked(``long[]`` b, ``long`` count);</b></h4>
		<p>Writes a long array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteLongArrayPackedDiff(``long[]`` write, ``long[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two long arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteULongArrayPacked(``ulong[]`` b, ``long`` count);</b></h4>
		<p>Writes a ulong array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteULongArrayPackedDiff(``ulong[]`` write, ``ulong[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two ulong arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteFloatArrayPacked(``float[]`` b, ``long`` count);</b></h4>
		<p>Writes a float array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteFloatArrayPackedDiff(``float[]`` write, ``float[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two float arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteDoubleArrayPacked(``double[]`` b, ``long`` count);</b></h4>
		<p>Writes a double array in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` b</p>
			<p>The array to write</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteDoubleArrayPackedDiff(``double[]`` write, ``double[]`` compare, ``long`` count);</b></h4>
		<p>Writes the diff between two double arrays in a packed format</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` write</p>
			<p>The new array</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``double[]`` compare</p>
			<p>The previous array to use for diff</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` count</p>
			<p>The amount of elements to write</p>
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
