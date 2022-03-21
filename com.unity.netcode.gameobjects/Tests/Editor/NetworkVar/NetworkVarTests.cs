using NUnit.Framework;

namespace Unity.Netcode.EditorTests.NetworkVar
{
    public class NetworkVarTests
    {
        [Test]
        public void TestAssignmentUnchanged()
        {
            NetworkVariable<int> intVar = new NetworkVariable<int>();

            intVar.Value = 314159265;

            intVar.OnValueChanged += (value, newValue) =>
            {
                Assert.Fail("OnValueChanged was invoked when setting the same value");
            };

            intVar.Value = 314159265;
        }

        [Test]
        public void TestAssignmentChanged()
        {
            NetworkVariable<int> intVar = new NetworkVariable<int>();

            intVar.Value = 314159265;

            bool changed = false;

            intVar.OnValueChanged += (value, newValue) =>
            {
                changed = true;
            };

            intVar.Value = 314159266;

            Assert.True(changed);
        }
    }
}
