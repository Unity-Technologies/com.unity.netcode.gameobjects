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
        UtrTestingYamatoTimeout = 40
    };

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.netcode.gameobjects",
            new PackageOptions()
            {
                ReleaseOptions = new ReleaseOptions() { IsReleasing = true },
                MaximumEditorVersion = "6000.3", // This maximum version was set to enable the release of 2.8.1 due to breaking editor changes introduced in 6000.4/6000.5 editors which we will resolve in NGOv2.9.0
                ValidationOptions = validationOptions
            }
        }
    };

    public NGOSettings()
    {
        Wrench = new WrenchSettings(
            packagesRootPaths,
            PackageOptions
        );

    Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
    }

    public WrenchSettings Wrench { get; private set; }
}
