using System;
using System.Collections.Generic;
using System.Linq;

namespace MLAPI.Logging
{
    public class Context
    {
        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        private Context()
        {
        }
        
        public static Context Background()
        {
            return new Context();
        }

        public static Context WithValue(Context parent, string name, object value)
        {
            var copy = (Context)parent.MemberwiseClone();
            copy.properties.Add(name, value);
            return copy;
        }

        public object GetProperty(string name)
        {
            return properties.TryGetValue(name, out var result) ? result : null;
        }

        public override string ToString()
        {
            var lines = properties.Select(kvp => kvp.Key + ": " + kvp.Value);
            return string.Join(Environment.NewLine, lines);
        }
    }
}