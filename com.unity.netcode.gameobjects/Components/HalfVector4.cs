using UnityEngine;

namespace Unity.Netcode.Components
{
    internal struct HalfVector4 : INetworkSerializable
    {
        public ushort X;
        public ushort Y;
        public ushort Z;
        public ushort W;

        // Since Quaternions are normalized, we increase their half precision
        // by multiplying each value by 1000 when converting to a half float
        // and then reverting that when converting back to a full float.
        private const float k_PrecisionAdjustmentUp = 10000.0f;
        private const float k_PrecisionAdjustmentDown = 0.0001f;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref X);
            serializer.SerializeValue(ref Y);
            serializer.SerializeValue(ref Z);
            serializer.SerializeValue(ref W);
        }

        public void ToVector4(ref Vector4 halfVector4)
        {
            halfVector4.x = Mathf.HalfToFloat(X);
            halfVector4.y = Mathf.HalfToFloat(Y);
            halfVector4.z = Mathf.HalfToFloat(Z);
            halfVector4.w = Mathf.HalfToFloat(W);
        }
        public void ToQuaternion(ref Quaternion quaternion)
        {
            quaternion.x = Mathf.HalfToFloat(X) * k_PrecisionAdjustmentDown;
            quaternion.y = Mathf.HalfToFloat(Y) * k_PrecisionAdjustmentDown;
            quaternion.z = Mathf.HalfToFloat(Z) * k_PrecisionAdjustmentDown;
            quaternion.w = Mathf.HalfToFloat(W) * k_PrecisionAdjustmentDown;
        }

        public void FromVector4(ref Vector4 vector4)
        {
            X = Mathf.FloatToHalf(vector4.x);
            Y = Mathf.FloatToHalf(vector4.y);
            Z = Mathf.FloatToHalf(vector4.z);
            W = Mathf.FloatToHalf(vector4.w);
        }

        public void FromQuaternion(ref Quaternion quaternion)
        {
            X = Mathf.FloatToHalf(quaternion.x * k_PrecisionAdjustmentUp);
            Y = Mathf.FloatToHalf(quaternion.y * k_PrecisionAdjustmentUp);
            Z = Mathf.FloatToHalf(quaternion.z * k_PrecisionAdjustmentUp);
            W = Mathf.FloatToHalf(quaternion.w * k_PrecisionAdjustmentUp);
        }

        public HalfVector4(Vector4 vector4)
        {
            X = Mathf.FloatToHalf(vector4.x);
            Y = Mathf.FloatToHalf(vector4.y);
            Z = Mathf.FloatToHalf(vector4.z);
            W = Mathf.FloatToHalf(vector4.w);
        }

        public HalfVector4(float x, float y, float z, float w)
        {
            X = Mathf.FloatToHalf(x);
            Y = Mathf.FloatToHalf(y);
            Z = Mathf.FloatToHalf(z);
            W = Mathf.FloatToHalf(w);
        }
    }
}
