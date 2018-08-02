using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MLAPI.Serialization 
{ 
    interface IBitWritable 
    {
        void Write(BitWriter writer);
        void Read(BitReader reader);
    }
}
