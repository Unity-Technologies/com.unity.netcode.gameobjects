---
title: ServerRPCAttribute
name: ServerRPCAttribute
permalink: /api/server-rpcattribute/
---

<div style="line-height: 1;">
	<h2 markdown="1">ServerRPCAttribute ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Messaging</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Attribute used on methods to me marked as ServerRPC
            ServerRPC methods can be requested from a client and will execute on the server
            Remember that a host is a server and a client</p>

<div>
	<h3 markdown="1">Inherited Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` TypeId { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Attribute``</h5>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` RequireOwnership;</b></h4>
		<p>Whether or not the ServerRPC should only be run if executed by the owner of the object</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``ServerRPCAttribute``](/api/server-rpcattribute/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Equals(``object`` obj);</b></h4>
		<h5 markdown="1">Inherited from: ``Attribute``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` obj</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetHashCode();</b></h4>
		<h5 markdown="1">Inherited from: ``Attribute``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Match(``object`` obj);</b></h4>
		<h5 markdown="1">Inherited from: ``Attribute``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` obj</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsDefaultAttribute();</b></h4>
		<h5 markdown="1">Inherited from: ``Attribute``</h5>
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
