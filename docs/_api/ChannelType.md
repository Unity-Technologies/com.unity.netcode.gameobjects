---
title: ChannelType
name: ChannelType
permalink: /api/channel-type/
---

<div style="line-height: 1;">
	<h2 markdown="1">ChannelType ``enum``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Transports</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Delivery methods</p>
<div>
	<h3 markdown="1">Enum Values</h3>
	<div>
		<h4 markdown="1"><b>``Unreliable``</b></h4>
		<p>Unreliable message</p>
	</div>
	<div>
		<h4 markdown="1"><b>``UnreliableSequenced``</b></h4>
		<p>Unreliable with sequencing</p>
	</div>
	<div>
		<h4 markdown="1"><b>``Reliable``</b></h4>
		<p>Reliable message</p>
	</div>
	<div>
		<h4 markdown="1"><b>``ReliableSequenced``</b></h4>
		<p>Reliable message where messages are guaranteed to be in the right order</p>
	</div>
	<div>
		<h4 markdown="1"><b>``ReliableFragmentedSequenced``</b></h4>
		<p>A reliable message with guaranteed order with fragmentation support</p>
	</div>
</div>
