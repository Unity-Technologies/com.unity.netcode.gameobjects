using MLAPI.NetworkingManagerComponents.Binary;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MLAPI.Data
{
    internal static class FieldTypeHelper
    {
        // Support ANY value type
        // Support arrays
        // Diff sending arrays

        internal static readonly HashSet<Type> validTypes = new HashSet<Type>(); //This is the types that are checked to not contain ref types other than arrays

        internal static bool ObjectEqual(object o1, object o2)
        {
            return o1 == o2 || (o1 != null && (o1.Equals(o2) || (o1 is Array && (o1 as Array).SequenceEquals(o2 as Array))));
        }

        /// <summary>
        /// Shallow copies everything but semi deep copies multi dimensional arrays
        /// </summary>
        /// <param name="o">The object to sheep copy</param>
        /// <returns>A Sheep copy of the object</returns>
        internal static object SheepCopy(this object o)
        {
            if (o == null) return null;
            Type type = o.GetType();
            if (type.IsArray)
            {
                Array oldArray = o as Array;
                Array array = Array.CreateInstance(o.GetType().GetElementType(), (o as Array).Length);
                for (int i = 0; i < oldArray.Length; i++) array.SetValue(oldArray.GetValue(i).SheepCopy(), i);
                return array;
            }
            else if (type.IsValueType || type == typeof(string)) return o;
            else throw new Exception("[MLAPI] Reference typed objects are not supported in SyncedVar fields");
        }

        internal static bool SequenceEquals(this Array a1, Array a2)
        {
            if ((a1 == null) != (a2 == null)) return false;
            if (a1 == null || a2 == null) return true;
            if (a1.Length != a2.Length) return false;
            bool equal = true;
            object val1;
            object val2;
            for (int i = 0; i < a1.Length; i++)
            {
                val1 = a1.GetValue(i);
                val2 = a2.GetValue(i);
                Type elementType = a1.GetType().GetElementType();
                if (elementType.IsArray && !(a1.GetValue(i) as Array).SequenceEquals((a2.GetValue(i) as Array))) equal = false;
                else if (!ObjectEqual(val1, val2)) return false;
            }
            return equal;
        }

        internal static void CheckForReferenceTypes(Type type)
        {
            if (validTypes.Contains(type)) return;
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                if (validTypes.Contains(field.FieldType)) continue;
                if (IsRefType(field.FieldType)) throw new Exception("[MLAPI] Reference typed objects are not supported in SyncedVar fields");
            }
            validTypes.Add(type);
        }

        internal static bool IsRefType(Type t)
        {
            return (!t.IsValueType && t != typeof(string)) || (t.IsArray && IsRefType(t.GetElementType()));
        }

        internal static void WriteFieldType(BitWriterDeprecated writer, object newValue)
        {
            WriteFieldType(writer, newValue, null);
        }

        internal static void WriteFieldType(BitWriterDeprecated writer, object newValue, object oldValue)
        {
            oldValue = null;
            Type newValueType = newValue.GetType();
            CheckForReferenceTypes(newValueType);

            if (newValueType.IsArray)
            {
                writer.WriteBool(oldValue == null);
                Array newArray = newValue as Array;
                if (oldValue == null) // No diff, just populate new values
                {
                    writer.WriteUShort((ushort)newArray.Length);
                    foreach (var value in newArray)
                        WriteFieldType(writer, value);
                }
                else // Send a diff
                {
                    Array oldArray = oldValue as Array;
                    bool diff = newArray.Length != oldArray.Length;
                    writer.WriteBool(diff); //Has length changed
                    if (diff) writer.WriteUShort((ushort)newArray.Length);
                    object newHolder = null, oldHolder = null;
                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (i >= oldArray.Length) // Due to an increase in the new array size, new values have to be populated past the old array's length
                        {
                            WriteFieldType(writer, newHolder);
                        }
                        else if (!ObjectEqual(newHolder = newArray.GetValue(i), oldHolder = oldArray.GetValue(i)))
                        {
                            // Notifying about a change at index 1
                            // [0, 1, 2] // New array
                            // [0, 4]    // Old array
                            writer.WriteBool(true); // An element at an index that exists on both ends of the connection has changed
                            WriteFieldType(writer, newHolder, oldHolder); // The changed index
                        }
                        else writer.WriteBool(false);
                    }
                }
            }
            else if(!newValueType.IsValueType && newValueType != typeof(string)) throw new Exception("[MLAPI] Reference typed objects are not supported in SyncedVar fields");
            else
            {
                if (newValue is bool)
                    writer.WriteBool((bool)newValue);
                else if (newValue is byte)
                    writer.WriteByte((byte)newValue);
                else if (newValue is double)
                    writer.WriteDouble((double)newValue);
                else if (newValue is float)
                    writer.WriteFloat((float)newValue);
                else if (newValue is int)
                    writer.WriteInt((int)newValue);
                else if (newValue is long)
                    writer.WriteLong((long)newValue);
                else if (newValue is sbyte)
                    writer.WriteSByte((sbyte)newValue);
                else if (newValue is short)
                    writer.WriteShort((short)newValue);
                else if (newValue is uint)
                    writer.WriteUInt((uint)newValue);
                else if (newValue is ulong)
                    writer.WriteULong((ulong)newValue);
                else if (newValue is ushort)
                    writer.WriteUShort((ushort)newValue);
                else if (newValue is string)
                    writer.WriteString((string)newValue);
                else if (newValue is Vector3 vector3)
                {
                    writer.WriteFloat(vector3.x);
                    writer.WriteFloat(vector3.y);
                    writer.WriteFloat(vector3.z);
                }
                else if (newValue is Vector2 vector2)
                {
                    writer.WriteFloat(vector2.x);
                    writer.WriteFloat(vector2.y);
                }
                else if (newValue is Quaternion)
                {
                    Vector3 euler = ((Quaternion)newValue).eulerAngles;
                    writer.WriteFloat(euler.x);
                    writer.WriteFloat(euler.y);
                    writer.WriteFloat(euler.z);
                }
                else
                {
                    BinarySerializer.Serialize(newValue, writer);
                }
            }
        }

        internal static object ReadFieldType(BitReaderDeprecated reader, Type type)
        {
            return ReadFieldType(reader, type, null);
        }

        internal static object ReadFieldType(BitReaderDeprecated reader, Type type, object oldObject)
        {
            CheckForReferenceTypes(type);
            if (type.IsArray)
            {
                bool isFlush = reader.ReadBool(); // Whether or not to ignore the possible existence of an old value
                Type elementType = type.GetElementType();
                Array oldArray = oldObject as Array;
                if (isFlush || oldObject == null) // Populating new array indices
                {
                    ushort len = reader.ReadUShort();
                    Array data = oldArray != null && oldArray.Length == len ? oldArray : Array.CreateInstance(elementType, len);
                    for (ushort i = 0; i < len; i++) data.SetValue(ReadFieldType(reader, elementType), i);
                    return data;
                }
                else // Updating by diff
                {
                    bool lenChange = reader.ReadBool();
                    ushort len = lenChange ? reader.ReadUShort() : (ushort)oldArray.Length;

                    //Debug.LogError("Me lenf' ees: "+len+" (changed? "+(lenChange?"ye":"nah")+")");

                    // What to write to
                    Array newArray = lenChange ? Array.CreateInstance(type.GetElementType(), len) : oldArray;

                    // Copy old values (TODO: Optimize)
                    // [1, 2, 3]
                    // [1, 2]
                    if (lenChange)
                        for(ushort i = (ushort)(Math.Min((ushort)oldArray.Length, len) - 1); i>=0; --i)
                            newArray.SetValue(oldArray.GetValue(i), i);

                    // Sync new values
                    for (ushort i = 0; i < len; ++i)
                    {
                        // New data is out of old bounds: directly sync new data
                        if (i >= oldArray.Length) newArray.SetValue(ReadFieldType(reader, elementType), i);

                        // Check for new data and sync data if necessary
                        else if (reader.ReadBool())
                        {
                            //Debug.LogError("Syncing data (index="+i+")");
                            newArray.SetValue(ReadFieldType(reader, elementType, oldArray.GetValue(i)), i);
                        }
                    }
                    oldObject = newArray;
                    return newArray;
                }
            }
            else if (!type.IsValueType && type != typeof(string)) throw new Exception("[MLAPI] Reference typed objects are not supported in SyncedVar fields");
            else
            {
                if (type == typeof(bool))
                    return reader.ReadBool();
                else if (type == typeof(byte))
                    return reader.ReadByte();
                else if (type == typeof(double))
                    return reader.ReadDouble();
                else if (type == typeof(float))
                    return reader.ReadFloat();
                else if (type == typeof(int))
                    return reader.ReadInt();
                else if (type == typeof(long))
                    return reader.ReadLong();
                else if (type == typeof(sbyte))
                    return reader.ReadSByte();
                else if (type == typeof(short))
                    return reader.ReadShort();
                else if (type == typeof(uint))
                    return reader.ReadUInt();
                else if (type == typeof(ulong))
                    return reader.ReadULong();
                else if (type == typeof(ushort))
                    return reader.ReadUShort();
                else if (type == typeof(string))
                    return reader.ReadString();
                else if (type == typeof(Vector3))
                {
                    return new Vector3
                    {
                        x = reader.ReadFloat(),
                        y = reader.ReadFloat(),
                        z = reader.ReadFloat()
                    };
                }
                else if (type == typeof(Vector2))
                {
                    return new Vector2()
                    {
                        x = reader.ReadFloat(),
                        y = reader.ReadFloat()
                    };
                }
                else if (type == typeof(Quaternion))
                {
                    return Quaternion.Euler(new Vector3
                    {
                        x = reader.ReadFloat(),
                        y = reader.ReadFloat(),
                        z = reader.ReadFloat()
                    });
                }
                else
                {
                    return BinarySerializer.Deserialize(reader, type);
                }
            }
        }
    }
}
