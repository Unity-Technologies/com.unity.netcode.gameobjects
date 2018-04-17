using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MLAPI.Data
{
    internal class SyncedVarField
    {
        internal FieldInfo FieldInfo;
        internal FieldType FieldType;
        internal object FieldValue;
        internal MethodInfo HookMethod;
        internal bool Dirty;
        internal bool Target;
    }
}
