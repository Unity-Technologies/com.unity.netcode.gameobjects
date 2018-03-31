# MessageChunker.GetMessageOrdered Method 
 

Converts a list of chunks back into the original buffer, this requires the list to be in correct order and properly verified

**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static byte[] GetMessageOrdered(
	ref List<byte[]> chunks,
	int chunkSize = -1
)
```

<br />

#### Parameters
&nbsp;<dl><dt>chunks</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/6sh2ey19" target="_blank">System.Collections.Generic.List</a>(<a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">Byte</a>[])<br />The list of chunks</dd><dt>chunkSize (Optional)</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The size of each chunk. Optional</dd></dl>

#### Return Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">Byte</a>[]<br />\[Missing <returns> documentation for "M:MLAPI.NetworkingManagerComponents.MessageChunker.GetMessageOrdered(System.Collections.Generic.List{System.Byte[]}@,System.Int32)"\]

## See Also


#### Reference
<a href="T_MLAPI_NetworkingManagerComponents_MessageChunker">MessageChunker Class</a><br /><a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />