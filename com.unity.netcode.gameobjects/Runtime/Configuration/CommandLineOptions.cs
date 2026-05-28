using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    /// <summary>
    /// This class contains a list of the application instance domain's command line arguments that
    /// are used when entering PlayMode or the build is executed.
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// Command-line options singleton
        /// </summary>
        [Obsolete("Not used anymore replaced by TryGetArg")]
        public static CommandLineOptions Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new CommandLineOptions();
                }
                return s_Instance;
            }
            private set
            {
                s_Instance = value;
            }
        }
        private static CommandLineOptions s_Instance;

        // Contains the current application instance domain's command line arguments
        private static readonly List<string> k_CommandLineArguments = new List<string>(Environment.GetCommandLineArgs());

        /// <summary>
        /// Returns the value of an argument or null if the argument is not present
        /// </summary>
        /// <param name="arg">The name of the argument</param>
        /// <returns><see cref="string"/>Value of the command line argument passed in.</returns>
        [Obsolete("Not used anymore replaced by TryGetArg")]
        public string GetArg(string arg)
        {
            var argIndex = k_CommandLineArguments.IndexOf(arg);
            if (argIndex >= 0 && argIndex < k_CommandLineArguments.Count - 1)
            {
                return k_CommandLineArguments[argIndex + 1];
            }
            return null;
        }

        /// <summary>
        /// Returns true if the argument was found.
        /// </summary>
        /// <param name="arg">The name of the argument to look up.</param>
        /// <param name="argValue">The argument's value, or <see langword="null"/> if not found.</param>
        /// <returns><c>true</c> if the argument was found; otherwise <c>false</c>.</returns>
        public static bool TryGetArg(string arg, out string argValue)
        {
            var argIndex = k_CommandLineArguments.IndexOf(arg);
            if (argIndex >= 0 && argIndex < k_CommandLineArguments.Count - 1)
            {
                argValue = k_CommandLineArguments[argIndex + 1];
                return true;
            }
            argValue = null;
            return false;
        }
    }
}
