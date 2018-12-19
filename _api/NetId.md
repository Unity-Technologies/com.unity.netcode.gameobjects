---
title: NetId
permalink: /api/net-id/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetId ``struct``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Transports.UNET</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Represents a ClientId structure</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` HostId;</b></h4>
		<p>The hostId this client is on</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort`` ConnectionId;</b></h4>
		<p>The connectionId this client is assigned</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``byte`` Meta;</b></h4>
		<p>Meta data about hte client</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetId``](/MLAPI/api/net-id/)(``byte`` hostId, ``ushort`` connectionId, ``bool`` isServer);</b></h4>
		<p>Initializes a new instance of the netId struct from transport values</p>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte`` hostId</p>
			<p>Host identifier.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ushort`` connectionId</p>
			<p>Connection identifier.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` isServer</p>
			<p>If set to true is isServer.</p>
		</div>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetId``](/MLAPI/api/net-id/)(``uint`` clientId);</b></h4>
		<p>Initializes a new instance of the netId struct from a clientId</p>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` clientId</p>
			<p>Client identifier.</p>
		</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsServer();</b></h4>
		<p>Returns wheter or not the clientId represents a -1</p>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>true, if server, false otherwise.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` GetClientId();</b></h4>
		<p>Gets the clientId.</p>
		<h5 markdown="1"><b>Returns ``uint``</b></h5>
		<div>
			<p>The client identifier.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Equals(``object`` obj);</b></h4>
		<p>Checks if two NetId's are equal</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` obj</p>
			<p>NetId to compare to</p>
		</div>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>Wheter or not the two NetIds are equal</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetHashCode();</b></h4>
		<p>Returns a hash code for the instance</p>
		<h5 markdown="1"><b>Returns ``int``</b></h5>
		<div>
			<p>Returns a hash code for the instance</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: ``ValueType``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Type`` GetType();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
</div>
<br>
