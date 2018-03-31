# MessageChunker.GetChunkedMessage Method 
 

Chunks a large byte array to smaller chunks

**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static List<byte[]> GetChunkedMessage(
	ref byte[] message,
	int chunkSize
)
```

<br />

#### Parameters
&nbsp;<dl><dt>message</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">System.Byte</a>[]<br />The large byte array</dd><dt>chunkSize</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The amount of bytes of non header data to use for each chunk</dd></dl>

#### Return Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/6sh2ey19" target="_blank">List</a>(<a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">Byte</a>[])<br />List of chunks

## See Also


#### Reference
<a href="T_MLAPI_NetworkingManagerComponents_MessageChunker">MessageChunker Class</a><br /><a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />