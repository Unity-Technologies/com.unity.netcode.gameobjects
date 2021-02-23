namespace MLAPI.Profiling
{
    public class PerformanceTickData
    {
        public int tickID;

        public readonly KvStore tickData = new KvStore();

        public void Increment(string fieldName, int count = 1)
        {
            tickData.Increment(fieldName, count);
        }

        public int GetData(string fieldName)
        {
            return tickData.GetData(fieldName);
        }
    }
}
