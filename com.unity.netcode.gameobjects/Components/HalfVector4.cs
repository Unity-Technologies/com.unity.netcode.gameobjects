using UnityEngine;

namespace Unity.Netcode.Components
{
    internal struct HalfVector4 : INetworkSerializable
    {
        public ushort X;
        public ushort Y;
        public ushort Z;
        public ushort W;

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
            quaternion.x = Mathf.HalfToFloat(X);
            quaternion.y = Mathf.HalfToFloat(Y);
            quaternion.z = Mathf.HalfToFloat(Z);
            quaternion.w = Mathf.HalfToFloat(W);
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
            X = Mathf.FloatToHalf(quaternion.x);
            Y = Mathf.FloatToHalf(quaternion.y);
            Z = Mathf.FloatToHalf(quaternion.z);
            W = Mathf.FloatToHalf(quaternion.w);
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
