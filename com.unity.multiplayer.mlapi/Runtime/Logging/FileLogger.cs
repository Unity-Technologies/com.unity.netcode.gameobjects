using System.IO;

namespace MLAPI.Logging
{
    /// <summary>
    /// Helper class for logging
    /// Saves to a file identified by the process ID of each client
    /// This is useful when debugging what happens on multiple machines
    /// ! Only recommended for debugging, not for production !
    /// </summary>
    public class FileLogger
    {
        public static FileLogger Get() { return instance; }
        private static FileLogger instance = new FileLogger();
        private StreamWriter writer;

        private FileLogger()
        {
            System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess();
            writer = new StreamWriter("log." + p.Id + ".txt");
            writer.AutoFlush = true;
        }

        ~FileLogger()
        {
            writer.Close();
        }

        /// <summary>
        /// Logs a line
        /// <param name="line">The line to log to the file for this process</param>
        /// </summary>
        public void Log(string line)
        {
            writer.WriteLine(line);
        }
    }
}
