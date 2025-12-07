using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    /// <inheritdoc />
    /// <remarks>
    /// This is a buffered linear interpolator for a <see cref="Quaternion"/> type value
    /// </remarks>
    public class BufferedLinearInterpolatorQuaternion : BufferedLinearInterpolator<Quaternion>
    {
        /// <summary>
        /// Use <see cref="Quaternion.Slerp"/> when <see cref="true"/>.
        /// Use <see cref="Quaternion.Lerp"/> when <see cref="false"/>
        /// </summary>
        /// <remarks>
        /// When using half precision (due to the imprecision) using <see cref="Quaternion.Lerp"/> is
        /// less processor intensive (i.e. precision is already "imprecise").
        /// When using full precision (to maintain precision) using <see cref="Quaternion.Slerp"/> is
        /// more processor intensive yet yields more precise results.
        /// </remarks>
        public bool IsSlerp;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Quaternion InterpolateUnclamped(Quaternion start, Quaternion end, float time)
        {
            if (IsSlerp)
            {
                return Quaternion.SlerpUnclamped(start, end, time);
            }
            else
            {
                return Quaternion.LerpUnclamped(start, end, time);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Quaternion Interpolate(Quaternion start, Quaternion end, float time)
        {
            if (IsSlerp)
            {
                return Quaternion.Slerp(start, end, time);
            }
            else
            {
                return Quaternion.Lerp(start, end, time);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Quaternion rateOfChange, float duration, float deltaTime, float maxSpeed = float.PositiveInfinity)
        {
            Vector3 currentEuler = current.eulerAngles;
            Vector3 targetEuler = target.eulerAngles;
            for (int i = 0; i < 3; i++)
            {
                var velocity = rateOfChange[i];
                currentEuler[i] = Mathf.SmoothDampAngle(currentEuler[i], targetEuler[i], ref velocity, duration, maxSpeed, deltaTime);
                rateOfChange[i] = velocity;
            }
            return Quaternion.Euler(currentEuler);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override bool IsApproximately(Quaternion first, Quaternion second, float precision = 1E-06F)
        {
            return Mathf.Abs(first.x - second.x) <= precision &&
                Mathf.Abs(first.y - second.y) <= precision &&
                Mathf.Abs(first.z - second.z) <= precision &&
                Mathf.Abs(first.w - second.w) <= precision;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal override Quaternion OnConvertTransformSpace(Transform transform, Quaternion rotation, bool inLocalSpace)
        {
            if (inLocalSpace)
            {
                return Quaternion.Inverse(transform.rotation) * rotation;
            }
            else
            {
                return transform.rotation * rotation;
            }
        }
    }
}
