---
title: SerializationManager
name: SerializationManager
permalink: /api/serialization-manager/
---

<div style="line-height: 1;">
	<h2 markdown="1">SerializationManager ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Helper class to manage the MLAPI serialization.</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` RegisterSerializationHandlers(``CustomSerializationDelegate<T>`` onSerialize, ``CustomDeserializationDelegate<T>`` onDeserialize);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CustomSerializationDelegate<T>`` onSerialize</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``CustomDeserializationDelegate<T>`` onDeserialize</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``bool`` RemoveSerializationHandlers();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``bool`` IsTypeSupported(``Type`` type);</b></h4>
		<p>Returns if a type is supported for serialization</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
			<p>The type to check</p>
		</div>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>Whether or not the type is supported</p>
		</div>
	</div>
	<br>
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
