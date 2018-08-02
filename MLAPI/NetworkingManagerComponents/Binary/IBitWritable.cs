using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MLAPI.Serialization 
{ 
    public interface IBitWritable 
    {
        void Write(Stream stream);
        void Read(Stream stream);
    }
}
