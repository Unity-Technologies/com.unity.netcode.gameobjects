---
title: CryptographyHelper
name: CryptographyHelper
permalink: /api/cryptography-helper/
---

<div style="line-height: 1;">
	<h2 markdown="1">CryptographyHelper ``class``</h2>
	<p style="font-size: 20px;"><b>Namespace:</b> MLAPI.Security</p>
	<p style="font-size: 20px;"><b>Assembly:</b> MLAPI.dll</p>
</div>
<p>Helper class for encryption purposes</p>

<div>
	<h3 markdown="1">Public Fields</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public [``VerifyCertificateDelegate``](/api/verify-certificate-delegate/) OnValidateCertificateCallback;</b></h4>
		<p>The delegate to invoke to validate the certificates</p>
	</div>
</div>
<br>
<div>
	<h3 markdown="1">Public Static Methods</h3>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``bool`` VerifyCertificate(``X509Certificate2`` certificate, ``string`` hostname);</b></h4>
		<p></p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``X509Certificate2`` certificate</p>
			<p>The certificate to validate</p>
		</div>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``string`` hostname</p>
			<p>The hostname the certificate is claiming to be</p>
		</div>
		<h5 markdown="1"><b>Returns ``bool``</b></h5>
		<div>
			<p>Whether or not the certificate is considered valid</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``byte[]`` GetClientKey(``ulong`` clientId);</b></h4>
		<p>Gets the aes key for any given clientId</p>
		<h5><b>Parameters</b></h5>
		<div>
			<p style="font-size: 20px; color: #444;" markdown="1">``ulong`` clientId</p>
			<p>The clientId of the client whose aes key we want</p>
		</div>
		<h5 markdown="1"><b>Returns ``byte[]``</b></h5>
		<div>
			<p>The aes key in binary</p>
		</div>
	</div>
	<br>
	<div style="line-height: 1;">
		<h4 markdown="1"><b>public static ``byte[]`` GetServerKey();</b></h4>
		<p>Gets the aes key for the server</p>
		<h5 markdown="1"><b>Returns ``byte[]``</b></h5>
		<div>
			<p>The servers aes key</p>
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
