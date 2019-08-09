---
title: FixedQueue&lt;T&gt;
name: FixedQueue<T>
permalink: /api/fixed-queue%3C-t%3E/
---

<div style="line-height: 1;">
	<h2 markdown="1">FixedQueue&lt;T&gt; ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Collections</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Queue with a fixed size</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` Count { get; }</b></h4>
		<p>The amount of enqueued objects</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` Item { get; }</b></h4>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``FixedQueue<T>``](/api/fixed-queue%3C-t%3E/)(``int`` maxSize);</b></h4>
		<p>Creates a new FixedQueue with a given size</p>
	</div>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` maxSize</p>
			<p>The size of the queue</p>
		</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Enqueue(``T`` t);</b></h4>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``T`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` Dequeue();</b></h4>
		<p>Dequeues an object</p>
		<h5 markdown="1"><b>Returns ``T``</b></h5>
		<div>
			<p></p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` ElementAt(``int`` index);</b></h4>
		<p>Gets the element at a given virtual index</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``int`` index</p>
			<p>The virtual index to get the item from</p>
		</div>
		<h5 markdown="1"><b>Returns ``T``</b></h5>
		<div>
			<p>The element at the virtual index</p>
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
