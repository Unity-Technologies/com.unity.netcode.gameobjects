---
title: RpcMethod&lt;T1,T2,T3,T4,T5,T6,T7&gt;
name: RpcMethod<T1,T2,T3,T4,T5,T6,T7>
permalink: /api/rpc-method%3C-t1,-t2,-t3,-t4,-t5,-t6,-t7%3E/
---

<div style="line-height: 1;">
	<h2 markdown="1">RpcMethod&lt;T1,T2,T3,T4,T5,T6,T7&gt; ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
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
		<h4 markdown="1"><b>public [``RpcMethod<T1,T2,T3,T4,T5,T6,T7>``](/api/rpc-method%3C-t1,-t2,-t3,-t4,-t5,-t6,-t7%3E/)(``object`` object, ``IntPtr`` method);</b></h4>
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
		<h4 markdown="1"><b>public ``void`` Invoke(``T1`` t1, ``T2`` t2, ``T3`` t3, ``T4`` t4, ``T5`` t5, ``T6`` t6, ``T7`` t7);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T1`` t1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T2`` t2</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T3`` t3</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T4`` t4</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T5`` t5</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T6`` t6</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T7`` t7</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``IAsyncResult`` BeginInvoke(``T1`` t1, ``T2`` t2, ``T3`` t3, ``T4`` t4, ``T5`` t5, ``T6`` t6, ``T7`` t7, ``AsyncCallback`` callback, ``object`` object);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T1`` t1</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T2`` t2</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T3`` t3</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T4`` t4</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T5`` t5</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T6`` t6</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T7`` t7</p>
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
