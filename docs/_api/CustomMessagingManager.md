---
title: CustomMessagingManager
name: CustomMessagingManager
permalink: /api/custom-messaging-manager/
---

<div style="line-height: 1;">
	<h2 markdown="1">CustomMessagingManager ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Messaging</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The manager class to manage custom messages, note that this is different from the NetworkingManager custom messages.
            These are named and are much easier to use.</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` SendUnnamedMessage(``List<ulong>`` clientIds, [``BitStream``](/api/bit-stream/) stream, ``string`` channel, [``SecuritySendFlags``](/api/security-send-flags/) security);</b></h4>
		<p>Sends unnamed message to a list of clients</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<ulong>`` clientIds</p>
			<p>The clients to send to, sends to everyone if null</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``BitStream``](/api/bit-stream/) stream</p>
			<p>The message stream containing the data</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` channel</p>
			<p>The channel to send the data on</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``SecuritySendFlags``](/api/security-send-flags/) security</p>
			<p>The security settings to apply to the message</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` SendUnnamedMessage(``ulong`` clientId, [``BitStream``](/api/bit-stream/) stream, ``string`` channel, [``SecuritySendFlags``](/api/security-send-flags/) security);</b></h4>
		<p>Sends a unnamed message to a specific client</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The client to send the message to</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``BitStream``](/api/bit-stream/) stream</p>
			<p>The message stream containing the data</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` channel</p>
			<p>The channel tos end the data on</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``SecuritySendFlags``](/api/security-send-flags/) security</p>
			<p>The security settings to apply to the message</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` RegisterNamedMessageHandler(``string`` name, [``HandleNamedMessageDelegate``](/api/handle-named-message-delegate/) callback);</b></h4>
		<p>Registers a named message handler delegate.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` name</p>
			<p>Name of the message.</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``HandleNamedMessageDelegate``](/api/handle-named-message-delegate/) callback</p>
			<p>The callback to run when a named message is received.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` UnregisterNamedMessageHandler(``string`` name);</b></h4>
		<p>Unregisters a named message handler.</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` name</p>
			<p>The name of the message.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` SendNamedMessage(``string`` name, ``ulong`` clientId, ``Stream`` stream, ``string`` channel, [``SecuritySendFlags``](/api/security-send-flags/) security);</b></h4>
		<p>Sends a named message</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` name</p>
			<p>The message name to send</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The client to send the message to</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The message stream containing the data</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` channel</p>
			<p>The channel tos end the data on</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``SecuritySendFlags``](/api/security-send-flags/) security</p>
			<p>The security settings to apply to the message</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``void`` SendNamedMessage(``string`` name, ``List<ulong>`` clientIds, ``Stream`` stream, ``string`` channel, [``SecuritySendFlags``](/api/security-send-flags/) security);</b></h4>
		<p>Sends the named message</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` name</p>
			<p>The message name to send</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<ulong>`` clientIds</p>
			<p>The clients to send to, sends to everyone if null</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
			<p>The message stream containing the data</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` channel</p>
			<p>The channel to send the data on</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``SecuritySendFlags``](/api/security-send-flags/) security</p>
			<p>The security settings to apply to the message</p>
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
