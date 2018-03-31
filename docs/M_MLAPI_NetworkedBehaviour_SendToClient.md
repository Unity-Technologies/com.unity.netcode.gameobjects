# NetworkedBehaviour.SendToClient Method 
 

Sends a buffer to a client with a given clientId from Server

**Namespace:**&nbsp;<a href="N_MLAPI">MLAPI</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
protected void SendToClient(
	int clientId,
	string messageType,
	string channelName,
	byte[] data
)
```

<br />

#### Parameters
&nbsp;<dl><dt>clientId</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The clientId to send the message to</dd><dt>messageType</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/s1wwdcbf" target="_blank">System.String</a><br />User defined messageType</dd><dt>channelName</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/s1wwdcbf" target="_blank">System.String</a><br />User defined channelName</dd><dt>data</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">System.Byte</a>[]<br />The binary data to send</dd></dl>

## See Also


#### Reference
<a href="T_MLAPI_NetworkedBehaviour">NetworkedBehaviour Class</a><br /><a href="N_MLAPI">MLAPI Namespace</a><br />