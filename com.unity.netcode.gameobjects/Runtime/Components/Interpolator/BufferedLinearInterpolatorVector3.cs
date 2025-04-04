using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// A <see cref="BufferedLinearInterpolator{T}"/> <see cref="Vector3"/> implementation.
    /// </summary>
    public class BufferedLinearInterpolatorVector3 : BufferedLinearInterpolator<Vector3>
    {
        /// <summary>
        /// Use <see cref="Vector3.Slerp"/> when <see cref="true"/>.
        /// Use <see cref="Vector3.Lerp"/> when <see cref="false"/>
        /// </summary>
        public bool IsSlerp;
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Vector3 InterpolateUnclamped(Vector3 start, Vector3 end, float time)
        {
            if (IsSlerp)
            {
                return Vector3.SlerpUnclamped(start, end, time);
            }
            else
            {
                return Vector3.LerpUnclamped(start, end, time);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Vector3 Interpolate(Vector3 start, Vector3 end, float time)
        {
            if (IsSlerp)
            {
                return Vector3.Slerp(start, end, time);
            }
            else
            {
                return Vector3.Lerp(start, end, time);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal override Vector3 OnConvertTransformSpace(Transform transform, Vector3 position, bool inLocalSpace)
        {
            if (inLocalSpace)
            {
                return transform.InverseTransformPoint(position);

            }
            else
            {
                return transform.TransformPoint(position);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override bool IsApproximately(Vector3 first, Vector3 second, float precision = 1E-06F)
        {
            return Math.Round(Mathf.Abs(first.x - second.x), 2) <= precision &&
                Math.Round(Mathf.Abs(first.y - second.y), 2) <= precision &&
                Math.Round(Mathf.Abs(first.z - second.z), 2) <= precision;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 rateOfChange, float duration, float deltaTime, float maxSpeed)
        {
            return Vector3.SmoothDamp(current, target, ref rateOfChange, duration, maxSpeed, deltaTime);
        }
    }
}
