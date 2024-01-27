using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public static class EqualityHelper
    {
        /// <summary>
        /// Custom equal assertion, that fix the issue with Pose equality
        /// See https://forum.unity.com/threads/pose-equality-not-implemented-in-accordance-with-quaterion.1484145/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="compareValue"></param>
        public static void AreEqual<T>(T value, T compareValue)
        {
            if (value.GetType() == typeof(Pose)) {
                object v = value;
                object cv = compareValue;
                Assert.AreEqual(((Pose)v).position, ((Pose)cv).position);
                Assert.AreEqual(((Pose)v).rotation, ((Pose)cv).rotation);
            } else {
                Assert.AreEqual(value, compareValue);
            }
        }
    }
}
