using NGO.Cookbook.Settings;
using RecipeEngine;
using RecipeEngine.Modules.BuildAutomation;
using RecipeEngine.Modules.Wrench.Helpers;


// ReSharper disable once CheckNamespace
public static class Program
{
    public static int Main(string[] args)
    {
        var settings = new NGOSettings();
        var buildAutomationSettings = new NgoBuildAutomationSettings();

        // ReSharper disable once UnusedVariable
        var engine = EngineFactory
            .Create()
            .ScanAll()
            .WithWrenchModule(settings.Wrench)
            .WithBuildAutomation(buildAutomationSettings.BuildAutomation)
            .GenerateAsync().Result;
        return engine;
    }
}
