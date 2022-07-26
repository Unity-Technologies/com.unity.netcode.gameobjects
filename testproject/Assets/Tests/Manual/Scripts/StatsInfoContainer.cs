using System.Collections.Generic;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    /// <summary>
    /// StatsInfoContainer
    /// Used to transfer server statistics to a requesting client
    /// </summary>
    public struct StatsInfoContainer : INetworkSerializable
    {
        public List<float> StatValues;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                float[] statValuesArray = null;
                serializer.SerializeValue(ref statValuesArray);
                StatValues = new List<float>(statValuesArray);
            }
            else
            {
                float[] statValuesArray = StatValues?.ToArray() ?? new float[0];
                serializer.SerializeValue(ref statValuesArray);
            }
        }
    }
}
