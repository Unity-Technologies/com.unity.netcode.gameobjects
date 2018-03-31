# NetworkPoolManager.CreatePool Method 
 

Creates a networked object pool. Can only be called from the server

**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static void CreatePool(
	string poolName,
	int spawnablePrefabIndex,
	uint size = 16
)
```

<br />

#### Parameters
&nbsp;<dl><dt>poolName</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/s1wwdcbf" target="_blank">System.String</a><br />Name of the pool</dd><dt>spawnablePrefabIndex</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The index of the prefab to use in the spawnablePrefabs array</dd><dt>size (Optional)</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/ctys3981" target="_blank">System.UInt32</a><br />The amount of objects in the pool</dd></dl>

## See Also


#### Reference
<a href="T_MLAPI_NetworkingManagerComponents_NetworkPoolManager">NetworkPoolManager Class</a><br /><a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />