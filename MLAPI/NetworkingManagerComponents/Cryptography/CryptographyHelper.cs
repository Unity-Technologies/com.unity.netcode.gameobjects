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
        /// <summary>
        /// The delegate type used to validate certificates
        /// </summary>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="hostname">The hostname the certificate is claiming to be</param>
        public delegate bool VerifyCertificateDelegate(X509Certificate2 certificate, string hostname);
        /// <summary>
        /// The delegate to invoke to validate the certificates
        /// </summary>
        public static VerifyCertificateDelegate OnValidateCertificateCallback = null;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="hostname">The hostname the certificate is claiming to be</param>
        /// <returns>Whether or not the certificate is considered valid</returns>
        public static bool VerifyCertificate(X509Certificate2 certificate, string hostname)
        {
            if (OnValidateCertificateCallback != null) return OnValidateCertificateCallback(certificate, hostname);
            return certificate.Verify() && (hostname == certificate.GetNameInfo(X509NameType.DnsName, false) || hostname == "127.0.0.1");
        }

        /// <summary>
        /// Gets the aes key for any given clientId
        /// </summary>
        /// <param name="clientId">The clientId of the client whose aes key we want</param>
        /// <returns>The aes key in binary</returns>
        public static byte[] GetClientKey(uint clientId)
        {
            if (NetworkingManager.Singleton.IsServer)
            {
                if (NetworkingManager.Singleton.ConnectedClients.ContainsKey(clientId))
                {
                    return NetworkingManager.Singleton.ConnectedClients[clientId].AesKey;
                }
                else if (NetworkingManager.Singleton.PendingClients.ContainsKey(clientId))
                {
                    return NetworkingManager.Singleton.PendingClients[clientId].AesKey;
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

        /// <summary>
        /// Gets the aes key for the server
        /// </summary>
        /// <returns>The servers aes key</returns>
        public static byte[] GetServerKey()
        {
            if (NetworkingManager.Singleton.IsServer)
            {
                return null;
            }
            else
            {
                return NetworkingManager.Singleton.clientAesKey;
            }
        }

        internal static bool ConstTimeArrayEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            
            int i = a.Length;
            int cmp = 0;
            
            while (i != 0)
            {
                --i;
                cmp |= (a[i] ^ b[i]);
            }
            
            return cmp == 0;
        }
    }
}
#endif
