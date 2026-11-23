using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    internal static partial class NgoCliQueries
    {
        /// <summary>Most elements shown for a list or other collection value; the count is always the full one.</summary>
        internal const int MaxElementsShown = 32;

        /// <summary>How deep struct and class values are expanded into their fields before falling back to ToString.</summary>
        private const int k_MaxValueDepth = 2;

        private static readonly Dictionary<Type, PropertyInfo> s_ValueProperties = new Dictionary<Type, PropertyInfo>();

        /// <summary>
        /// The NetworkVariable values of each NetworkBehaviour a NetworkObject owns, as this peer sees them. Nested
        /// behaviours carry the path of their GameObject below the NetworkObject. A variable that can't be read
        /// reports why instead of failing the whole object.
        /// </summary>
        internal static List<BehaviourState> DescribeBehaviourState(NetworkManager manager, NetworkObject networkObject, string behaviourFilter)
        {
            var states = new List<BehaviourState>();
            foreach (var behaviour in OwnedBehaviours(networkObject))
            {
                var type = behaviour.GetType();
                if (!string.IsNullOrEmpty(behaviourFilter)
                    && !string.Equals(type.Name, behaviourFilter, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(type.FullName, behaviourFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var state = new BehaviourState
                {
                    Behaviour = type.Name,
                    Path = RelativePath(networkObject.transform, behaviour.transform),
                };
                foreach (var variable in behaviour.NetworkVariableFields)
                {
                    state.Variables.Add(DescribeVariable(manager, variable));
                }
                states.Add(state);
            }
            if (states.Count == 0 && !string.IsNullOrEmpty(behaviourFilter))
            {
                throw new ArgumentException($"{networkObject.name} has no NetworkBehaviour named '{behaviourFilter}'. Its behaviours are listed under behaviours in netcode.gameobjects get_object.");
            }
            return states;
        }

        /// <summary>The NetworkBehaviours NGO assigns to this NetworkObject: a nested NetworkObject owns the ones below it.</summary>
        internal static IEnumerable<NetworkBehaviour> OwnedBehaviours(NetworkObject networkObject)
        {
            foreach (var behaviour in networkObject.GetComponentsInChildren<NetworkBehaviour>(true))
            {
                if (behaviour.GetComponentInParent<NetworkObject>(true) == networkObject)
                {
                    yield return behaviour;
                }
            }
        }

        private static VariableState DescribeVariable(NetworkManager manager, NetworkVariableBase variable)
        {
            var state = new VariableState
            {
                Name = variable.Name,
                Type = FriendlyTypeName(variable.GetType()),
                ReadPerm = variable.ReadPerm,
                WritePerm = variable.WritePerm,
            };
            try
            {
                if (!manager.IsServer && !variable.CanClientRead(manager.LocalClientId))
                {
                    state.Note = $"Not sent to this client: read permission is {variable.ReadPerm}. Read it on the server or the owner.";
                    return state;
                }
                var valueProperty = GetValueProperty(variable.GetType());
                if (valueProperty != null)
                {
                    state.Value = FormatTopLevel(valueProperty.GetValue(variable), state);
                }
                else if (variable is IEnumerable collection)
                {
                    state.Value = FormatTopLevel(collection, state);
                }
                else if (GetEnumeratorMethod(variable.GetType()) is { } getEnumerator)
                {
                    // NetworkList<T> has a public GetEnumerator but doesn't implement IEnumerable.
                    state.Value = FormatTopLevel(new EnumeratorSource((IEnumerator)getEnumerator.Invoke(variable, null)), state);
                }
                else
                {
                    state.Value = variable.ToString();
                }
            }
            catch (Exception exception)
            {
                var inner = exception is TargetInvocationException { InnerException: not null } ? exception.InnerException : exception;
                state.Note = $"Couldn't read the value: {inner.GetType().Name}: {inner.Message}";
            }
            return state;
        }

        /// <summary>The public instance Value property of NetworkVariable&lt;T&gt;, AnticipatedNetworkVariable&lt;T&gt; and similar, cached per type.</summary>
        private static PropertyInfo GetValueProperty(Type variableType)
        {
            if (!s_ValueProperties.TryGetValue(variableType, out var property))
            {
                property = variableType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                if (property != null && (!property.CanRead || property.GetIndexParameters().Length > 0))
                {
                    property = null;
                }
                s_ValueProperties[variableType] = property;
            }
            return property;
        }

        private static MethodInfo GetEnumeratorMethod(Type variableType)
        {
            var method = variableType.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            return method != null && typeof(IEnumerator).IsAssignableFrom(method.ReturnType) ? method : null;
        }

        /// <summary>Lets an enumerator from a duck-typed GetEnumerator be read as a collection.</summary>
        private sealed class EnumeratorSource : IEnumerable
        {
            private readonly IEnumerator m_Enumerator;

            internal EnumeratorSource(IEnumerator enumerator)
            {
                m_Enumerator = enumerator;
            }

            public IEnumerator GetEnumerator() => m_Enumerator;
        }

        private static object FormatTopLevel(object value, VariableState state)
        {
            if (value is IEnumerable collection && !IsScalar(value))
            {
                var items = new List<object>();
                var count = 0;
                foreach (var item in collection)
                {
                    if (count < MaxElementsShown)
                    {
                        items.Add(FormatValue(item, 1));
                    }
                    count++;
                }
                state.Count = count;
                state.Truncated = count > MaxElementsShown;
                return items;
            }
            return FormatValue(value, 0);
        }

        /// <summary>
        /// Turns a NetworkVariable value into plain data the CLI can return: numbers, strings and enum names as they
        /// are, Unity vectors, quaternions and colours as arrays, references as their ids and names, collections as
        /// capped lists, and other structs and classes as their public fields, a couple of levels deep.
        /// </summary>
        internal static object FormatValue(object value, int depth)
        {
            switch (value)
            {
                case null:
                    return null;
                case string or bool or char:
                    return value;
                case Enum e:
                    return e.ToString();
                case float f:
                    return f;
                case double d:
                    return d;
                case Vector2 v:
                    return new[] { v.x, v.y };
                case Vector3 v:
                    return new[] { v.x, v.y, v.z };
                case Vector4 v:
                    return new[] { v.x, v.y, v.z, v.w };
                case Vector2Int v:
                    return new[] { v.x, v.y };
                case Vector3Int v:
                    return new[] { v.x, v.y, v.z };
                case Quaternion q:
                    return new[] { q.x, q.y, q.z, q.w };
                case Color c:
                    return new[] { c.r, c.g, c.b, c.a };
                case Color32 c:
                    return new[] { (int)c.r, c.g, c.b, c.a };
                case NetworkObjectReference reference:
                    return DescribeReference(reference);
                case NetworkBehaviourReference reference:
                    return reference.TryGet(out NetworkBehaviour referenced)
                        ? new Dictionary<string, object>
                        {
                            ["networkObjectId"] = (long)referenced.NetworkObjectId,
                            ["behaviour"] = referenced.GetType().Name,
                            ["name"] = referenced.name,
                        }
                        : null;
                case UnityEngine.Object unityObject:
                    // A destroyed object compares equal to null.
                    return unityObject ? unityObject.name : null;
            }

            var type = value.GetType();
            if (type.IsPrimitive || value is decimal)
            {
                return value;
            }
            if (IsScalar(value))
            {
                return value.ToString();
            }
            if (value is IDictionary dictionary)
            {
                var result = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (result.Count == MaxElementsShown)
                    {
                        break;
                    }
                    result[Convert.ToString(entry.Key, CultureInfo.InvariantCulture)] = depth < k_MaxValueDepth ? FormatValue(entry.Value, depth + 1) : entry.Value?.ToString();
                }
                return result;
            }
            if (value is IEnumerable enumerable)
            {
                var result = new List<object>();
                foreach (var item in enumerable)
                {
                    if (result.Count == MaxElementsShown)
                    {
                        break;
                    }
                    result.Add(depth < k_MaxValueDepth ? FormatValue(item, depth + 1) : item?.ToString());
                }
                return result;
            }
            if (depth >= k_MaxValueDepth)
            {
                return value.ToString();
            }
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            if (fields.Length == 0)
            {
                return value.ToString();
            }
            var members = new Dictionary<string, object>();
            foreach (var field in fields)
            {
                members[field.Name] = FormatValue(field.GetValue(value), depth + 1);
            }
            return members;
        }

        private static Dictionary<string, object> DescribeReference(NetworkObjectReference reference)
        {
            var result = new Dictionary<string, object> { ["networkObjectId"] = (long)reference.NetworkObjectId };
            if (reference.TryGet(out var referenced))
            {
                result["name"] = referenced.name;
            }
            return result;
        }

        /// <summary>Values best shown as their text: Unity.Collections fixed strings, and Guid-like types with no fields.</summary>
        private static bool IsScalar(object value)
        {
            var type = value.GetType();
            return (type.Namespace == "Unity.Collections" && type.Name.StartsWith("FixedString", StringComparison.Ordinal))
                || value is Guid || value is DateTime || value is TimeSpan;
        }

        /// <summary>NetworkVariable&lt;Int32&gt; rather than NetworkVariable`1.</summary>
        private static string FriendlyTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }
            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
            {
                name = name.Substring(0, tick);
            }
            var arguments = type.GetGenericArguments();
            var names = new string[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                names[i] = FriendlyTypeName(arguments[i]);
            }
            return $"{name}<{string.Join(", ", names)}>";
        }

        /// <summary>The path of <paramref name="child"/> below <paramref name="root"/>, or null when they're the same.</summary>
        private static string RelativePath(Transform root, Transform child)
        {
            if (child == root)
            {
                return null;
            }
            var parts = new List<string>();
            for (var current = child; current != null && current != root; current = current.parent)
            {
                parts.Add(current.name);
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
