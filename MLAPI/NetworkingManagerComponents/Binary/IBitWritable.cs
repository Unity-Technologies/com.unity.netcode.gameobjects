using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MLAPI.Serialization 
{
    /// <summary>
    /// IBitWritable provides a interface for writable types
    /// </summary>
    public interface IBitWritable 
    {
        /// <summary>
        /// Writes the contents of the type instance to the stream
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        void Write(Stream stream);
        /// <summary>
        /// Reads the contents from the stream and applies it to the type instance
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        void Read(Stream stream);
    }
}
