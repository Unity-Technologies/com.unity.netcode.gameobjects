namespace Unity.Netcode
{
    /// <summary>
    /// Conveys a version number on a remote node for the given message (identified by its hash)
    /// </summary>
    internal struct MessageVersionData
    {
        public uint Hash;
        public int Version;

        public void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Hash);
            BytePacker.WriteValueBitPacked(writer, Version);
        }

        public void Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out Hash);
            ByteUnpacker.ReadValueBitPacked(reader, out Version);
        }
    }
}
