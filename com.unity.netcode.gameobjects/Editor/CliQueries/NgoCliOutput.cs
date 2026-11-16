using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    /// <summary>
    /// Turns the CLI result models into plain dictionaries and lists with camelCase keys, the shape pipeline
    /// commands return. This keeps the models in Unity naming style and the package free of a JSON library
    /// dependency. Enums become their names. Null fields are kept as explicit nulls, so a value that isn't
    /// available reads differently from a misspelled field.
    /// </summary>
    internal static class NgoCliOutput
    {
        internal static object ToWire(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string or bool or char:
                    return value;
                case Enum e:
                    return e.ToString();
                case IDictionary dictionary:
                    {
                        var result = new Dictionary<string, object>(dictionary.Count);
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            result[Convert.ToString(entry.Key, CultureInfo.InvariantCulture)] = ToWire(entry.Value);
                        }
                        return result;
                    }
                case IEnumerable enumerable:
                    {
                        var result = new List<object>();
                        foreach (var item in enumerable)
                        {
                            result.Add(ToWire(item));
                        }
                        return result;
                    }
            }

            var type = value.GetType();
            if (type.IsPrimitive || value is decimal)
            {
                return value;
            }

            var fields = new Dictionary<string, object>();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                fields[ToCamelCase(field.Name)] = ToWire(field.GetValue(value));
            }
            return fields;
        }

        internal static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            {
                return name;
            }
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
