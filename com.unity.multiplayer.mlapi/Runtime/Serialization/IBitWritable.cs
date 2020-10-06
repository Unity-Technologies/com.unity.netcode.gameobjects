using System;
using System.IO;
using System.Reflection;

namespace MLAPI.Serialization 
{
    /// <summary>
    /// Interface for serializable classes and structs
    /// </summary>
    public interface IBitWritable
    {
        /// <summary>
        /// Reads the contents from the stream and applies it to the type instance
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        void Read(Stream stream);
        /// <summary>
        /// Writes the contents of the type instance to the stream
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        void Write(Stream stream);
    }
}
