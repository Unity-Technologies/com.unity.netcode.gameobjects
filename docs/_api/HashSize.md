---
title: HashSize
name: HashSize
permalink: /api/hash-size/
---

<div style="line-height: 1;">
	<h2 markdown="1">HashSize ``enum``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Configuration</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Represents the length of a var int encoded hash
            Note that the HashSize does not say anything about the actual final output due to the var int encoding
            It just says how many bytes the maximum will be</p>
<div>
	<h3 markdown="1">Enum Values</h3>
	<div>
		<h4 markdown="1"><b>``VarIntTwoBytes``</b></h4>
		<p>Two byte hash</p>
	</div>
	<div>
		<h4 markdown="1"><b>``VarIntFourBytes``</b></h4>
		<p>Four byte hash</p>
	</div>
	<div>
		<h4 markdown="1"><b>``VarIntEightBytes``</b></h4>
		<p>Eight byte hash</p>
	</div>
</div>
