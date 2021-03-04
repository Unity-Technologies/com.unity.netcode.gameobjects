using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_2020_2_OR_NEWER
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
#endif

namespace MLAPI.Profiling
{
#if ENABLE_PROFILER
    [StructLayout(LayoutKind.Sequential)]
#else
    [StructLayout(LayoutKind.Sequential, Size = 0)]
#endif
    internal readonly struct ProfilerCounterValue<T> where T : unmanaged
    {
#if UNITY_2020_2_OR_NEWER
#if ENABLE_PROFILER
        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        readonly unsafe T* m_Value;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProfilerCounterValue(ProfilerCategory category, string name, ProfilerMarkerDataUnit dataUnit, ProfilerCounterOptions counterOptions)
        {
#if ENABLE_PROFILER
            byte dataType = ProfilerCounterUtility.GetProfilerMarkerDataType<T>();
            unsafe
            {
                m_Value = (T*)ProfilerUnsafeUtility.CreateCounterValue(out var counterPtr, name, category, MarkerFlags.Default, dataType, (byte)dataUnit, UnsafeUtility.SizeOf<T>(), counterOptions);
            }
#endif
        }

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_PROFILER
                unsafe
                {
                    return *m_Value;
                }
#else
                return default;
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if ENABLE_PROFILER
                unsafe
                {
                    *m_Value = value;
                }
#endif
            }
        }
#endif
    }
}