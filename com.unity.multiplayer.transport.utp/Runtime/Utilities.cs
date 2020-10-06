using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Assets.Scripts.Transport
{
    public static class Utilities
    {
        unsafe static byte[] SerializeUnmanagedArray<T>(NativeArray<T> value) where T : unmanaged
        {
            var bytes = new byte[UnsafeUtility.SizeOf<T>() * value.Length + sizeof(int)];
            fixed (byte* ptr = bytes) {
                var buf = new UnsafeAppendBuffer(ptr, bytes.Length);
                buf.Add(value);
            }

            return bytes;
        }

        unsafe static NativeArray<T> DeserializeUnmanagedArray<T>(byte[] buffer, Allocator allocator = Allocator.Temp) where T : unmanaged
        {
            fixed (byte* ptr = buffer) {
                var buf = new UnsafeAppendBuffer.Reader(ptr, buffer.Length);
                buf.ReadNext<T>(out var array, allocator);
                return array;
            }
        }

        public unsafe static byte[] SerializeUnmanaged<T>(ref T value) where T : unmanaged
        {
            var bytes = new byte[UnsafeUtility.SizeOf<T>()];
            fixed (byte* ptr = bytes) {
                UnsafeUtility.CopyStructureToPtr(ref value, ptr);
            }

            return bytes;
        }

        public unsafe static T DeserializeUnmanaged<T>(byte[] buffer) where T : unmanaged
        {
            fixed (byte* ptr = buffer) {
                UnsafeUtility.CopyPtrToStructure<T>(ptr, out var value);
                return value;
            }
        }

        public unsafe static T DeserializeUnmanaged<T>(ref NativeSlice<byte> buffer) where T : unmanaged
        {
            int structSize = UnsafeUtility.SizeOf<T>();
            long ptr = (long)buffer.GetUnsafePtr();
            long size = buffer.Length;

            long addr = ptr + size - structSize;

            var data = UnsafeUtility.ReadArrayElement<T>((void*)addr, 0);

            return data;
        }
    }
}
