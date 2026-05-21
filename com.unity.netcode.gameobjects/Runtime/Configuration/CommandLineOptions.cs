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
        private static List<string> s_CommandLineArguments = new List<string>(Environment.GetCommandLineArgs());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad()
        {
            Instance = new CommandLineOptions();
#if UNITY_EDITOR
            s_CommandLineArguments = new List<string>(Environment.GetCommandLineArgs());
#endif
        }

        /// <summary>
        /// Returns the value of an argument or null if the argument is not present
        /// </summary>
        /// <param name="arg">The name of the argument</param>
        /// <returns><see cref="string"/>Value of the command line argument passed in.</returns>
        public string GetArg(string arg)
        {
            var argIndex = s_CommandLineArguments.IndexOf(arg);
            if (argIndex >= 0 && argIndex < s_CommandLineArguments.Count - 1)
            {
                return s_CommandLineArguments[argIndex + 1];
            }
            return null;
        }
    }
}
