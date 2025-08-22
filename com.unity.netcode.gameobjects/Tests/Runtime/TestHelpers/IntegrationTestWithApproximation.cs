using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// An extended <see cref="NetcodeIntegrationTest"/> class that includes various helper mehtods
    /// to handle aproximating <see cref="float"/> various in order to avoid floating point drift
    /// related errors.
    /// </summary>
    public abstract class IntegrationTestWithApproximation : NetcodeIntegrationTest
    {
        private const float k_AproximateDeltaVariance = 0.016f;

        /// <summary>
        /// Returns a six decimal place string to represent a <see cref="Vector3"/> value.
        /// </summary>
        /// <param name="vector3">A reference to the <see cref="Vector3"/> value to convert to a string.</param>
        /// <returns><see cref="string"/> representing the <see cref="Vector3"/> passed in.</returns>
        protected string GetVector3Values(ref Vector3 vector3)
        {
            return $"({vector3.x:F6},{vector3.y:F6},{vector3.z:F6})";
        }

        /// <summary>
        /// Returns a six decimal place string to represent a <see cref="Vector3"/> value.
        /// </summary>
        /// <param name="vector3">The <see cref="Vector3"/> value to convert to a string.</param>
        /// <returns><see cref="string"/> representing the <see cref="Vector3"/> passed in.</returns>
        protected string GetVector3Values(Vector3 vector3)
        {
            return GetVector3Values(ref vector3);
        }

        /// <summary>
        /// Override this to make changes to the precision level used.
        /// </summary>
        /// <returns>A <see cref="float"/> value representing the minimum tolerence value required to be considered an approximation of a <see cref="float"/> value comparison.</returns>
        protected virtual float GetDeltaVarianceThreshold()
        {
            return k_AproximateDeltaVariance;
        }

        /// <summary>
        /// Returns the delta between two Euler angles.
        /// </summary>
        /// <param name="a">The first Euler value.</param>
        /// <param name="b">The second Euler value.</param>
        /// <returns>A <see cref="float"/> delta between the two <see cref="float"/> values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float EulerDelta(float a, float b)
        {
            return Mathf.DeltaAngle(a, b);
        }

        /// <summary>
        /// Returns the delta between two <see cref="Vector3"/> values that
        /// represent Euler angles.
        /// </summary>
        /// <param name="a">The first Euler <see cref="Vector3"/> value.</param>
        /// <param name="b">The second Euler <see cref="Vector3"/> value.</param>
        /// <returns>A <see cref="Vector3"/> delta between the two <see cref="Vector3"/> values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 EulerDelta(Vector3 a, Vector3 b)
        {
            return new Vector3(Mathf.DeltaAngle(a.x, b.x), Mathf.DeltaAngle(a.y, b.y), Mathf.DeltaAngle(a.z, b.z));
        }

        /// <summary>
        /// Determines an aproximated comparison between two Euler values.
        /// </summary>
        /// <param name="a">The first Euler value.</param>
        /// <param name="b">The second Euler value.</param>
        /// <returns>Returns <see cref="true"/> if they are approximately the same and <see cref="false"/> if they are not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ApproximatelyEuler(float a, float b)
        {
            return Mathf.Abs(EulerDelta(a, b)) <= GetDeltaVarianceThreshold();
        }

        /// <summary>
        /// Determines an aproximated comparison between two <see cref="float"/> values.
        /// </summary>
        /// <param name="a">The first <see cref="float"/> value.</param>
        /// <param name="b">The second <see cref="float"/> value.</param>
        /// <returns>Returns <see cref="true"/> if they are approximately the same and <see cref="false"/> if they are not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= GetDeltaVarianceThreshold();
        }

        /// <summary>
        /// Determines an aproximated comparison between two <see cref="Vector2"/> values.
        /// </summary>
        /// <param name="a">The first <see cref="Vector2"/> value.</param>
        /// <param name="b">The second <see cref="Vector2"/> value.</param>
        /// <returns>Returns <see cref="true"/> if they are approximately the same and <see cref="false"/> if they are not.</returns>
        protected bool Approximately(Vector2 a, Vector2 b)
        {
            var deltaVariance = GetDeltaVarianceThreshold();
            return Math.Round(Mathf.Abs(a.x - b.x), 2) <= deltaVariance &&
                Math.Round(Mathf.Abs(a.y - b.y), 2) <= deltaVariance;
        }

        /// <summary>
        /// Determines an aproximated comparison between two <see cref="Vector3"/> values.
        /// </summary>
        /// <param name="a">The first <see cref="Vector3"/> value.</param>
        /// <param name="b">The second <see cref="Vector3"/> value.</param>
        /// <returns>Returns <see cref="true"/> if they are approximately the same and <see cref="false"/> if they are not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Approximately(Vector3 a, Vector3 b)
        {
            var deltaVariance = GetDeltaVarianceThreshold();
            return Math.Round(Mathf.Abs(a.x - b.x), 2) <= deltaVariance &&
                Math.Round(Mathf.Abs(a.y - b.y), 2) <= deltaVariance &&
                Math.Round(Mathf.Abs(a.z - b.z), 2) <= deltaVariance;
        }

        /// <summary>
        /// Determines an aproximated comparison between two <see cref="Quaternion"/> values.
        /// </summary>
        /// <param name="a">The first <see cref="Quaternion"/> value.</param>
        /// <param name="b">The second <see cref="Quaternion"/> value.</param>
        /// <returns>Returns <see cref="true"/> if they are approximately the same and <see cref="false"/> if they are not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Approximately(Quaternion a, Quaternion b)
        {
            var deltaVariance = GetDeltaVarianceThreshold();
            return Mathf.Abs(a.x - b.x) <= deltaVariance &&
                Mathf.Abs(a.y - b.y) <= deltaVariance &&
                Mathf.Abs(a.z - b.z) <= deltaVariance &&
                Mathf.Abs(a.w - b.w) <= deltaVariance;
        }

        /// <summary>
        /// Determines an aproximated comparison between two <see cref="Vector3"/> expressed in Euler values.
        /// </summary>
        /// <param name="a">The first <see cref="Vector3"/> expressed in Euler values.</param>
        /// <param name="b">The second <see cref="Vector3"/> expressed in Euler values.</param>
        /// <returns>Returns <see cref="true"/> if they are approximately the same and <see cref="false"/> if they are not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ApproximatelyEuler(Vector3 a, Vector3 b)
        {
            return ApproximatelyEuler(a.x, b.x) && ApproximatelyEuler(a.y, b.y) && ApproximatelyEuler(a.z, b.z);
        }

        /// <summary>
        /// Returns a randomly generated <see cref="Vector3"/> based on the min and max range specified in the parameters.
        /// </summary>
        /// <remarks>
        /// Each axis value is a randomly generated value between min and max.
        /// </remarks>
        /// <param name="min">The minimum <see cref="float"/> value.</param>
        /// <param name="max">The maximum <see cref="float"/> value.</param>
        /// <returns>The randomly generated <see cref="Vector3"/> result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 GetRandomVector3(float min, float max)
        {
            return new Vector3(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
        }

        /// <summary>
        /// Overloaded constructor accepting the <see cref="NetworkTopologyTypes"/> and <see cref="NetcodeIntegrationTest.HostOrServer"/> as parameters.
        /// </summary>
        /// <remarks>
        /// This is useful when using the <see cref="TestFixtureAttribute"/> with your integration test.
        /// </remarks>
        /// <param name="networkTopologyType">The <see cref="NetworkTopologyTypes"/> to use for the <see cref="TestFixtureAttribute"/> pass.</param>
        /// <param name="hostOrServer">The <see cref="NetcodeIntegrationTest.HostOrServer"/> to use for the <see cref="TestFixtureAttribute"/> pass.</param>
        public IntegrationTestWithApproximation(NetworkTopologyTypes networkTopologyType, HostOrServer hostOrServer) : base(networkTopologyType, hostOrServer) { }

        /// <summary>
        /// Overloaded constructor accepting the <see cref="NetworkTopologyTypes"/> as a parameter.
        /// </summary>
        /// <remarks>
        /// This is useful when using the <see cref="TestFixtureAttribute"/> with your integration test.
        /// </remarks>
        /// <param name="networkTopologyType">The <see cref="NetworkTopologyTypes"/> to use for the <see cref="TestFixtureAttribute"/> pass.</param>
        public IntegrationTestWithApproximation(NetworkTopologyTypes networkTopologyType) : base(networkTopologyType) { }

        /// <summary>
        /// Overloaded constructor accepting the  <see cref="NetcodeIntegrationTest.HostOrServer"/> as a parameter.
        /// </summary>
        /// <remarks>
        /// This is useful when using the <see cref="TestFixtureAttribute"/> with your integration test.
        /// </remarks>
        /// <param name="hostOrServer">The <see cref="NetcodeIntegrationTest.HostOrServer"/> to use for the <see cref="TestFixtureAttribute"/> pass.</param>
        public IntegrationTestWithApproximation(HostOrServer hostOrServer) : base(hostOrServer) { }

        /// <summary>
        /// Default constructor with no parameters.
        /// </summary>
        public IntegrationTestWithApproximation() : base() { }
    }
}
