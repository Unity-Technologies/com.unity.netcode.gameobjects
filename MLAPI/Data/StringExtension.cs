using System;

namespace MLAPI.Data
{
    public static class StringExtension
    {
        private const uint FNV_offset_basis32 = 2166136261;
        private const uint FNV_prime32 = 16777619;

        /// <summary>
        /// non cryptographic stable hash code,  
        /// it will always return the same hash for the same
        /// string.  
        /// 
        /// This is simply an implementation of FNV-1
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
    }
}
