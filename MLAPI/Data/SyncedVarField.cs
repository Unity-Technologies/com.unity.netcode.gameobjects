using System.Reflection;
using MLAPI.Attributes;

namespace MLAPI.Data
{
    internal class SyncedVarField
    {
        internal FieldInfo FieldInfo;
        internal object FieldValue;
        internal MethodInfo HookMethod;
        internal bool Dirty;
        internal bool Target;
        internal SyncedVar Attribute;
    }
}
