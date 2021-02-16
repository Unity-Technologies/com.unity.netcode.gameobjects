using System;
using System.IO;

#if DEVELOPMENT_BUILD || UNITY_EDITOR

namespace MLAPI.Logging
{
    /// <summary>
    /// Helper class for logging
    /// Saves to a file identified by the current UTC epoch time. This is useful
    /// when debugging what happens on multiple processes on the same machine
    /// Only available for debugging on DEVELOPMENT_BUILD or UNITY_EDITOR
    /// </summary>
    public class FileLogger:IDisposable
    {
        private static FileLogger m_Instance = null;
        public static FileLogger Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new FileLogger();
                }
                return m_Instance;
            }
        }

        private StreamWriter m_Writer;

        private FileLogger()
        {
            m_Writer = new StreamWriter("log." + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".txt");
            m_Writer.AutoFlush = true;
        }

        public void Dispose()
        {
            m_Writer.Dispose();
        }

        /// <summary>
        /// Logs a line
        /// <param name="line">The line to log to the file for this process</param>
        /// </summary>
        public void Log(string line)
        {
            m_Writer.WriteLine(line);
        }
    }
}

#endif
