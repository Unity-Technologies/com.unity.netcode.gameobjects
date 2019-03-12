---
title: HashCode
name: HashCode
permalink: /api/hash-code/
---

<div style="line-height: 1;">
	<h2 markdown="1">HashCode ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Data</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Provides extension methods for getting hashes</p>

<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ushort`` GetStableHash16(``string`` txt);</b></h4>
		<p>non cryptographic stable hash code,  
            it will always return the same hash for the same
            string.  
            
            This is simply an implementation of FNV-1 32 bit xor folded to 16 bit
            https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` txt</p>
			<p>Text.</p>
		</div>
		<h5 markdown="1"><b>Returns ``ushort``</b></h5>
		<div>
			<p>The stable hash32.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``uint`` GetStableHash32(``string`` txt);</b></h4>
		<p>non cryptographic stable hash code,  
            it will always return the same hash for the same
            string.  
            
            This is simply an implementation of FNV-1 32 bit
            https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` txt</p>
			<p>Text.</p>
		</div>
		<h5 markdown="1"><b>Returns ``uint``</b></h5>
		<div>
			<p>The stable hash32.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ulong`` GetStableHash64(``string`` txt);</b></h4>
		<p>non cryptographic stable hash code,  
            it will always return the same hash for the same
            string.  
            
            This is simply an implementation of FNV-1  64 bit
            https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` txt</p>
			<p>Text.</p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>The stable hash32.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ushort`` GetStableHash16(``byte[]`` bytes);</b></h4>
		<p>non cryptographic stable hash code,  
            it will always return the same hash for the same
            string.  
            
            This is simply an implementation of FNV-1 32 bit xor folded to 16 bit
            https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` bytes</p>
			<p>Text.</p>
		</div>
		<h5 markdown="1"><b>Returns ``ushort``</b></h5>
		<div>
			<p>The stable hash32.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``uint`` GetStableHash32(``byte[]`` bytes);</b></h4>
		<p>non cryptographic stable hash code,  
            it will always return the same hash for the same
            string.  
            
            This is simply an implementation of FNV-1 32 bit
            https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` bytes</p>
			<p>Text.</p>
		</div>
		<h5 markdown="1"><b>Returns ``uint``</b></h5>
		<div>
			<p>The stable hash32.</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``ulong`` GetStableHash64(``byte[]`` bytes);</b></h4>
		<p>non cryptographic stable hash code,  
            it will always return the same hash for the same
            string.  
            
            This is simply an implementation of FNV-1  64 bit
            https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``byte[]`` bytes</p>
			<p>Text.</p>
		</div>
		<h5 markdown="1"><b>Returns ``ulong``</b></h5>
		<div>
			<p>The stable hash32.</p>
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
