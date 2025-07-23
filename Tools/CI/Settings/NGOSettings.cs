using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace NGO.Cookbook.Settings;

public class NGOSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] packagesRootPaths = {"."};

    static ValidationOptions validationOptions = new ValidationOptions()
    {
        ProjectPath = "testproject",
        UtrTestingYamatoTimeout = 180 // 3h This it to address the issue that we are running both package and project test and that their execution is much slower on editors below 6000
    };

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.netcode.gameobjects",
            new PackageOptions()
            {
                ReleaseOptions = new ReleaseOptions() { IsReleasing = true },
                ValidationOptions = validationOptions
            }
        }
    };

    public NGOSettings()
    {
        Wrench = new WrenchSettings(
            packagesRootPaths,
            PackageOptions,
            false,
            false,
            @"Tools\CI\NGO.Cookbook.csproj"); // There should be fix soon and there should be no need of specifying the path

        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
    }

    public WrenchSettings Wrench { get; private set; }
}
