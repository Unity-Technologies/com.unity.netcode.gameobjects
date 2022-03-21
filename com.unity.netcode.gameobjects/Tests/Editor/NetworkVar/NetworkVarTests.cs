using NUnit.Framework;

namespace Unity.Netcode.EditorTests.NetworkVar
{
    public class NetworkVarTests
    {
        [Test]
        public void TestAssignmentUnchanged()
        {
            var intVar = new NetworkVariable<int>();

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
            var intVar = new NetworkVariable<int>();

            intVar.Value = 314159265;

            var changed = false;

            intVar.OnValueChanged += (value, newValue) =>
            {
                changed = true;
            };

            intVar.Value = 314159266;

            Assert.True(changed);
        }
    }
}
