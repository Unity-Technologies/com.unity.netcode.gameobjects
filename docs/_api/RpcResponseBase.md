---
title: RpcResponseBase
name: RpcResponseBase
permalink: /api/rpc-response-base/
---

<div style="line-height: 1;">
	<h2 markdown="1">RpcResponseBase ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Messaging</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Abstract base class for RpcResponse</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` Id { get; set; }</b></h4>
		<p>Unique ID for the Rpc Request and Response pair</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsDone { get; set; }</b></h4>
		<p>Whether or not the operation is done. This does not mean it was successful. Check IsSuccessful for that
            This will be true both when the operation was successful and when a timeout occured</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsSuccessful { get; set; }</b></h4>
		<p>Whether or not a valid result was received</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ulong`` ClientId { get; set; }</b></h4>
		<p>The clientId which the Request/Response was done wit</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` Timeout { get; set; }</b></h4>
		<p>The amount of time to wait for the operation to complete</p>
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
