using System;

internal static class CLI
{
    /// <summary>
    /// summary bla bla
    /// </summary>
    /// <param name="check">Check for standards issues (will not change files)</param>
    /// <param name="fix">Try to fix standards issues (could change files)</param>
    /// <param name="project">Target project path</param>
    /// <param name="verbosity">Logs verbosity level</param>
    private static void Main(bool check = true, bool fix = false, string project = "testproject", string verbosity = "minimal")
    {
        Console.WriteLine($"{nameof(check)}: {check}");
        Console.WriteLine($"{nameof(fix)}: {fix}");
        Console.WriteLine($"{nameof(project)}: {project}");
        Console.WriteLine($"{nameof(verbosity)}: {verbosity}");
    }
}