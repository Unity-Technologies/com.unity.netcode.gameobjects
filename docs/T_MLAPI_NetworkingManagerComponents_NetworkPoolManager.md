# NetworkPoolManager Class
 

Main class for managing network pools


## Inheritance Hierarchy
<a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">System.Object</a><br />&nbsp;&nbsp;MLAPI.NetworkingManagerComponents.NetworkPoolManager<br />
**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static class NetworkPoolManager
```

<br />
The NetworkPoolManager type exposes the following members.


## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_NetworkPoolManager_CreatePool">CreatePool</a></td><td>
Creates a networked object pool. Can only be called from the server</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_NetworkPoolManager_DestroyPool">DestroyPool</a></td><td>
This destroys an object pool and all of it's objects. Can only be called from the server</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_NetworkPoolManager_DestroyPoolObject">DestroyPoolObject</a></td><td>
Destroys a NetworkedObject if it's part of a pool. Use this instead of the MonoBehaviour Destroy method. Can only be called from Server.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_MLAPI_NetworkingManagerComponents_NetworkPoolManager_SpawnPoolObject">SpawnPoolObject</a></td><td>
Spawns a object from the pool at a given position and rotation. Can only be called from server.</td></tr></table>&nbsp;
<a href="#networkpoolmanager-class">Back to Top</a>

## See Also


#### Reference
<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />