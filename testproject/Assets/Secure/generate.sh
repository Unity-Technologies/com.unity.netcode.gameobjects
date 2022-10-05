openssl genrsa -out clientPrivateKeyForRootCA.pem 2048
openssl req -x509 -new -nodes -key clientPrivateKeyForRootCA.pem -sha256 -days 1095 -out myGameClientCA.pem
openssl genrsa -out myGameServerPrivate.pem 2048
openssl req -new -key myGameServerPrivate.pem -out myGameServerCertificateSigningRequest.pem
openssl x509 -req -in myGameServerCertificateSigningRequest.pem -CA myGameClientCA.pem -CAkey clientPrivateKeyForRootCA.pem -CAcreateserial -out myGameServerCertificate.pem -days 365 -sha256

