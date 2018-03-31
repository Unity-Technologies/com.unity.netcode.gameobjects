# MessageChunker.HasMissingParts Method 
 

Checks if a list of chunks has missing parts

**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static bool HasMissingParts(
	ref List<byte[]> chunks,
	uint expectedChunksCount
)
```

<br />

#### Parameters
&nbsp;<dl><dt>chunks</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/6sh2ey19" target="_blank">System.Collections.Generic.List</a>(<a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">Byte</a>[])<br />The list of chunks</dd><dt>expectedChunksCount</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/ctys3981" target="_blank">System.UInt32</a><br />The expected amount of chunks</dd></dl>

#### Return Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/a28wyd50" target="_blank">Boolean</a><br />If list of chunks has missing parts

## See Also


#### Reference
<a href="T_MLAPI_NetworkingManagerComponents_MessageChunker">MessageChunker Class</a><br /><a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />