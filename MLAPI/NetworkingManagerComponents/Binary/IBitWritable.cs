using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MLAPI.Serialization 
{ 
    public interface IBitWritable 
    {
        void Write(BitWriter writer);
        void Read(BitReader reader);
    }
}
