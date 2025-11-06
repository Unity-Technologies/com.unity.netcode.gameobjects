using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    public class CommandLineOptions
    {
        public static CommandLineOptions Instance { get; private set; } = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoad() => Instance = new CommandLineOptions();

        // Contains the current application instance domain's command line arguments
        internal static List<string> CommandLineArguments = new List<string>();

        // Invoked upon application start
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
