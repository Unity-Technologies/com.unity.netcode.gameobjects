#if !DISABLE_CRYPTOGRAPHY
using System.Security.Cryptography;
using System.IO;
using MLAPI.Serialization;

namespace MLAPI.Cryptography
{
    /// <summary>
    /// Helper class for encryption purposes
    /// </summary>
    public static class CryptographyHelper
    {
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
                aes.Key = NetworkingManager.singleton.ConnectedClients[clientId].AesKey;
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
                aes.Key = NetworkingManager.singleton.ConnectedClients[clientId].AesKey;;
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
    }
}
#endif
