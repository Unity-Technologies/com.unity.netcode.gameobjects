using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Netcode
{
    internal static class CollectionSerializationUtility
    {
        public static void WriteNativeArrayDelta<T>(FastBufferWriter writer, ref NativeArray<T> value, ref NativeArray<T> previousValue) where T : unmanaged
        {
            using var changes = new ResizableBitVector(Allocator.Temp);
            int minLength = math.min(value.Length, previousValue.Length);
            var numChanges = 0;
            for (var i = 0; i < minLength; ++i)
            {
                var val = value[i];
                var prevVal = previousValue[i];
                if (!NetworkVariableSerialization<T>.AreEqual(ref val, ref prevVal))
                {
                    ++numChanges;
                    changes.Set(i);
                }
            }

            for (var i = previousValue.Length; i < value.Length; ++i)
            {
                ++numChanges;
                changes.Set(i);
            }

            if (changes.GetSerializedSize() + FastBufferWriter.GetWriteSize<T>() * numChanges > FastBufferWriter.GetWriteSize<T>() * value.Length)
            {
                writer.WriteByteSafe(1);
                writer.WriteValueSafe(value);
                return;
            }
            writer.WriteByte(0);
            BytePacker.WriteValuePacked(writer, value.Length);
            writer.WriteValueSafe(changes);
            unsafe
            {
                var ptr = (T*)value.GetUnsafePtr();
                var prevPtr = (T*)previousValue.GetUnsafePtr();
                for (int i = 0; i < value.Length; ++i)
                {
                    if (changes.IsSet(i))
                    {
                        if (i < previousValue.Length)
                        {
                            NetworkVariableSerialization<T>.WriteDelta(writer, ref ptr[i], ref prevPtr[i]);
                        }
                        else
                        {
                            NetworkVariableSerialization<T>.Write(writer, ref ptr[i]);
                        }
                    }
                }
            }
        }
        public static void ReadNativeArrayDelta<T>(FastBufferReader reader, ref NativeArray<T> value) where T : unmanaged
        {
            reader.ReadByteSafe(out byte full);
            if (full == 1)
            {
                value.Dispose();
                reader.ReadValueSafe(out value, Allocator.Persistent);
                return;
            }
            ByteUnpacker.ReadValuePacked(reader, out int length);
            var changes = new ResizableBitVector(Allocator.Temp);
            using var toDispose = changes;
            {
                reader.ReadNetworkSerializableInPlace(ref changes);

                var previousLength = value.Length;
                if (length != value.Length)
                {
                    var newArray = new NativeArray<T>(length, Allocator.Persistent);
                    unsafe
                    {
                        UnsafeUtility.MemCpy(newArray.GetUnsafePtr(), value.GetUnsafePtr(), math.min(newArray.Length * sizeof(T), value.Length * sizeof(T)));
                    }
                    value.Dispose();
                    value = newArray;
                }

                unsafe
                {
                    var ptr = (T*)value.GetUnsafePtr();
                    for (var i = 0; i < value.Length; ++i)
                    {
                        if (changes.IsSet(i))
                        {
                            if (i < previousLength)
                            {
                                NetworkVariableSerialization<T>.ReadDelta(reader, ref ptr[i]);
                            }
                            else
                            {
                                NetworkVariableSerialization<T>.Read(reader, ref ptr[i]);
                            }
                        }
                    }
                }
            }
        }
        public static void WriteListDelta<T>(FastBufferWriter writer, ref List<T> value, ref List<T> previousValue)
        {
            if (value == null || previousValue == null)
            {
                writer.WriteByteSafe(1);
                NetworkVariableSerialization<List<T>>.Write(writer, ref value);
                return;
            }
            using var changes = new ResizableBitVector(Allocator.Temp);
            int minLength = math.min(value.Count, previousValue.Count);
            var numChanges = 0;
            for (var i = 0; i < minLength; ++i)
            {
                var val = value[i];
                var prevVal = previousValue[i];
                if (!NetworkVariableSerialization<T>.AreEqual(ref val, ref prevVal))
                {
                    ++numChanges;
                    changes.Set(i);
                }
            }

            for (var i = previousValue.Count; i < value.Count; ++i)
            {
                ++numChanges;
                changes.Set(i);
            }

            if (numChanges >= value.Count * 0.9)
            {
                writer.WriteByteSafe(1);
                NetworkVariableSerialization<List<T>>.Write(writer, ref value);
                return;
            }

            writer.WriteByteSafe(0);
            BytePacker.WriteValuePacked(writer, value.Count);
            writer.WriteValueSafe(changes);
            for (int i = 0; i < value.Count; ++i)
            {
                if (changes.IsSet(i))
                {
                    var reffable = value[i];
                    if (i < previousValue.Count)
                    {
                        var prevReffable = previousValue[i];
                        NetworkVariableSerialization<T>.WriteDelta(writer, ref reffable, ref prevReffable);
                    }
                    else
                    {
                        NetworkVariableSerialization<T>.Write(writer, ref reffable);
                    }
                }
            }
        }
        public static void ReadListDelta<T>(FastBufferReader reader, ref List<T> value)
        {
            reader.ReadByteSafe(out byte full);
            if (full == 1)
            {
                NetworkVariableSerialization<List<T>>.Read(reader, ref value);
                return;
            }
            ByteUnpacker.ReadValuePacked(reader, out int length);
            var changes = new ResizableBitVector(Allocator.Temp);
            using var toDispose = changes;
            {
                reader.ReadNetworkSerializableInPlace(ref changes);

                if (length < value.Count)
                {
                    value.RemoveRange(length, value.Count - length);
                }

                for (var i = 0; i < length; ++i)
                {
                    if (changes.IsSet(i))
                    {
                        if (i < value.Count)
                        {
                            T item = value[i];
                            NetworkVariableSerialization<T>.ReadDelta(reader, ref item);
                            value[i] = item;
                        }
                        else
                        {
                            T item = default;
                            NetworkVariableSerialization<T>.Read(reader, ref item);
                            value.Add(item);
                        }
                    }
                }
            }
        }

        private static class ListCache<T>
        {
            private static List<T> s_AddedList = new List<T>();
            private static List<T> s_RemovedList = new List<T>();
            private static List<T> s_ChangedList = new List<T>();

            public static List<T> GetAddedList()
            {
                s_AddedList.Clear();
                return s_AddedList;
            }
            public static List<T> GetRemovedList()
            {
                s_RemovedList.Clear();
                return s_RemovedList;
            }
            public static List<T> GetChangedList()
            {
                s_ChangedList.Clear();
                return s_ChangedList;
            }
        }

        public static void WriteHashSetDelta<T>(FastBufferWriter writer, ref HashSet<T> value, ref HashSet<T> previousValue) where T : IEquatable<T>
        {
            if (value == null || previousValue == null)
            {
                writer.WriteByteSafe(1);
                NetworkVariableSerialization<HashSet<T>>.Write(writer, ref value);
                return;
            }
            var added = ListCache<T>.GetAddedList();
            var removed = ListCache<T>.GetRemovedList();
            foreach (var item in value)
            {
                if (!previousValue.Contains(item))
                {
                    added.Add(item);
                }
            }

            foreach (var item in previousValue)
            {
                if (!value.Contains(item))
                {
                    removed.Add(item);
                }
            }

            if (added.Count + removed.Count >= value.Count)
            {
                writer.WriteByteSafe(1);
                NetworkVariableSerialization<HashSet<T>>.Write(writer, ref value);
                return;
            }

            writer.WriteByteSafe(0);
            writer.WriteValueSafe(added.Count);
            for (var i = 0; i < added.Count; ++i)
            {
                var item = added[i];
                NetworkVariableSerialization<T>.Write(writer, ref item);
            }
            writer.WriteValueSafe(removed.Count);
            for (var i = 0; i < removed.Count; ++i)
            {
                var item = removed[i];
                NetworkVariableSerialization<T>.Write(writer, ref item);
            }
        }

        public static void ReadHashSetDelta<T>(FastBufferReader reader, ref HashSet<T> value) where T : IEquatable<T>
        {
            reader.ReadByteSafe(out byte full);
            if (full != 0)
            {
                NetworkVariableSerialization<HashSet<T>>.Read(reader, ref value);
                return;
            }
            reader.ReadValueSafe(out int addedCount);
            for (var i = 0; i < addedCount; ++i)
            {
                T item = default;
                NetworkVariableSerialization<T>.Read(reader, ref item);
                value.Add(item);
            }
            reader.ReadValueSafe(out int removedCount);
            for (var i = 0; i < removedCount; ++i)
            {
                T item = default;
                NetworkVariableSerialization<T>.Read(reader, ref item);
                value.Remove(item);
            }
        }
        public static void WriteDictionaryDelta<TKey, TVal>(FastBufferWriter writer, ref Dictionary<TKey, TVal> value, ref Dictionary<TKey, TVal> previousValue)
            where TKey : IEquatable<TKey>
        {
            if (value == null || previousValue == null)
            {
                writer.WriteByteSafe(1);
                NetworkVariableSerialization<Dictionary<TKey, TVal>>.Write(writer, ref value);
                return;
            }
            var added = ListCache<KeyValuePair<TKey, TVal>>.GetAddedList();
            var changed = ListCache<KeyValuePair<TKey, TVal>>.GetRemovedList();
            var removed = ListCache<KeyValuePair<TKey, TVal>>.GetChangedList();
            foreach (var item in value)
            {
                var val = item.Value;
                var hasPrevVal = previousValue.TryGetValue(item.Key, out var prevVal);
                if (!hasPrevVal)
                {
                    added.Add(item);
                }
                else if (!NetworkVariableSerialization<TVal>.AreEqual(ref val, ref prevVal))
                {
                    changed.Add(item);
                }
            }

            foreach (var item in previousValue)
            {
                if (!value.ContainsKey(item.Key))
                {
                    removed.Add(item);
                }
            }

            if (added.Count + removed.Count + changed.Count >= value.Count)
            {
                writer.WriteByteSafe(1);
                NetworkVariableSerialization<Dictionary<TKey, TVal>>.Write(writer, ref value);
                return;
            }

            writer.WriteByteSafe(0);
            writer.WriteValueSafe(added.Count);
            for (var i = 0; i < added.Count; ++i)
            {
                (var key, var val) = (added[i].Key, added[i].Value);
                NetworkVariableSerialization<TKey>.Write(writer, ref key);
                NetworkVariableSerialization<TVal>.Write(writer, ref val);
            }
            writer.WriteValueSafe(removed.Count);
            for (var i = 0; i < removed.Count; ++i)
            {
                var key = removed[i].Key;
                NetworkVariableSerialization<TKey>.Write(writer, ref key);
            }
            writer.WriteValueSafe(changed.Count);
            for (var i = 0; i < changed.Count; ++i)
            {
                (var key, var val) = (changed[i].Key, changed[i].Value);
                NetworkVariableSerialization<TKey>.Write(writer, ref key);
                NetworkVariableSerialization<TVal>.Write(writer, ref val);
            }
        }

        public static void ReadDictionaryDelta<TKey, TVal>(FastBufferReader reader, ref Dictionary<TKey, TVal> value)
            where TKey : IEquatable<TKey>
        {
            reader.ReadByteSafe(out byte full);
            if (full != 0)
            {
                NetworkVariableSerialization<Dictionary<TKey, TVal>>.Read(reader, ref value);
                return;
            }
            // Added
            reader.ReadValueSafe(out int length);
            for (var i = 0; i < length; ++i)
            {
                (TKey key, TVal val) = (default, default);
                NetworkVariableSerialization<TKey>.Read(reader, ref key);
                NetworkVariableSerialization<TVal>.Read(reader, ref val);
                value.Add(key, val);
            }
            // Removed
            reader.ReadValueSafe(out length);
            for (var i = 0; i < length; ++i)
            {
                TKey key = default;
                NetworkVariableSerialization<TKey>.Read(reader, ref key);
                value.Remove(key);
            }
            // Changed
            reader.ReadValueSafe(out length);
            for (var i = 0; i < length; ++i)
            {
                (TKey key, TVal val) = (default, default);
                NetworkVariableSerialization<TKey>.Read(reader, ref key);
                NetworkVariableSerialization<TVal>.Read(reader, ref val);
                value[key] = val;
            }
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public static void WriteNativeListDelta<T>(FastBufferWriter writer, ref NativeList<T> value, ref NativeList<T> previousValue) where T : unmanaged
        {
            using var changes = new ResizableBitVector(Allocator.Temp);
            int minLength = math.min(value.Length, previousValue.Length);
            var numChanges = 0;
            for (var i = 0; i < minLength; ++i)
            {
                var val = value[i];
                var prevVal = previousValue[i];
                if (!NetworkVariableSerialization<T>.AreEqual(ref val, ref prevVal))
                {
                    ++numChanges;
                    changes.Set(i);
                }
            }

            for (var i = previousValue.Length; i < value.Length; ++i)
            {
                ++numChanges;
                changes.Set(i);
            }

            if (changes.GetSerializedSize() + FastBufferWriter.GetWriteSize<T>() * numChanges > FastBufferWriter.GetWriteSize<T>() * value.Length)
            {
                writer.WriteByteSafe(1);
                writer.WriteValueSafe(value);
                return;
            }

            writer.WriteByte(0);
            BytePacker.WriteValuePacked(writer, value.Length);
            writer.WriteValueSafe(changes);
            unsafe
            {
                var ptr = (T*)value.GetUnsafePtr();
                var prevPtr = (T*)previousValue.GetUnsafePtr();
                for (int i = 0; i < value.Length; ++i)
                {
                    if (changes.IsSet(i))
                    {
                        if (i < previousValue.Length)
                        {
                            NetworkVariableSerialization<T>.WriteDelta(writer, ref ptr[i], ref prevPtr[i]);
                        }
                        else
                        {
                            NetworkVariableSerialization<T>.Write(writer, ref ptr[i]);
                        }
                    }
                }
            }
        }
        public static void ReadNativeListDelta<T>(FastBufferReader reader, ref NativeList<T> value) where T : unmanaged
        {
            reader.ReadByteSafe(out byte full);
            if (full == 1)
            {
                reader.ReadValueSafeInPlace(ref value);
                return;
            }
            ByteUnpacker.ReadValuePacked(reader, out int length);
            var changes = new ResizableBitVector(Allocator.Temp);
            using var toDispose = changes;
            {
                reader.ReadNetworkSerializableInPlace(ref changes);

                var previousLength = value.Length;
                if (length != value.Length)
                {
                    value.Resize(length, NativeArrayOptions.UninitializedMemory);
                }

                unsafe
                {
                    var ptr = (T*)value.GetUnsafePtr();
                    for (var i = 0; i < value.Length; ++i)
                    {
                        if (changes.IsSet(i))
                        {
                            if (i < previousLength)
                            {
                                NetworkVariableSerialization<T>.ReadDelta(reader, ref ptr[i]);
                            }
                            else
                            {
                                NetworkVariableSerialization<T>.Read(reader, ref ptr[i]);
                            }
                        }
                    }
                }
            }
        }

        public static unsafe void WriteNativeHashSetDelta<T>(FastBufferWriter writer, ref NativeHashSet<T> value, ref NativeHashSet<T> previousValue) where T : unmanaged, IEquatable<T>
        {
            var added = stackalloc T[value.Count()];
            var removed = stackalloc T[previousValue.Count()];
            var addedCount = 0;
            var removedCount = 0;
            foreach (var item in value)
            {
                if (!previousValue.Contains(item))
                {
                    added[addedCount] = item;
                    ++addedCount;
                }
            }

            foreach (var item in previousValue)
            {
                if (!value.Contains(item))
                {
                    removed[removedCount] = item;
                    ++removedCount;
                }
            }

            if (addedCount + removedCount >= value.Count())
            {
                writer.WriteByteSafe(1);
                writer.WriteValueSafe(value);
                return;
            }

            writer.WriteByteSafe(0);
            writer.WriteValueSafe(addedCount);
            for (var i = 0; i < addedCount; ++i)
            {
                NetworkVariableSerialization<T>.Write(writer, ref added[i]);
            }
            writer.WriteValueSafe(removedCount);
            for (var i = 0; i < removedCount; ++i)
            {
                NetworkVariableSerialization<T>.Write(writer, ref removed[i]);
            }
        }

        public static void ReadNativeHashSetDelta<T>(FastBufferReader reader, ref NativeHashSet<T> value) where T : unmanaged, IEquatable<T>
        {
            reader.ReadByteSafe(out byte full);
            if (full != 0)
            {
                reader.ReadValueSafeInPlace(ref value);
                return;
            }
            reader.ReadValueSafe(out int addedCount);
            for (var i = 0; i < addedCount; ++i)
            {
                T item = default;
                NetworkVariableSerialization<T>.Read(reader, ref item);
                value.Add(item);
            }
            reader.ReadValueSafe(out int removedCount);
            for (var i = 0; i < removedCount; ++i)
            {
                T item = default;
                NetworkVariableSerialization<T>.Read(reader, ref item);
                value.Remove(item);
            }
        }

        public static unsafe void WriteNativeHashMapDelta<TKey, TVal>(FastBufferWriter writer, ref NativeHashMap<TKey, TVal> value, ref NativeHashMap<TKey, TVal> previousValue)
            where TKey : unmanaged, IEquatable<TKey>
            where TVal : unmanaged
        {
            var added = stackalloc KeyValue<TKey, TVal>[value.Count()];
            var changed = stackalloc KeyValue<TKey, TVal>[value.Count()];
            var removed = stackalloc KeyValue<TKey, TVal>[previousValue.Count()];
            var addedCount = 0;
            var changedCount = 0;
            var removedCount = 0;
            foreach (var item in value)
            {
                var hasPrevVal = previousValue.TryGetValue(item.Key, out var prevVal);
                if (!hasPrevVal)
                {
                    added[addedCount] = item;
                    ++addedCount;
                }
                else if (!NetworkVariableSerialization<TVal>.AreEqual(ref item.Value, ref prevVal))
                {
                    changed[changedCount] = item;
                    ++changedCount;
                }
            }

            foreach (var item in previousValue)
            {
                if (!value.ContainsKey(item.Key))
                {
                    removed[removedCount] = item;
                    ++removedCount;
                }
            }

            if (addedCount + removedCount + changedCount >= value.Count())
            {
                writer.WriteByteSafe(1);
                writer.WriteValueSafe(value);
                return;
            }

            writer.WriteByteSafe(0);
            writer.WriteValueSafe(addedCount);
            for (var i = 0; i < addedCount; ++i)
            {
                (var key, var val) = (added[i].Key, added[i].Value);
                NetworkVariableSerialization<TKey>.Write(writer, ref key);
                NetworkVariableSerialization<TVal>.Write(writer, ref val);
            }
            writer.WriteValueSafe(removedCount);
            for (var i = 0; i < removedCount; ++i)
            {
                var key = removed[i].Key;
                NetworkVariableSerialization<TKey>.Write(writer, ref key);
            }
            writer.WriteValueSafe(changedCount);
            for (var i = 0; i < changedCount; ++i)
            {
                (var key, var val) = (changed[i].Key, changed[i].Value);
                NetworkVariableSerialization<TKey>.Write(writer, ref key);
                NetworkVariableSerialization<TVal>.Write(writer, ref val);
            }
        }

        public static void ReadNativeHashMapDelta<TKey, TVal>(FastBufferReader reader, ref NativeHashMap<TKey, TVal> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TVal : unmanaged
        {
            reader.ReadByteSafe(out byte full);
            if (full != 0)
            {
                reader.ReadValueSafeInPlace(ref value);
                return;
            }
            // Added
            reader.ReadValueSafe(out int length);
            for (var i = 0; i < length; ++i)
            {
                (TKey key, TVal val) = (default, default);
                NetworkVariableSerialization<TKey>.Read(reader, ref key);
                NetworkVariableSerialization<TVal>.Read(reader, ref val);
                value.Add(key, val);
            }
            // Removed
            reader.ReadValueSafe(out length);
            for (var i = 0; i < length; ++i)
            {
                TKey key = default;
                NetworkVariableSerialization<TKey>.Read(reader, ref key);
                value.Remove(key);
            }
            // Changed
            reader.ReadValueSafe(out length);
            for (var i = 0; i < length; ++i)
            {
                (TKey key, TVal val) = (default, default);
                NetworkVariableSerialization<TKey>.Read(reader, ref key);
                NetworkVariableSerialization<TVal>.Read(reader, ref val);
                value[key] = val;
            }
        }
#endif
    }
}
