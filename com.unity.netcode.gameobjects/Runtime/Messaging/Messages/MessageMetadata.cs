namespace Unity.Netcode
{
    public struct MessageVersionData
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
