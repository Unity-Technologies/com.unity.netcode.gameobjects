using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    /// <inheritdoc />
    /// <remarks>
    /// This is a buffered linear interpolator for a <see cref="float"/> type value
    /// </remarks>
    public class BufferedLinearInterpolatorFloat : BufferedLinearInterpolator<float>
    {
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float InterpolateUnclamped(float start, float end, float time)
        {
            return Mathf.LerpUnclamped(start, end, time);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float Interpolate(float start, float end, float time)
        {
            return Mathf.Lerp(start, end, time);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override bool IsApproximately(float first, float second, float precision = 1E-06F)
        {
            return Mathf.Approximately(first, second);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override float SmoothDamp(float current, float target, ref float rateOfChange, float duration, float deltaTime, float maxSpeed = float.PositiveInfinity)
        {
            return Mathf.SmoothDamp(current, target, ref rateOfChange, duration, maxSpeed, deltaTime);
        }
    }
}
