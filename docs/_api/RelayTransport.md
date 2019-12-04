---
title: RelayTransport
name: RelayTransport
permalink: /api/relay-transport/
---

<div style="line-height: 1;">
	<h2 markdown="1">RelayTransport ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Transports.UNET</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Enabled { get; set; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` RelayAddress { get; set; }</b></h4>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``ushort`` RelayPort { get; set; }</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` Connect(``int`` hostId, ``string`` serverAddress, ``int`` serverPort, ``int`` exceptionConnectionId, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` serverAddress</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` serverPort</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` exceptionConnectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` ConnectWithSimulator(``int`` hostId, ``string`` serverAddress, ``int`` serverPort, ``int`` exceptionConnectionId, ``Byte&`` error, ``ConnectionSimulatorConfig`` conf);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` serverAddress</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` serverPort</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` exceptionConnectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ConnectionSimulatorConfig`` conf</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` ConnectEndPoint(``int`` hostId, ``EndPoint`` endPoint, ``int`` exceptionConnectionId, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``EndPoint`` endPoint</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` exceptionConnectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddHost(``HostTopology`` topology, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddHost(``HostTopology`` topology, ``int`` port, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddHost(``HostTopology`` topology, ``int`` port, ``string`` ip, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` ip</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddHostWithSimulator(``HostTopology`` topology, ``int`` minTimeout, ``int`` maxTimeout, ``int`` port, ``string`` ip, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` minTimeout</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` maxTimeout</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` ip</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddHostWithSimulator(``HostTopology`` topology, ``int`` minTimeout, ``int`` maxTimeout, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` minTimeout</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` maxTimeout</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddHostWithSimulator(``HostTopology`` topology, ``int`` minTimeout, ``int`` maxTimeout, ``int`` port, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` minTimeout</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` maxTimeout</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddWebsocketHost(``HostTopology`` topology, ``int`` port, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``int`` AddWebsocketHost(``HostTopology`` topology, ``int`` port, ``string`` ip, ``bool`` createServer);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``HostTopology`` topology</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` port</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` ip</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` createServer</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``bool`` Disconnect(``int`` hostId, ``int`` connectionId, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` connectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``bool`` Send(``int`` hostId, ``int`` connectionId, ``int`` channelId, ``byte[]`` buffer, ``int`` size, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` connectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` channelId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` size</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``bool`` QueueMessageForSending(``int`` hostId, ``int`` connectionId, ``int`` channelId, ``byte[]`` buffer, ``int`` size, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` connectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` channelId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` size</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``bool`` SendQueuedMessages(``int`` hostId, ``int`` connectionId, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` connectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Byte&`` error</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``NetworkEventType`` ReceiveFromHost(``int`` hostId, ``Int32&`` connectionId, ``Int32&`` channelId, ``byte[]`` buffer, ``int`` bufferSize, ``Int32&`` receivedSize, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Int32&`` connectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Int32&`` channelId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
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
		<h4 markdown="1"><b>public static ``NetworkEventType`` Receive(``Int32&`` hostId, ``Int32&`` connectionId, ``Int32&`` channelId, ``byte[]`` buffer, ``int`` bufferSize, ``Int32&`` receivedSize, ``Byte&`` error);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Int32&`` hostId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Int32&`` connectionId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Int32&`` channelId</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` buffer</p>
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
