---
title: ConnectionIdSpreadMethod
name: ConnectionIdSpreadMethod
permalink: /api/connection-id-spread-method/
---

<div style="line-height: 1;">
	<h2 markdown="1">ConnectionIdSpreadMethod ``enum``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Transports.Multiplex</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The method to use to distribute the transport connectionIds in a fixed size 64 bit integer.</p>
<div>
	<h3 markdown="1">Enum Values</h3>
	<div>
		<h4 markdown="1"><b>``MakeRoomLastBits``</b></h4>
		<p>Drops the first few bits (left side) by shifting the transport clientId to the left and inserting the transportId in the first bits.
            Ensure that ALL transports dont use the last bits in their produced clientId.
            For incremental clientIds, this is the most space efficient assuming that every transport get used an equal amount.</p>
	</div>
	<div>
		<h4 markdown="1"><b>``ReplaceFirstBits``</b></h4>
		<p>Drops the first few bits (left side) and replaces them with the transport index.
            Ensure that ALL transports dont use the first few bits in the produced clientId.</p>
	</div>
	<div>
		<h4 markdown="1"><b>``ReplaceLastBits``</b></h4>
		<p>Drops the last few bits (right side) and replaces them with the transport index.
            Ensure that ALL transports dont use the last bits in their produced clientId.
            This option is for advanced users and will not work with the official MLAPI transports as they use the last bits.</p>
	</div>
	<div>
		<h4 markdown="1"><b>``MakeRoomFirstBits``</b></h4>
		<p>Drops the last few bits (right side) by shifting the transport clientId to the right and inserting the transportId in the first bits.
            Ensure that ALL transports dont use the first bits in their produced clientId.</p>
	</div>
	<div>
		<h4 markdown="1"><b>``Spread``</b></h4>
		<p>Spreads the clientIds evenly among the transports.</p>
	</div>
</div>
