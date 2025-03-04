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
        protected override float InterpolateUnclamped(float start, float end, float time)
        {
            return Mathf.LerpUnclamped(start, end, time);
        }

        /// <inheritdoc />
        protected override float Interpolate(float start, float end, float time)
        {
            return Mathf.Lerp(start, end, time);
        }

        /// <inheritdoc />
        protected internal override bool IsAproximately(float first, float second, float precision = 1E-07F)
        {
            return Mathf.Approximately(first, second);
        }

        /// <inheritdoc />
        protected internal override float SmoothDamp(float current, float target, ref float rateOfChange, float duration, float deltaTime, float maxSpeed = float.PositiveInfinity)
        {
            if (IsAngularValue)
            {
                return Mathf.SmoothDampAngle(current, target, ref rateOfChange, duration, maxSpeed, deltaTime);
            }
            else
            {
                return Mathf.SmoothDamp(current, target, ref rateOfChange, duration, maxSpeed, deltaTime);
            }
        }
    }
}
