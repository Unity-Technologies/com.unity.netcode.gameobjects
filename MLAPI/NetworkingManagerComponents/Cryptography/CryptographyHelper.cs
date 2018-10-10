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
    }
}
#endif
