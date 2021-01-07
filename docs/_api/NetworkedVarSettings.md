---
title: NetworkedVarSettings
name: NetworkedVarSettings
permalink: /api/networked-var-settings/
---

<div style="line-height: 1;">
	<h2 markdown="1">NetworkedVarSettings ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.NetworkedVar</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The settings class used by the build in NetworkVar implementations</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetworkedVarPermission``](/api/networked-var-permission/) WritePermission;</b></h4>
		<p>Defines the write permissions for this var</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetworkedVarPermission``](/api/networked-var-permission/) ReadPermission;</b></h4>
		<p>Defines the read permissions for this var</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetworkedVarPermissionsDelegate``](/api/networked-var-permissions-delegate/) WritePermissionCallback;</b></h4>
		<p>The delegate used to evaluate write permission when the "Custom" mode is used</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetworkedVarPermissionsDelegate``](/api/networked-var-permissions-delegate/) ReadPermissionCallback;</b></h4>
		<p>The delegate used to evaluate read permission when the "Custom" mode is used</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` SendTickrate;</b></h4>
		<p>The maximum times per second this var will be synced.
            A value of 0 will cause the variable to sync as soon as possible after being changed.
            A value of less than 0 will cause the variable to sync only at once at spawn and not update again.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` SendChannel;</b></h4>
		<p>The name of the channel to use for this variable.
            Variables with different channels will be split into different packets</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NetworkedVarSettings``](/api/networked-var-settings/)();</b></h4>
		<p>Constructs a new NetworkedVarSettings instance</p>
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
