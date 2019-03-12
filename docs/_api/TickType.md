---
title: TickType
name: TickType
permalink: /api/tick-type/
---

<div style="line-height: 1;">
	<h2 markdown="1">TickType ``enum``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Profiling</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The type of Tick</p>
<div>
	<h3 markdown="1">Enum Values</h3>
	<div>
		<h4 markdown="1"><b>``Event``</b></h4>
		<p>Event tick. During EventTick SyncedVars are flushed etc</p>
	</div>
	<div>
		<h4 markdown="1"><b>``Receive``</b></h4>
		<p>Receive tick. During ReceiveTick data is received from the transport</p>
	</div>
	<div>
		<h4 markdown="1"><b>``Send``</b></h4>
		<p>Send tick. During Send data is sent from Transport queue</p>
	</div>
</div>
