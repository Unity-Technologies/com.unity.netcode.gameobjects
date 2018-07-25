#if !DISABLE_CRYPTOGRAPHY
using System;
using System.Security.Cryptography;
using System.IO;

namespace MLAPI.Cryptography
{
    /// <summary>
    /// Helper class for encryption purposes
    /// </summary>
    public static class CryptographyHelper
    {
        internal static byte[] EncryptionBuffer;
        private static readonly byte[] IVBuffer = new byte[16];
        /// <summary>
        /// Decrypts a message with AES with a given key and a salt that is encoded as the first 16 bytes of the buffer
        /// </summary>
        /// <param name="encryptedBuffer">The buffer with the salt</param>
        /// <param name="clientId">The clientId whose AES key to use</param>
        /// <returns>The decrypted byte array</returns>
        public static Stream Decrypt(byte[] encryptedBuffer, uint clientId)
        {
            Array.Copy(IVBuffer, 0, IVBuffer, 0, 16);

            using (MemoryStream stream = new MemoryStream(EncryptionBuffer))
            {
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.IV = IVBuffer;
                    aes.Key = NetworkingManager.singleton.ConnectedClients[clientId].AesKey;
                    using (CryptoStream cs = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedBuffer, 16, encryptedBuffer.Length - 16);
                    }

                    return stream;
                }
            }
        }

        /// <summary>
        /// Encrypts a message with AES with a given key and a random salt that gets encoded as the first 16 bytes of the encrypted buffer
        /// </summary>
        /// <param name="clearBuffer">The buffer to be encrypted</param>
        /// <param name="clientId">The clientId whose AES key to use</param>
        /// <returns>The encrypted byte array with encoded salt</returns>
        public static Stream Encrypt(byte[] clearBuffer, uint clientId)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.Key = NetworkingManager.singleton.ConnectedClients[clientId].AesKey;;
                    aes.GenerateIV();
                    stream.Write(aes.IV, 0, 16);
                    using (CryptoStream cs = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBuffer, 0, clearBuffer.Length);
                    }

                    return stream;
                }
            }
        }
    }
}
#endif
