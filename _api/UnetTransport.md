---
title: UnetTransport
permalink: /api/unet-transport/
---

<div style="line-height: 1;">
	<h2 markdown="1">UnetTransport ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Transports.UNET</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``ChannelType``](/MLAPI/api/channel-type/) InternalChannel { get; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``uint`` ServerClientId { get; }</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` serverConnectionId;</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` serverHostId;</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``List<TransportHost>`` ServerTransports;</b></h4>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``UnetTransport``](/MLAPI/api/unet-transport/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` Connect(``string`` address, ``int`` port, ``object`` settings, ``bool`` websocket, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` address</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` settings</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` websocket</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` DisconnectClient(``uint`` clientId);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` clientId</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` DisconnectFromServer();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetCurrentRTT(``uint`` clientId, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` clientId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetNetworkTimestamp();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetRemoteDelayTimeMS(``uint`` clientId, ``int`` remoteTimestamp, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` clientId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` remoteTimestamp</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``NetEventType``](/MLAPI/api/net-event-type/) PollReceive(``UInt32&`` clientId, ``Int32&`` channelId, ``Byte[]&`` data, ``int`` bufferSize, ``Int32&`` receivedSize, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``UInt32&`` clientId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Int32&`` channelId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte[]&`` data</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` bufferSize</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Int32&`` receivedSize</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` QueueMessageForSending(``uint`` clientId, ``byte[]`` dataBuffer, ``int`` dataSize, ``int`` channelId, ``bool`` skipqueue, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` clientId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` dataBuffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` dataSize</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` channelId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` skipqueue</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Shutdown();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendQueue(``uint`` clientId, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``uint`` clientId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` RegisterServerListenSocket(``object`` settings);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` settings</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` AddChannel([``ChannelType``](/MLAPI/api/channel-type/) type, ``object`` settings);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">[``ChannelType``](/MLAPI/api/channel-type/) type</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` settings</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` GetSettings();</b></h4>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Connect(``string`` address, ``int`` port, ``object`` settings, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` address</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` settings</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
	<br>
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
</div>
<br>
