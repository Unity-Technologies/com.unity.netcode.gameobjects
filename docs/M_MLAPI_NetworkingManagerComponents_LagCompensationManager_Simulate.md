# LagCompensationManager.Simulate Method (Int32, Action)
 

Turns time back a given amount of seconds, invokes an action and turns it back. The time is based on the estimated RTT of a clientId

**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static void Simulate(
	int clientId,
	Action action
)
```

<br />

#### Parameters
&nbsp;<dl><dt>clientId</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The clientId's RTT to use</dd><dt>action</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/bb534741" target="_blank">System.Action</a><br />The action to invoke when time is turned back</dd></dl>

## See Also


#### Reference
<a href="T_MLAPI_NetworkingManagerComponents_LagCompensationManager">LagCompensationManager Class</a><br /><a href="Overload_MLAPI_NetworkingManagerComponents_LagCompensationManager_Simulate">Simulate Overload</a><br /><a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />