namespace MLAPI.Serialization
{
    /// <summary>
    /// AutoBitWritable implements INetworkSerializable and automatically serializes fields using reflection
    /// </summary>
    public abstract class AutoNetworkSerializable : INetworkSerializable
    {
        private void Write(NetworkWriter writer)
        {
            var fields = SerializationManager.GetFieldsForType(GetType());
            for (int i = 0; i < fields.Length; i++)
            {
                writer.WriteObjectPacked(fields[i].GetValue(this));
            }
        }

        private void Read(NetworkReader reader)
        {
            var fields = SerializationManager.GetFieldsForType(GetType());
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].SetValue(this, reader.ReadObjectPacked(fields[i].FieldType));
            }
        }

        public void NetworkSerialize(NetworkSerializer serializer)
        {
            if (serializer.IsReading)
            {
                Read(serializer.Reader);
            }
            else
            {
                Write(serializer.Writer);
            }
        }
    }
}