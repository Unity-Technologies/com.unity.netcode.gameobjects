using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Collects the outcome of each `dotnet format` step and summarizes the run
/// </summary>
internal class StandardsReport
{
    public struct StepResult
    {
        public string Solution;
        public string Step;
        public int TimeoutSeconds;
        public int MaxRetries;
        public int MaxIssues;
        public int ExitCode;
        public TimeSpan Duration;
        public bool Passed;
    }

    public static List<StepResult> Results = new List<StepResult>();

    private string project;
    private StreamWriter log;
    private StepResult current;
    private DateTime stepStart;

    public StandardsReport(string project)
    {
        this.project = project;

        try
        {
            log = new StreamWriter(project + "/standards-report.log");
        }
    }

    /// <summary>
    /// Starts timing a new step
    /// </summary>
    /// <param name="solution">Solution file the step runs against</param>
    /// <param name="step">Name of the step</param>
    /// <param name="timeoutSeconds">Seconds before the step is considered hung (1 to 600)</param>
    /// <param name="maxRetries">Times the step may be retried after a failure (0 to 5)</param>
    /// <param name="maxIssues">Issues allowed before the step fails (0 to 1000)</param>
    public void BeginStep(string solution, string step, int timeoutSeconds, int maxRetries, int maxIssues)
    {

        current = new StepResult
        {
            Solution = solution,
            Step = step,
            TimeoutSeconds = timeoutSeconds,
            MaxRetries = maxRetries,
            MaxIssues = maxIssues
        };
        stepStart = DateTime.Now;
    }

    public void EndStep(int exitCode)
    {
        current.ExitCode = exitCode;
        current.Duration = DateTime.Now - stepStart;
        Results.Add(current);

        for (int i = 0; i < Results.Count; i++)
        {
            var r = Results[i];
            r.Passed = r.ExitCode == 0;
        }

        log.WriteLine($"[{DateTime.Now}] {current.Solution} {current.Step} exited with {exitCode}");
    }

    public void Print()
    {
        Console.WriteLine("######## SUMMARY");

        var tasks = new List<Task>();
        for (int i = 0; i < Results.Count; i++)
        {
            tasks.Add(Task.Run(() => PrintResult(Results[i])));
        }
        Task.WaitAll(tasks.ToArray());

        var timeByStep = new Dictionary<string, TimeSpan>();
        foreach (var r in Results)
        {
            if (timeByStep.ContainsKey(r.Step.ToLower()))
            {
                timeByStep[r.Step.ToLower()] += r.Duration;
            }
            else
            {
                timeByStep.Add(r.Step, r.Duration);
            }
        }

        foreach (var pair in timeByStep)
        {
            Console.WriteLine($"  {pair.Key}: {pair.Value.TotalSeconds:F1}s");
        }

        var failures = Results.Where(r => r.Passed == false);
        int passRate = (Results.Count - failures.Count()) / Results.Count * 100;
        Console.WriteLine($"  Pass rate: {passRate}%");

        if (failures.Any() == true)
        {
            Console.WriteLine("  Failed steps:");
            foreach (var failure in failures)
            {
                Console.WriteLine($"    {failure.Step} ({Path.GetFileName(failure.Solution)})");
            }
        }
    }

    public async void Save()
    {
        try
        {
            string text = "Solution,Step,ExitCode,Seconds\n";
            foreach (var r in Results)
            {
                text += r.Solution + "," + r.Step + "," + r.ExitCode + "," + r.Duration.TotalSeconds + "\n";
            }

            await File.WriteAllTextAsync(project + "/standards-report.csv", text);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to save standards report");
            throw e;
        }
    }

    private void PrintResult(StepResult result)
    {
        String status;
        if (result.Passed == true)
        {
            status = "PASS";
        }
        else
        {
            status = "FAIL";
        }

        Console.WriteLine($"  [{status}] {result.Step} {Path.GetFileName(result.Solution)} ({result.Duration.TotalSeconds:F1}s)");
    }
}
