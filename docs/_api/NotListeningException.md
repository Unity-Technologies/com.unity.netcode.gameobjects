---
title: NotListeningException
name: NotListeningException
permalink: /api/not-listening-exception/
---

<div style="line-height: 1;">
	<h2 markdown="1">NotListeningException ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Exceptions</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Exception thrown when the operation require NetworkingManager to be listening.</p>

<div>
	<h3 markdown="1">Inherited Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` Message { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``IDictionary`` Data { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Exception`` InnerException { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``MethodBase`` TargetSite { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` StackTrace { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` HelpLink { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` Source { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` HResult { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NotListeningException``](/api/not-listening-exception/)();</b></h4>
		<p>Constructs a NotListeningException</p>
	</div>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NotListeningException``](/api/not-listening-exception/)(``string`` message);</b></h4>
		<p>Constructs a NotListeningException with a message</p>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` message</p>
			<p>The exception message</p>
		</div>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``NotListeningException``](/api/not-listening-exception/)(``string`` message, ``Exception`` inner);</b></h4>
		<p>Constructs a NotListeningException with a message and a inner exception</p>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` message</p>
			<p>The exception message</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Exception`` inner</p>
			<p>The inner exception</p>
		</div>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Exception`` GetBaseException();</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetObjectData(``SerializationInfo`` info, ``StreamingContext`` context);</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SerializationInfo`` info</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``StreamingContext`` context</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Type`` GetType();</b></h4>
		<h5 markdown="1">Inherited from: ``Exception``</h5>
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
