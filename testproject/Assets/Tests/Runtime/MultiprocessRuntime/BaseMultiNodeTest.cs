namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public abstract class BaseMultiNodeTest
    {
        public virtual void SetupTestSuite()
        {
            // MultiprocessOrchestration.StartLocalMultiNodeClient(); // will automatically start built player as clients
        }
    }
}
