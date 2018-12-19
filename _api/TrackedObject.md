---
title: TrackedObject
permalink: /api/tracked-object/
---

<div style="line-height: 1;">
	<h2 markdown="1">TrackedObject ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>A component used for lag compensation. Each object with this component will get tracked</p>

<div>
	<h3 markdown="1">Public Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` TotalPoints { get; }</b></h4>
		<p>Gets the total amount of points stored in the component</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` AvgTimeBetweenPointsMs { get; }</b></h4>
		<p>Gets the average amount of time between the points in miliseconds</p>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``float`` TotalTimeHistory { get; }</b></h4>
		<p>Gets the total time history we have for this object</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Properties</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` useGUILayout { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` runInEditMode { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` enabled { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Behaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` isActiveAndEnabled { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Behaviour``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Transform`` transform { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``GameObject`` gameObject { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` tag { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` rigidbody { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` rigidbody2D { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` camera { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` light { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` animation { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` constantForce { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` renderer { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` audio { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiText { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` networkView { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiElement { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` guiTexture { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` collider { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` collider2D { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` hingeJoint { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` particleEmitter { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` particleSystem { get; }</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` name { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``HideFlags`` hideFlags { get; set; }</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
</div>
<br>
<div>
	<h3>Public Constructors</h3>
	<div style="line-height: 1; ">
		<h4 markdown="1"><b>public [``TrackedObject``](/MLAPI/api/tracked-object/)();</b></h4>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Inherited Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsInvoking();</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CancelInvoke();</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` Invoke(``string`` methodName, ``float`` time);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` time</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` InvokeRepeating(``string`` methodName, ``float`` time, ``float`` repeatRate);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` time</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``float`` repeatRate</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` CancelInvoke(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` IsInvoking(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine(``string`` methodName, ``object`` value);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine(``IEnumerator`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IEnumerator`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Coroutine`` StartCoroutine_Auto(``IEnumerator`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IEnumerator`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopCoroutine(``IEnumerator`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``IEnumerator`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopCoroutine(``Coroutine`` routine);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Coroutine`` routine</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopCoroutine(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` StopAllCoroutines();</b></h4>
		<h5 markdown="1">Inherited from: ``MonoBehaviour``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponent(``Type`` type);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponent();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponent(``string`` type);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` type</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponentInChildren(``Type`` t, ``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponentInChildren(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponentInChildren(``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponentInChildren();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInChildren(``Type`` t, ``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInChildren(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInChildren(``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponentsInChildren(``bool`` includeInactive, ``List<T>`` result);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` result</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInChildren();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponentsInChildren(``List<T>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component`` GetComponentInParent(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T`` GetComponentInParent();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInParent(``Type`` t, ``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponentsInParent(``Type`` t);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` t</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInParent(``bool`` includeInactive);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponentsInParent(``bool`` includeInactive, ``List<T>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``bool`` includeInactive</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponentsInParent();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Component[]`` GetComponents(``Type`` type);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponents(``Type`` type, ``List<Component>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``Type`` type</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<Component>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` GetComponents(``List<T>`` results);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``List<T>`` results</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``T[]`` GetComponents();</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` CompareTag(``string`` tag);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` tag</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName, ``object`` value, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName, ``object`` value);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessageUpwards(``string`` methodName, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName, ``object`` value);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName, ``object`` value, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` value</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` SendMessage(``string`` methodName, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName, ``object`` parameter, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` parameter</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName, ``object`` parameter);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` parameter</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``void`` BroadcastMessage(``string`` methodName, ``SendMessageOptions`` options);</b></h4>
		<h5 markdown="1">Inherited from: ``Component``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` methodName</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``SendMessageOptions`` options</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetInstanceID();</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``int`` GetHashCode();</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``bool`` Equals(``object`` other);</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``object`` other</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``string`` ToString();</b></h4>
		<h5 markdown="1">Inherited from: ``Object``</h5>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public ``Type`` GetType();</b></h4>
		<h5 markdown="1">Inherited from: ``object``</h5>
	</div>
</div>
<br>
