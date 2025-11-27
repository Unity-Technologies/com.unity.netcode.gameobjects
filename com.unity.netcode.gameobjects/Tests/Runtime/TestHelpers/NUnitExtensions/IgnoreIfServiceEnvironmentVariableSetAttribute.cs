using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Unity.Netcode.TestHelpers.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal class IgnoreIfServiceEnvironmentVariableSetAttribute : NUnitAttribute, IApplyToTest
    {
        public void ApplyToTest(Test test)
        {
            // NotRunnable is the more weighty status, always respect it first
            if (test.RunState == RunState.NotRunnable)
            {
                return;
            }

            if (bool.TryParse(NetcodeIntegrationTestHelpers.GetCMBServiceEnvironentVariable(), out var isTrue) && isTrue)
            {
                test.RunState = RunState.Ignored;
                test.Properties.Set("_SKIPREASON", NetcodeIntegrationTestHelpers.IgnoredForCmbServiceReason);
            }
        }
    }
}
