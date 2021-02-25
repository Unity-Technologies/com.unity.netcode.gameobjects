using System;
using Unity.Profiling.LowLevel;

namespace ProfilerModuleOverride
{
    struct ProfilerUtility
    {
        public static byte GetProfilerMarkerDataType<T>()
        {
            switch (Type.GetTypeCode(typeof(T))) {
                case TypeCode.Int32:
                    return (byte)ProfilerMarkerDataType.Int32;
                case TypeCode.UInt32:
                    return (byte)ProfilerMarkerDataType.UInt32;
                case TypeCode.Int64:
                    return (byte)ProfilerMarkerDataType.Int64;
                case TypeCode.UInt64:
                    return (byte)ProfilerMarkerDataType.UInt64;
                case TypeCode.Single:
                    return (byte)ProfilerMarkerDataType.Float;
                case TypeCode.Double:
                    return (byte)ProfilerMarkerDataType.Double;
                case TypeCode.String:
                    return (byte)ProfilerMarkerDataType.String16;
                default:
                    throw new ArgumentException($"Type {typeof(T)} is unsupported by ProfilerCounter.");
            }
        }
    }
}


