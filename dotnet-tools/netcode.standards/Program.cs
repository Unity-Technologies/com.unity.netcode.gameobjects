using System;
using System.IO;
using System.Diagnostics;

internal static class Program
{
    /// <summary>
    /// C# coding standards tool for Netcode that relies on `.editorconfig` ruleset and `dotnet format` tool
    /// </summary>
    /// <param name="check">Check for standards issues (will not change files)</param>
    /// <param name="fix">Try to fix standards issues (could change files)</param>
    /// <param name="project">Target project folder</param>
    /// <param name="pattern">Search pattern string</param>
    /// <param name="verbosity">Logs verbosity level</param>
    private static int Main(
        bool check = false, bool fix = false,
        string project = "testproject",
        string pattern = "*.sln",
        string verbosity = "normal")
    {
        if (check && fix)
        {
            Console.WriteLine($"FAILED: Please use --{nameof(check)} or --{nameof(fix)} individually, not both at the same time");
            return 1;
        }

        if (!check && !fix)
        {
            Console.WriteLine($"FAILED: Please use at least one of --{nameof(check)} or --{nameof(fix)} workflows");
            return 2;
        }

        foreach (var file in Directory.GetFiles(project, pattern))
        {
            var procInfo = new ProcessStartInfo("dotnet");

            procInfo.Arguments = check
                ? $"format whitespace {file} --no-restore --verify-no-changes --verbosity {verbosity}"
                : $"format whitespace {file} --no-restore --verbosity {verbosity}";
            Console.WriteLine($"######## START -> {(check ? "check" : "fix")} whitespace issues");
            var whitespace = Process.Start(procInfo);
            whitespace.WaitForExit();
            if (whitespace.ExitCode != 0)
            {
                Console.WriteLine("######## FAILED -> found whitespace issues (see details above)");
                return whitespace.ExitCode;
            }
            Console.WriteLine("######## SUCCEEDED -> no whitespace issues");

            procInfo.Arguments = check
                ? $"format style {file} --severity error --no-restore --verify-no-changes --verbosity {verbosity}"
                : $"format style {file} --severity error --no-restore --verbosity {verbosity}";
            Console.WriteLine($"######## START -> {(check ? "check" : "fix")} style/naming issues");
            var style = Process.Start(procInfo);
            style.WaitForExit();
            if (style.ExitCode != 0)
            {
                Console.WriteLine("######## FAILED -> found style/naming issues (see details above)");
                return style.ExitCode;
            }
            Console.WriteLine("######## SUCCEEDED -> no style/naming issues");
        }

        return 0;
    }
}