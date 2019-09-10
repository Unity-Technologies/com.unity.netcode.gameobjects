---
title: CustomSerializationDelegate&lt;T&gt;
name: CustomSerializationDelegate<T>
permalink: /api/custom-serialization-delegate%3C-t%3E/
---

<div style="line-height: 1;">
	<h2 markdown="1">CustomSerializationDelegate&lt;T&gt; ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Serialization</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>The delegate used when registering custom serialization for a type.</p>

<div>
	<h3 markdown="1">Inherited Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``MethodInfo`` Method { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Delegate``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` Target { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Delegate``</h5>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``CustomSerializationDelegate<T>``](/api/custom-serialization-delegate%3C-t%3E/)(``object`` object, ``IntPtr`` method);</b></h4>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` object</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IntPtr`` method</p>
		</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Invoke(``Stream`` stream, ``T`` instance);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T`` instance</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``IAsyncResult`` BeginInvoke(``Stream`` stream, ``T`` instance, ``AsyncCallback`` callback, ``object`` object);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Stream`` stream</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T`` instance</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``AsyncCallback`` callback</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` object</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` EndInvoke(``IAsyncResult`` result);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IAsyncResult`` result</p>
		</div>
	</div>
	<br>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetObjectData(``SerializationInfo`` info, ``StreamingContext`` context);</b></h4>
		<h5 markdown="1">Inherited from: ``MulticastDelegate``</h5>
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
		<h4 markdown="1"><b>public ``bool`` Equals(``object`` obj);</b></h4>
		<h5 markdown="1">Inherited from: ``MulticastDelegate``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` obj</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetHashCode();</b></h4>
		<h5 markdown="1">Inherited from: ``MulticastDelegate``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Delegate[]`` GetInvocationList();</b></h4>
		<h5 markdown="1">Inherited from: ``MulticastDelegate``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` DynamicInvoke(``object[]`` args);</b></h4>
		<h5 markdown="1">Inherited from: ``Delegate``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object[]`` args</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``object`` Clone();</b></h4>
		<h5 markdown="1">Inherited from: ``Delegate``</h5>
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
