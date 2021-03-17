namespace MLAPI.Configuration
{
    /// <summary>
    /// Represents the length of a var int encoded hash
    /// Note that the HashSize does not say anything about the actual final output due to the var int encoding
    /// It just says how many bytes the maximum will be
    /// </summary>
    public enum HashSize
    {
        /// <summary>
        /// Two byte hash
        /// </summary>
        VarIntTwoBytes,

        /// <summary>
        /// Four byte hash
        /// </summary>
        VarIntFourBytes,

        /// <summary>
        /// Eight byte hash
        /// </summary>
        VarIntEightBytes
    }
}