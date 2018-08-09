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
        private static readonly byte[] IVBuffer = new byte[16];
        /// <summary>
        /// Decrypts a message with AES with a given key and a salt that is encoded as the first 16 bytes of the buffer
        /// </summary>
        /// <param name="encryptedStream">The encrypted stream</param>
        /// <param name="clientId">The clientId whose AES key to use</param>
        /// <returns>The decrypted stream</returns>
        public static Stream DecryptStream(Stream encryptedStream, uint clientId)
        {
            encryptedStream.Read(IVBuffer, 0, 16);
            
            using (RijndaelManaged aes = new RijndaelManaged())
            {
                aes.IV = IVBuffer;
                aes.Key = NetworkingManager.singleton.isServer ? NetworkingManager.singleton.ConnectedClients[clientId].AesKey : NetworkingManager.singleton.clientAesKey;
                using (CryptoStream cs = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (PooledBitStream outStream = PooledBitStream.Get())
                    {
                        outStream.CopyFrom(cs);
                        return outStream;
                    }
                }
            }
        }

        /// <summary>
        /// Encrypts a message with AES with a given key and a random salt that gets encoded as the first 16 bytes of the encrypted buffer
        /// </summary>
        /// <param name="clearStream">The stream to be encrypted</param>
        /// <param name="clientId">The clientId whose AES key to use</param>
        /// <returns>The encrypted stream with encoded salt</returns>
        public static Stream EncryptStream(Stream clearStream, uint clientId)
        {
            using (RijndaelManaged aes = new RijndaelManaged())
            {
                aes.Key = NetworkingManager.singleton.isServer ? NetworkingManager.singleton.ConnectedClients[clientId].AesKey : NetworkingManager.singleton.clientAesKey;
                aes.GenerateIV();
                
                using (CryptoStream cs = new CryptoStream(clearStream, aes.CreateEncryptor(), CryptoStreamMode.Read))
                {
                    using (PooledBitStream outStream = PooledBitStream.Get())
                    {
                        outStream.Write(aes.IV, 0, 16);
                        outStream.CopyFrom(cs);
                        return outStream;
                    }
                }
            }
        }

        public static bool VerifyCertificate(X509Certificate2 certificate, string hostname)
        {
            if (OnValidateCertificateCallback != null) return OnValidateCertificateCallback(certificate, hostname);
            return certificate.Verify() && hostname == certificate.GetNameInfo(X509NameType.DnsName, false);
        }
    }
}
#endif
