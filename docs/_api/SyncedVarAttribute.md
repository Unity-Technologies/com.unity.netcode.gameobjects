---
title: SyncedVarAttribute
name: SyncedVarAttribute
permalink: /api/synced-var-attribute/
---

<div style="line-height: 1;">
	<h2 markdown="1">SyncedVarAttribute ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.NetworkedVar</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>SyncedVar attribute. Use this to automatically syncronize fields from the server to clients.</p>

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
		<h4 markdown="1"><b>public ``string`` Channel;</b></h4>
		<p>The channel to send changes on.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` SendTickrate;</b></h4>
		<p>The maximum times per second this var will be synced.
            A value of 0 will cause the variable to sync as soon as possible after being changed.
            A value of less than 0 will cause the variable to sync only at once at spawn and not update again.</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``SyncedVarAttribute``](/api/synced-var-attribute/)();</b></h4>
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
