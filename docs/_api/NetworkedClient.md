---
title: NetworkedClient
name: NetworkedClient
permalink: /api/networked-client/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkedClient ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Connection</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A NetworkedClient</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` ClientId;</b></h4>
		<p>The Id of the NetworkedClient</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetworkedObject``](/api/networked-object/) PlayerObject;</b></h4>
		<p>The PlayerObject of the Client</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<NetworkedObject>`` OwnedObjects;</b></h4>
		<p>The NetworkedObject's owned by this Client</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte[]`` AesKey;</b></h4>
		<p>The encryption key used for this client</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkedClient``](/api/networked-client/)();</b></h4>
	</div>
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
