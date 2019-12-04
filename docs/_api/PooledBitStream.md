---
title: PooledBitStream
name: PooledBitStream
permalink: /api/pooled-bit-stream/
---

<div style="line-height: 1;">
	<h2 markdown="1">PooledBitStream ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization.Pooled</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Disposable BitStream that returns the Stream to the BitStreamPool when disposed</p>

<div>
	<h3 markdown="1">Inherited Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Resizable { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Whether or not the stream will grow the buffer to accomodate more data.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` GrowthFactor { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Factor by which buffer should grow when necessary.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CanRead { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Whether or not stream supports reading. (Always true)</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` HasDataToRead { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Whether or not or there is any data to be read from the stream.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CanSeek { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Whether or not seeking is supported by this stream. (Always true)</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CanWrite { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Whether or not this stream can accept new data. NOTE: this will return true even if only fewer than 8 bits can be written!</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` Capacity { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Current buffer size. The buffer will not be resized (if possible) until Position is equal to Capacity and an attempt to write data is made.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` Length { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>The current length of data considered to be "written" to the buffer.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` Position { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>The index that will be written to when any call to write data is made to this stream.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` BitPosition { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Bit offset into the buffer that new data will be written to.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` BitLength { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Length of data (in bits) that is considered to be written to the stream.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` BitAligned { get; }</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Whether or not the current BitPosition is evenly divisible by 8. I.e. whether or not the BitPosition is at a byte boundary.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CanTimeout { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ReadTimeout { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` WriteTimeout { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Dispose();</b></h4>
		<p>Returns the PooledBitStream into the static BitStreamPool</p>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static [``PooledBitStream``](/api/pooled-bit-stream/) Get();</b></h4>
		<p>Gets a PooledBitStream from the static BitStreamPool</p>
		<h5 markdown="1"><b>Returns [``PooledBitStream``](/api/pooled-bit-stream/)</b></h5>
		<div>
			<p>PooledBitStream</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Flush();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Flush stream. This does nothing since data is written directly to a byte buffer.</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` ReadByte();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Read a byte from the buffer. This takes into account possible byte misalignment.</p>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>A byte from the buffer or, if a byte can't be read, -1.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` PeekByte();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Peeks a byte without advancing the position</p>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>The peeked byte</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` ReadBit();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Read a single bit from the stream.</p>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>A bit in bool format. (True represents 1, False represents 0)</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` Read(``byte[]`` buffer, ``int`` offset, ``int`` count);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Read a subset of the stream buffer and write the contents to the supplied buffer.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
			<p>Buffer to copy data to.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
			<p>Offset into the buffer to write data to.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
			<p>How many bytes to attempt to read.</p>
		</div>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>Amount of bytes read.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``long`` Seek(``long`` offset, ``SeekOrigin`` origin);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Set position in stream to read from/write to.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` offset</p>
			<p>Offset from position origin.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SeekOrigin`` origin</p>
			<p>How to calculate offset.</p>
		</div>
		<h5 markdown="1"><b>Returns ``long``</b></h5>
		<div>
			<p>The new position in the buffer that data will be written to.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SetLength(``long`` value);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Set length of data considered to be "written" to the stream.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``long`` value</p>
			<p>New length of the written data.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Write(``byte[]`` buffer, ``int`` offset, ``int`` count);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Write data from the given buffer to the internal stream buffer.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
			<p>Buffer to write from.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
			<p>Offset in given buffer to start reading from.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
			<p>Amount of bytes to read copy from given buffer to stream buffer.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteByte(``byte`` value);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Write byte value to the internal stream buffer.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` value</p>
			<p>The byte value to write.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Write(``byte[]`` buffer);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Write data from the given buffer to the internal stream buffer.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
			<p>Buffer to write from.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` WriteBit(``bool`` bit);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Write a single bit to the stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` bit</p>
			<p>Value of the bit. True represents 1, False represents 0</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CopyFrom(``Stream`` s, ``int`` count);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Copy data from another stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` s</p>
			<p>Stream to copy from</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
			<p>How many bytes to read. Set to value less than one to read until ReadByte returns -1</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CopyTo(``Stream`` stream, ``int`` count);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Copies internal buffer to stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The stream to copy to</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
			<p>The maximum amount of bytes to copy. Set to value less than one to copy the full length</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CopyUnreadFrom(``Stream`` s, ``int`` count);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Copies urnead bytes from the source stream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` s</p>
			<p>The source stream to copy from</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
			<p>The max amount of bytes to copy</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CopyFrom([``BitStream``](/api/bit-stream/) stream, ``int`` dataCount, ``bool`` copyBits);</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Copys the bits from the provided BitStream</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``BitStream``](/api/bit-stream/) stream</p>
			<p>The stream to copy from</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` dataCount</p>
			<p>The amount of data evel</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` copyBits</p>
			<p>Whether or not to copy at the bit level rather than the byte level</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` GetBuffer();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Get the internal buffer being written to by this stream.</p>
		<h5 markdown="1"><b>Returns ``byte[]``</b></h5>
		<div>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` ToArray();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Creates a copy of the internal buffer. This only contains the used bytes</p>
		<h5 markdown="1"><b>Returns ``byte[]``</b></h5>
		<div>
			<p>A copy of used bytes in the internal buffer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` PadStream();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Writes zeros to fill the last byte</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SkipPadBits();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Reads zeros until the the stream is byte aligned</p>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: [``BitStream``](/api/bit-stream/)</h5>
		<p>Returns hex encoded version of the buffer</p>
		<h5 markdown="1"><b>Returns ``string``</b></h5>
		<div>
			<p>Hex encoded version of the buffer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` CopyToAsync(``Stream`` destination);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` destination</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` CopyToAsync(``Stream`` destination, ``int`` bufferSize);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` destination</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bufferSize</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` CopyToAsync(``Stream`` destination, ``CancellationToken`` cancellationToken);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` destination</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CancellationToken`` cancellationToken</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` CopyToAsync(``Stream`` destination, ``int`` bufferSize, ``CancellationToken`` cancellationToken);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` destination</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bufferSize</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CancellationToken`` cancellationToken</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CopyTo(``Stream`` destination);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` destination</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CopyTo(``Stream`` destination, ``int`` bufferSize);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` destination</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bufferSize</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Close();</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Dispose();</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` FlushAsync();</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` FlushAsync(``CancellationToken`` cancellationToken);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CancellationToken`` cancellationToken</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``IAsyncResult`` BeginRead(``byte[]`` buffer, ``int`` offset, ``int`` count, ``AsyncCallback`` callback, ``object`` state);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``AsyncCallback`` callback</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` state</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` EndRead(``IAsyncResult`` asyncResult);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IAsyncResult`` asyncResult</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task<int>`` ReadAsync(``byte[]`` buffer, ``int`` offset, ``int`` count);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task<int>`` ReadAsync(``byte[]`` buffer, ``int`` offset, ``int`` count, ``CancellationToken`` cancellationToken);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CancellationToken`` cancellationToken</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ValueTask<int>`` ReadAsync(``Memory<byte>`` buffer, ``CancellationToken`` cancellationToken);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Memory<byte>`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CancellationToken`` cancellationToken</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``IAsyncResult`` BeginWrite(``byte[]`` buffer, ``int`` offset, ``int`` count, ``AsyncCallback`` callback, ``object`` state);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``AsyncCallback`` callback</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` state</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` EndWrite(``IAsyncResult`` asyncResult);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IAsyncResult`` asyncResult</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` WriteAsync(``byte[]`` buffer, ``int`` offset, ``int`` count);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Task`` WriteAsync(``byte[]`` buffer, ``int`` offset, ``int`` count, ``CancellationToken`` cancellationToken);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` offset</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` count</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CancellationToken`` cancellationToken</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ValueTask`` WriteAsync(``ReadOnlyMemory<byte>`` buffer, ``CancellationToken`` cancellationToken);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ReadOnlyMemory<byte>`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CancellationToken`` cancellationToken</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` Read(``Span<byte>`` buffer);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Span<byte>`` buffer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Write(``ReadOnlySpan<byte>`` buffer);</b></h4>
		<h5 markdown="1">Inherited from: ``Stream``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ReadOnlySpan<byte>`` buffer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ObjRef`` CreateObjRef(``Type`` requestedType);</b></h4>
		<h5 markdown="1">Inherited from: ``MarshalByRefObject``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` requestedType</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` GetLifetimeService();</b></h4>
		<h5 markdown="1">Inherited from: ``MarshalByRefObject``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` InitializeLifetimeService();</b></h4>
		<h5 markdown="1">Inherited from: ``MarshalByRefObject``</h5>
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
</div>
<br>
