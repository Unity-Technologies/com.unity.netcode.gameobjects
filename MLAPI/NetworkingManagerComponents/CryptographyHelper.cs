using System;
using System.Security.Cryptography;
using System.IO;

namespace MLAPI.NetworkingManagerComponents
{
    public static class CryptographyHelper
    {
        /// <summary>
        /// Decrypts a message with AES with a given key and a salt that is encoded as the first 16 bytes of the buffer
        /// </summary>
        /// <param name="encryptedBuffer">The buffer with the salt</param>
        /// <param name="key">The key to use</param>
        /// <returns>The decrypted byte array</returns>
        public static byte[] Decrypt(byte[] encryptedBuffer, byte[] key)
        {
            byte[] iv = new byte[16];
            Array.Copy(encryptedBuffer, 0, iv, 0, 16);

            using (MemoryStream stream = new MemoryStream())
            {
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.IV = iv;
                    aes.Key = key;
                    using (CryptoStream cs = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedBuffer, 16, encryptedBuffer.Length - 16);
                    }
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Encrypts a message with AES with a given key and a random salt that gets encoded as the first 16 bytes of the encrypted buffer
        /// </summary>
        /// <param name="clearBuffer">The buffer to be encrypted</param>
        /// <param name="key">The key to use</param>
        /// <returns>The encrypted byte array with encoded salt</returns>
        public static byte[] Encrypt(byte[] clearBuffer, byte[] key)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    using (CryptoStream cs = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBuffer, 0, clearBuffer.Length);
                    }
                    byte[] encrypted = stream.ToArray();
                    byte[] final = new byte[encrypted.Length + 16];
                    Array.Copy(aes.IV, final, 16);
                    Array.Copy(encrypted, 0, final, 16, encrypted.Length);
                    return final;
                }
            }
        }
    }
}
