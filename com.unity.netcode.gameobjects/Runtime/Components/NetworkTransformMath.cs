using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Burst compatible replacement methods for non-burst compatible math methods that <see cref="NetworkTransform"/> uses.
    /// </summary>
    /// <remarks>
    /// <c>NetworkTransformMathTests</c> measures each method against the non-burst compatible version that it replaces.
    /// </remarks>
    internal static class NetworkTransformMath
    {
        internal const float Rad2Deg = 360f / (math.PI * 2f);
        internal const float Deg2Rad = (math.PI * 2f) / 360f;

        /// <summary>
        /// <see cref="Mathf.Repeat(float, float)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Repeat(float t, float length)
        {
            return math.clamp(t - math.floor(t / length) * length, 0.0f, length);
        }

        /// <summary>
        /// <see cref="Mathf.DeltaAngle(float, float)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float DeltaAngle(float current, float target)
        {
            var delta = Repeat(target - current, 360.0f);
            if (delta > 180.0f)
            {
                delta -= 360.0f;
            }
            return delta;
        }

        /// <summary>
        /// The burst compatible version of <see cref="Vector3.Lerp(Vector3, Vector3, float)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 Lerp(float3 start, float3 end, float time)
        {
            // Written per component in the same form the engine uses so the rounding matches.
            time = math.clamp(time, 0.0f, 1.0f);
            return new float3(
                start.x + (end.x - start.x) * time,
                start.y + (end.y - start.y) * time,
                start.z + (end.z - start.z) * time);
        }

        /// <summary>
        /// Brings each euler angle into the 0 to 360 range, matching what the engine returns.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float NormalizeAngle(float angle)
        {
            // Written as a loop free expression so it stays branch predictable and Burst friendly.
            var normalized = Repeat(angle, 360.0f);
            return normalized;
        }

        /// <summary>
        /// The burst compatible version of <see cref="Quaternion.eulerAngles"/>.
        /// </summary>
        /// <remarks>
        /// Extracted in ZXY order to match <see cref="Quaternion.Euler(Vector3)"/>, which applies Z, then X,
        /// then Y. For a rotation matrix R = Ry * Rx * Rz that gives sin(x) = -m12, y = atan2(m02, m22) and
        /// z = atan2(m10, m11), written below directly in terms of the quaternion components.
        /// </remarks>
        internal static float3 EulerAngles(quaternion rotation)
        {
            var q = rotation.value;
            var sqx = q.x * q.x;
            var sqy = q.y * q.y;
            var sqz = q.z * q.z;

            var m10 = 2.0f * (q.x * q.y + q.w * q.z);
            var m11 = 1.0f - 2.0f * (sqx + sqz);
            var sinX = 2.0f * (q.x * q.w - q.y * q.z);

            float3 result;
            result.x = math.atan2(sinX, math.sqrt(m10 * m10 + m11 * m11));
            result.y = math.atan2(2.0f * (q.x * q.z + q.w * q.y), 1.0f - 2.0f * (sqx + sqy));
            result.z = math.atan2(m10, m11);

            result *= Rad2Deg;
            result.x = NormalizeAngle(result.x);
            result.y = NormalizeAngle(result.y);
            result.z = NormalizeAngle(result.z);
            return result;
        }

        /// <summary>
        /// The burst compatible version of <see cref="Quaternion.Euler(float, float, float)"/> with the
        /// exception that it takes a <see cref="float3"/> for all axis.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion Euler(float3 eulerDegrees)
        {
            // The engine applies the rotations in Z, X, then Y order.
            return quaternion.EulerZXY(eulerDegrees * Deg2Rad);
        }

        /// <summary>
        /// The burst compatible version of <see cref="Quaternion.Slerp(Quaternion, Quaternion, float)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion Slerp(quaternion start, quaternion end, float time)
        {
            return math.slerp(start, end, math.clamp(time, 0.0f, 1.0f));
        }

        /// <summary>
        /// The burst compatible version of <see cref="Quaternion.Lerp(Quaternion, Quaternion, float)"/>, which normalizes its result.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion Nlerp(quaternion start, quaternion end, float time)
        {
            return math.nlerp(start, end, math.clamp(time, 0.0f, 1.0f));
        }

        /// <summary>
        /// The burst compatible version of <see cref="Vector3.Slerp(Vector3, Vector3, float)"/>, which interpolates both direction and
        /// magnitude.
        /// </summary>
        internal static float3 Slerp(float3 start, float3 end, float time)
        {
            time = math.clamp(time, 0.0f, 1.0f);

            var startMagnitude = math.length(start);
            var endMagnitude = math.length(end);

            // With a zero length input there is no direction to rotate through, so this degenerates to a lerp.
            if (startMagnitude < math.EPSILON || endMagnitude < math.EPSILON)
            {
                return math.lerp(start, end, time);
            }

            var startDirection = start / startMagnitude;
            var endDirection = end / endMagnitude;
            var magnitude = math.lerp(startMagnitude, endMagnitude, time);

            var dot = math.clamp(math.dot(startDirection, endDirection), -1.0f, 1.0f);
            var angle = math.acos(dot);
            var sinAngle = math.sin(angle);

            // Both ends of the range make sinAngle approach zero, which the division below cannot survive.
            // Nearly parallel is safe to lerp through. Nearly antiparallel has no defined rotation plane at
            // all, so any implementation has to pick one; that case is expected to differ from the engine.
            if (sinAngle < 0.001f)
            {
                return math.normalizesafe(math.lerp(startDirection, endDirection, time), startDirection) * magnitude;
            }

            var direction = (math.sin((1.0f - time) * angle) * startDirection + math.sin(time * angle) * endDirection) / sinAngle;
            return direction * magnitude;
        }

        /// <summary>
        /// The burst compatible version of <see cref="Vector3.SmoothDamp(Vector3, Vector3, ref Vector3, float, float, float)"/>.
        /// </summary>
        /// <remarks>
        /// A direct port of the engine's managed implementation.
        /// </remarks>
        internal static float3 SmoothDamp(float3 current, float3 target, ref float3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = math.max(0.0001f, smoothTime);
            var omega = 2.0f / smoothTime;

            var x = omega * deltaTime;
            var exp = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);

            var changeX = current.x - target.x;
            var changeY = current.y - target.y;
            var changeZ = current.z - target.z;
            var originalTo = target;

            // Clamp the maximum speed. The engine takes this square root in double precision, which is
            // observable in the result, so it is taken the same way here.
            var maxChange = maxSpeed * smoothTime;
            var maxChangeSq = maxChange * maxChange;
            var sqrMagnitude = changeX * changeX + changeY * changeY + changeZ * changeZ;
            if (sqrMagnitude > maxChangeSq)
            {
                var magnitude = (float)math.sqrt((double)sqrMagnitude);
                changeX = changeX / magnitude * maxChange;
                changeY = changeY / magnitude * maxChange;
                changeZ = changeZ / magnitude * maxChange;
            }

            var targetX = current.x - changeX;
            var targetY = current.y - changeY;
            var targetZ = current.z - changeZ;

            var tempX = (currentVelocity.x + omega * changeX) * deltaTime;
            var tempY = (currentVelocity.y + omega * changeY) * deltaTime;
            var tempZ = (currentVelocity.z + omega * changeZ) * deltaTime;

            currentVelocity.x = (currentVelocity.x - omega * tempX) * exp;
            currentVelocity.y = (currentVelocity.y - omega * tempY) * exp;
            currentVelocity.z = (currentVelocity.z - omega * tempZ) * exp;

            var output = new float3(
                targetX + (changeX + tempX) * exp,
                targetY + (changeY + tempY) * exp,
                targetZ + (changeZ + tempZ) * exp);

            // Prevent overshooting.
            var originalMinusCurrent = originalTo - current;
            var outputMinusOriginal = output - originalTo;
            if (math.dot(originalMinusCurrent, outputMinusOriginal) > 0.0f)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }
            return output;
        }

        /// <summary>
        /// The burst compatible version of <see cref="Mathf.SmoothDampAngle(float, float, ref float, float, float, float)"/>.
        /// </summary>
        /// <remarks>
        /// A direct port of the engine's managed implementation.
        /// </remarks>
        internal static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            target = current + DeltaAngle(current, target);

            smoothTime = math.max(0.0001f, smoothTime);
            var omega = 2.0f / smoothTime;

            var x = omega * deltaTime;
            var exp = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);

            var change = current - target;
            var originalTo = target;

            var maxChange = maxSpeed * smoothTime;
            change = math.clamp(change, -maxChange, maxChange);
            target = current - change;

            var temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            var output = target + (change + temp) * exp;

            if (originalTo - current > 0.0f == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }
            return output;
        }
    }
}
