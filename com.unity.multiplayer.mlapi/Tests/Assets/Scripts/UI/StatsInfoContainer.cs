using System.Collections.Generic;
using MLAPI.Serialization;

namespace MLAPI.TestAssets.UI
{
    /// <summary>
    /// StatsInfoContainer
    /// Used to transfer server statistics to a requesting client
    /// </summary>
    internal struct StatsInfoContainer : INetworkSerializable
    {
        public List<float> StatValues;

        public void NetworkSerialize(NetworkSerializer serializer)
        {
            if (serializer.IsReading)
            {
                float[] statValuesArray = null;
                serializer.Serialize(ref statValuesArray);
                StatValues = new List<float>(statValuesArray);
            }
            else
            {
                float[] statValuesArray = StatValues?.ToArray() ?? new float[0];
                serializer.Serialize(ref statValuesArray);
            }
        }
    }

}
