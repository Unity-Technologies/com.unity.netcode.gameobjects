---
title: SocketTask
name: SocketTask
permalink: /api/socket-task/
---

<div style="line-height: 1;">
	<h2 markdown="1">SocketTask ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Transports.Tasks</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A single socket task.</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsDone { get; set; }</b></h4>
		<p>Gets or sets a value indicating whether this  is done.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Success { get; set; }</b></h4>
		<p>Gets or sets a value indicating whether this  is success.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Exception`` TransportException { get; set; }</b></h4>
		<p>Gets or sets the transport exception.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``SocketError`` SocketError { get; set; }</b></h4>
		<p>Gets or sets the socket error.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` TransportCode { get; set; }</b></h4>
		<p>Gets or sets the transport code.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` Message { get; set; }</b></h4>
		<p>Gets or sets the message.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` State { get; set; }</b></h4>
		<p>Gets or sets the state.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``SocketTask``](/api/socket-task/) Done { get; }</b></h4>
		<p>Gets a done task.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``SocketTask``](/api/socket-task/) Fault { get; }</b></h4>
		<p>Gets a faulty task.</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``SocketTask``](/api/socket-task/) Working { get; }</b></h4>
		<p>Gets a working task.</p>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``SocketTask``](/api/socket-task/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``SocketTasks``](/api/socket-tasks/) AsTasks();</b></h4>
		<p>Converts to a SocketTasks.</p>
		<h5 markdown="1"><b>Returns [``SocketTasks``](/api/socket-tasks/)</b></h5>
		<div>
			<p>The tasks.</p>
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
