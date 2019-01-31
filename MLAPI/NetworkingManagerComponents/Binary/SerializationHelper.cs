using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MLAPI.Internal;
using UnityEngine;

namespace MLAPI.Serialization
{
    /// <summary>
    /// Helper class providing helper serialization methods
    /// </summary>
    public static class SerializationHelper
    {
        private static readonly Dictionary<Type, FieldInfo[]> fieldCache = new Dictionary<Type, FieldInfo[]>();

        internal static FieldInfo[] GetFieldsForType(Type type)
        {
            if (fieldCache.ContainsKey(type))
                return fieldCache[type];
            else
            {
                FieldInfo[] fields = type
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => (x.IsPublic || x.GetCustomAttributes(typeof(SerializeField), true).Length > 0) && IsTypeSupported(x.FieldType))
                    .OrderBy(x => x.Name).ToArray();
                
                fieldCache.Add(type, fields);

                return fields;
            }
        }

        private static readonly HashSet<Type> SupportedTypes = new HashSet<Type>()
        {
            typeof(byte),
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(short),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(bool),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Color),
            typeof(Color32),
            typeof(Ray),
            typeof(Quaternion),
            typeof(char),
            typeof(GameObject),
            typeof(NetworkedObject),
            typeof(NetworkedBehaviour)
        };

        /// <summary>
        /// Returns if a type is supported for serialization
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>Whether or not the type is supported</returns>
        public static bool IsTypeSupported(Type type)
        {
            return type.IsEnum || SupportedTypes.Contains(type) || type.HasInterface(typeof(IBitWritable));
        }
    }
}