using System.Collections.Generic;
using MLAPI.Serialization;

/// <summary>
/// StatsInfoContainer
/// Used to transfer server statistics to a requesting client
/// Experimental Phase:
/// The Try/Catch blocks are only to catch any issues during experimental phase
/// </summary>
public struct StatsInfoContainer : INetworkSerializable
{
    public List<float> StatValues;

    public void NetworkSerialize(NetworkSerializer serializer)
    {
        if (serializer.IsReading)
        {
            float[] StatValuesArray = null;
            serializer.Serialize(ref StatValuesArray);
            StatValues = new List<float>(StatValuesArray);
        }
        else
        {
            float[] StatValuesArray = StatValues?.ToArray() ?? new float[0];
            serializer.Serialize(ref StatValuesArray);
        }
    }
}

