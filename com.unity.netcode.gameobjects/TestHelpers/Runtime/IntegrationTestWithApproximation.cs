using System.Runtime.CompilerServices;
using UnityEngine;


namespace Unity.Netcode.TestHelpers.Runtime
{
    public abstract class IntegrationTestWithApproximation : NetcodeIntegrationTest
    {
        private const float k_AproximateDeltaVariance = 0.01f;

        protected virtual float GetDeltaVarianceThreshold()
        {
            return k_AproximateDeltaVariance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float EulerDelta(float a, float b)
        {
            return Mathf.DeltaAngle(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 EulerDelta(Vector3 a, Vector3 b)
        {
            return new Vector3(Mathf.DeltaAngle(a.x, b.x), Mathf.DeltaAngle(a.y, b.y), Mathf.DeltaAngle(a.z, b.z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ApproximatelyEuler(float a, float b)
        {
            return Mathf.Abs(EulerDelta(a, b)) <= GetDeltaVarianceThreshold();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= GetDeltaVarianceThreshold();
        }

        protected bool Approximately(Vector2 a, Vector2 b)
        {
            var deltaVariance = GetDeltaVarianceThreshold();
            return Mathf.Abs(a.x - b.x) <= deltaVariance &&
                Mathf.Abs(a.y - b.y) <= deltaVariance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Approximately(Vector3 a, Vector3 b)
        {
            var deltaVariance = GetDeltaVarianceThreshold();
            return Mathf.Abs(a.x - b.x) <= deltaVariance &&
                Mathf.Abs(a.y - b.y) <= deltaVariance &&
                Mathf.Abs(a.z - b.z) <= deltaVariance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Approximately(Quaternion a, Quaternion b)
        {
            var deltaVariance = GetDeltaVarianceThreshold();
            return Mathf.Abs(a.x - b.x) <= deltaVariance &&
                Mathf.Abs(a.y - b.y) <= deltaVariance &&
                Mathf.Abs(a.z - b.z) <= deltaVariance &&
                Mathf.Abs(a.w - b.w) <= deltaVariance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ApproximatelyEuler(Vector3 a, Vector3 b)
        {
            return ApproximatelyEuler(a.x, b.x) && ApproximatelyEuler(a.y, b.y) && ApproximatelyEuler(a.z, b.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 GetRandomVector3(float min, float max)
        {
            return new Vector3(Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));
        }

    }
}
