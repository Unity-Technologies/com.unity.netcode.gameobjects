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
        protected internal override bool IsAproximately(Vector3 first, Vector3 second, float precision = 0.0001F)
        {
            return Vector3.Distance(first, second) <= precision;
        }

        /// <inheritdoc />
        protected internal override Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 rateOfChange, float duration, float deltaTime, float maxSpeed)
        {
            if (IsAngularValue)
            {
                current.x = Mathf.SmoothDampAngle(current.x, target.x, ref rateOfChange.x, duration, maxSpeed, deltaTime);
                current.y = Mathf.SmoothDampAngle(current.y, target.y, ref rateOfChange.y, duration, maxSpeed, deltaTime);
                current.z = Mathf.SmoothDampAngle(current.z, target.z, ref rateOfChange.z, duration, maxSpeed, deltaTime);
                return current;
            }
            else
            {
                return Vector3.SmoothDamp(current, target, ref rateOfChange, duration, maxSpeed, deltaTime);
            }
        }
    }
}
