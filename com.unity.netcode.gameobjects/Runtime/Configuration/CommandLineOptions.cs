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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoad() => Instance = new CommandLineOptions();

        // Contains the current application instance domain's command line arguments
        internal static List<string> CommandLineArguments = new List<string>();

        // Invoked upon application start, after scene load
        [RuntimeInitializeOnLoadMethod]
        private static void ParseCommandLineArguments()
        {
            // Get all the command line arguments to be parsed later and/or modified
            // prior to being parsed (for testing purposes).
            CommandLineArguments = new List<string>(Environment.GetCommandLineArgs());
        }

        /// <summary>
        /// Returns the value of an argument or null if there the argument is not present
        /// </summary>
        /// <param name="arg">The name of the argument</param>
        /// <returns><see cref="string"/>Value of the command line argument passed in.</returns>
        public string GetArg(string arg)
        {
            var argIndex = CommandLineArguments.IndexOf(arg);
            if (argIndex >= 0 && argIndex < CommandLineArguments.Count - 1)
            {
                return CommandLineArguments[argIndex + 1];
            }
            return null;
        }
    }
}
