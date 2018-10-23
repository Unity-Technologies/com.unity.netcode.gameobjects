#if !DISABLE_CRYPTOGRAPHY
using System.Security.Cryptography;
using System.IO;
using MLAPI.Serialization;
using System.Security.Cryptography.X509Certificates;
using System;

namespace MLAPI.Cryptography
{
    /// <summary>
    /// Helper class for encryption purposes
    /// </summary>
    public static class CryptographyHelper
    {
        public delegate bool VerifyCertificateDelegate(X509Certificate2 certificate, string hostname);
        public static VerifyCertificateDelegate OnValidateCertificateCallback = null;
        
        public static bool VerifyCertificate(X509Certificate2 certificate, string hostname)
        {
            if (OnValidateCertificateCallback != null) return OnValidateCertificateCallback(certificate, hostname);
            return certificate.Verify() && (hostname == certificate.GetNameInfo(X509NameType.DnsName, false) || hostname == "127.0.0.1");
        }

        public static byte[] GetClientKey(uint clientId)
        {
            if (NetworkingManager.singleton.isServer)
            {
                if (NetworkingManager.singleton.ConnectedClients.ContainsKey(clientId))
                {
                    return NetworkingManager.singleton.ConnectedClients[clientId].AesKey;
                }
                else if (NetworkingManager.singleton.PendingClients.ContainsKey(clientId))
                {
                    return NetworkingManager.singleton.PendingClients[clientId].AesKey;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static byte[] GetServerKey()
        {
            if (NetworkingManager.singleton.isServer)
            {
                return null;
            }
            else
            {
                return NetworkingManager.singleton.clientAesKey;
            }
        }
    }
}
#endif
