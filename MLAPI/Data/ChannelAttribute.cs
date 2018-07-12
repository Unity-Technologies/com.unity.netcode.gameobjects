using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MLAPI.Data
{
    public class ChannelSetting : Attribute
    {
        public string channel { get; set; } = "MLAPI_DEFAULT_MESSAGE";
    }
}
