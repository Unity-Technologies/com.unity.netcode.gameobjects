using System;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Logging;


namespace MLAPI.Logging
{
    public class ContextLogger
    {
        private readonly Context context;
        private Dictionary<string, object>  fields = new Dictionary<string, object>();

        public ContextLogger(Context ctx)
        {
            context = ctx;
        }
        
        public void WithField(string name, object value)
        {
            fields.Add(name, value);
        }
        
        public void WithFields(Dictionary<string, object> f)
        {
            fields = f;
        }
        
        private string Message(string message)
        {
            if (context != null)
            {
                message = $"{message} ctx:{context}";
            }
            if (fields != null && fields.Count > 0)
            {
                var lines = fields.Select(kvp => kvp.Key + ": " + kvp.Value);
                var l = string.Join(Environment.NewLine, lines);
                message = $"{message} fields:{l}";
            }
            return message;
        }
        
        /// <summary>Logs an info log with the proper MLAPI prefix</summary>
        /// <param name="message">The message to log</param>
        public void LogInfo(string message)
        {
            NetworkLog.LogInfo(Message(message));
        }

        /// <summary>Logs a warning log with the proper MLAPI prefix</summary>
        /// <param name="message">The message to log</param>
        public void LogWarning(string message)
        {
            NetworkLog.LogWarning(Message(message));
        }

        /// <summary>Logs an error log with the proper MLAPI prefix</summary>
        /// <param name="message">The message to log</param>
        public void LogError(string message)
        {
            NetworkLog.LogError(Message(message));
        }
    }
}
