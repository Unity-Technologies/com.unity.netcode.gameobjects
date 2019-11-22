using System.IO;
using System.Reflection;
using MLAPI.Serialization.Pooled;

namespace MLAPI.NetworkedVar
{
    internal class SyncedVarContainer
    {
        internal SyncedVarAttribute attribute;
        internal FieldInfo field;
        internal object fieldInstance;
        internal object value;
        internal bool isDirty;
        internal float lastSyncedTime;

        internal bool IsDirty()
        {
            if (attribute.SendTickrate >= 0 && (attribute.SendTickrate == 0 || NetworkingManager.Singleton.NetworkTime - lastSyncedTime >= (1f / attribute.SendTickrate)))
            {
                lastSyncedTime = NetworkingManager.Singleton.NetworkTime;

                object newValue = field.GetValue(fieldInstance);
                object oldValue = value;

                if (newValue != oldValue || isDirty)
                {
                    isDirty = true;

                    value = newValue;

                    return true;
                }
            }

            return false;
        }

        internal void ResetDirty()
        {
            value = field.GetValue(fieldInstance);
            isDirty = false;
        }

        internal void WriteValue(Stream stream, bool checkDirty = true)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                if (checkDirty)
                {
                    // Trigger a value update
                    IsDirty();
                }

                writer.WriteObjectPacked(value);
            }
        }

        internal void ReadValue(Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                value = reader.ReadObjectPacked(field.FieldType);

                field.SetValue(fieldInstance, value);
            }
        }
    }
}
