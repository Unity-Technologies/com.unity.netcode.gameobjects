#if !UNITY_TEST_FRAMEWORK_1_7_OR_NEWER
using System;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    internal sealed class UnityCoreClrExplicitDisabledAttribute : Attribute
    {
        public UnityCoreClrExplicitDisabledAttribute(string jiraIssue, string reason = null)
        {
        }
    }
}
#endif
