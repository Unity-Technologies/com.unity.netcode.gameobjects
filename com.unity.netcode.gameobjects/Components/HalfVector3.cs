using UnityEngine;

namespace Unity.Netcode.Components
{
    internal struct HalfVector3 : INetworkSerializable
    {
        public ushort X;
        public ushort Y;
        public ushort Z;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref X);
            serializer.SerializeValue(ref Y);
            serializer.SerializeValue(ref Z);
        }

        public float XHalf
        {
            get { return X; }
            set { X = Mathf.FloatToHalf(value); }
        }

        public float YHalf
        {
            get { return Y; }
            set { Y = Mathf.FloatToHalf(value); }
        }

        public float ZHalf
        {
            get { return Z; }
            set { Z = Mathf.FloatToHalf(value); }
        }


        public float XFloat
        {
            get { return Mathf.HalfToFloat(X); }
        }

        public float YFloat
        {
            get { return Mathf.HalfToFloat(Y); }
        }

        public float ZFloat
        {
            get { return Mathf.HalfToFloat(Z); }
        }

        public Vector3 ToVector3()
        {
            return new Vector3(XFloat, YFloat, ZFloat);
        }

        public void FromVector3(ref Vector3 vector3)
        {
            X = Mathf.FloatToHalf(vector3.x);
            Y = Mathf.FloatToHalf(vector3.y);
            Z = Mathf.FloatToHalf(vector3.z);
        }

        public HalfVector3(Vector3 vector3)
        {
            X = Mathf.FloatToHalf(vector3.x);
            Y = Mathf.FloatToHalf(vector3.y);
            Z = Mathf.FloatToHalf(vector3.z);
        }

        public HalfVector3(float x, float y, float z)
        {
            X = Mathf.FloatToHalf(x);
            Y = Mathf.FloatToHalf(y);
            Z = Mathf.FloatToHalf(z);
        }
    }
}
