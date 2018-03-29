using System;
using System.Security.Cryptography;
using System.IO;

namespace MLAPI.NetworkingManagerComponents
{
    public static class CryptographyHelper
    {
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
