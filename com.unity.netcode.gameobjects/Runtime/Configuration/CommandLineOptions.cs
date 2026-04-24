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
        private static List<string> s_CommandLineArguments = new List<string>(Environment.GetCommandLineArgs());

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnLoad()
        {
            Instance = new CommandLineOptions();
            s_Instance = new CommandLineOptions();
            // Get all the command line arguments to be parsed later and/or modified
            // prior to being parsed (for testing purposes).
            s_CommandLineArguments = new List<string>(Environment.GetCommandLineArgs());
        }
#endif

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
