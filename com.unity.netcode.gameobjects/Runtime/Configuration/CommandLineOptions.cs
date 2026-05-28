using System;
using System.Collections.Generic;
using UnityEngine;

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
        public static CommandLineOptions Instance { get; private set; }

        // Contains the current application instance domain's command line arguments
        private static readonly List<string> k_CommandLineArguments = new List<string>(Environment.GetCommandLineArgs());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad()
        {
            Instance = new CommandLineOptions();
        }

        /// <summary>
        /// Returns the value of an argument or null if the argument is not present
        /// </summary>
        /// <param name="arg">The name of the argument</param>
        /// <returns><see cref="string"/>Value of the command line argument passed in.</returns>
        public string GetArg(string arg)
        {
            var argIndex = k_CommandLineArguments.IndexOf(arg);
            if (argIndex >= 0 && argIndex < k_CommandLineArguments.Count - 1)
            {
                return k_CommandLineArguments[argIndex + 1];
            }
            return null;
        }
    }
}
