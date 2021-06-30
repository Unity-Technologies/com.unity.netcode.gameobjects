using System;

#if UNITY_2020_2_OR_NEWER
using Unity.Profiling.LowLevel;
#endif

namespace MLAPI.Profiling
{
    internal struct ProfilerCounterUtility
    {
#if UNITY_2020_2_OR_NEWER && ENABLE_PROFILER
        public static byte GetProfilerMarkerDataType<T>()
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
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
#endif
    }
}