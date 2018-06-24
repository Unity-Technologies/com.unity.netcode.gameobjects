using System;

namespace MLAPI.Data
{
    /// <summary>
    /// Provides extension methods for getting hashes
    /// </summary>
    public static class HashCode
    {
        private const uint FNV_offset_basis32 = 2166136261;
        private const uint FNV_prime32 = 16777619;

        private const ulong FNV_offset_basis64 = 14695981039346656037;
        private const ulong FNV_prime64 = 1099511628211;

        /// <summary>
        /// non cryptographic stable hash code,  
        /// it will always return the same hash for the same
        /// string.  
        /// 
        /// This is simply an implementation of FNV-1 32 bit xor folded to 16 bit
        /// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <returns>The stable hash32.</returns>
        /// <param name="txt">Text.</param>
        public static ushort GetStableHash16(this string txt)
        {
            uint hash32 = txt.GetStableHash32();

            return (ushort)((hash32 >> 16) ^ hash32);
        }


        /// <summary>
        /// non cryptographic stable hash code,  
        /// it will always return the same hash for the same
        /// string.  
        /// 
        /// This is simply an implementation of FNV-1 32 bit
        /// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <returns>The stable hash32.</returns>
        /// <param name="txt">Text.</param>
        public static uint GetStableHash32(this string txt)
        {
            unchecked
            {
                uint hash = FNV_offset_basis32;
                for (int i = 0; i < txt.Length; i++)
                {
                    uint ch = txt[i];
                    hash = hash * FNV_prime32;
                    hash = hash ^ ch;
                }
                return hash;
            }
        }

        /// <summary>
        /// non cryptographic stable hash code,  
        /// it will always return the same hash for the same
        /// string.  
        /// 
        /// This is simply an implementation of FNV-1  64 bit
        /// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <returns>The stable hash32.</returns>
        /// <param name="txt">Text.</param>
        public static ulong GetStableHash64(this string txt)
        {
            unchecked
            {
                ulong hash = FNV_offset_basis64;
                for (int i = 0; i < txt.Length; i++)
                {
                    ulong ch = txt[i];
                    hash = hash * FNV_prime64;
                    hash = hash ^ ch;
                }
                return hash;
            }
        }

        /// <summary>
        /// non cryptographic stable hash code,  
        /// it will always return the same hash for the same
        /// string.  
        /// 
        /// This is simply an implementation of FNV-1 32 bit xor folded to 16 bit
        /// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <returns>The stable hash32.</returns>
        /// <param name="bytes">Text.</param>
        public static ushort GetStableHash16(this byte[] bytes)
        {
            uint hash32 = bytes.GetStableHash32();

            return (ushort)((hash32 >> 16) ^ hash32);
        }

        /// <summary>
        /// non cryptographic stable hash code,  
        /// it will always return the same hash for the same
        /// string.  
        /// 
        /// This is simply an implementation of FNV-1 32 bit
        /// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <returns>The stable hash32.</returns>
        /// <param name="bytes">Text.</param>
        public static uint GetStableHash32(this byte[] bytes)
        {
            unchecked
            {
                uint hash = FNV_offset_basis32;
                for (int i = 0; i < bytes.Length; i++)
                {
                    uint bt = bytes[i];
                    hash = hash * FNV_prime32;
                    hash = hash ^ bt;
                }
                return hash;
            }
        }

        /// <summary>
        /// non cryptographic stable hash code,  
        /// it will always return the same hash for the same
        /// string.  
        /// 
        /// This is simply an implementation of FNV-1  64 bit
        /// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <returns>The stable hash32.</returns>
        /// <param name="bytes">Text.</param>
        public static ulong GetStableHash64(this byte[] bytes)
        {
            unchecked
            {
                ulong hash = FNV_offset_basis64;
                for (int i = 0; i < bytes.Length; i++)
                {
                    ulong bt = bytes[i];
                    hash = hash * FNV_prime64;
                    hash = hash ^ bt;
                }
                return hash;
            }
        }
    }
}
