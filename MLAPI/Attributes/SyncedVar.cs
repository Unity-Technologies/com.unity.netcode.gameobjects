using System;

namespace MLAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SyncedVar : Attribute
    {
        public string hook;

        public SyncedVar()
        {
            
        }
    }
}
