using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Platforms;
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
                ValidationOptions = validationOptions,
                MaximumEditorVersion = "6000.5"
            }
        }
    };

    public NGOSettings()
    {
        Wrench = new WrenchSettings(packagesRootPaths, PackageOptions);
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
        Wrench.Packages["com.unity.netcode.gameobjects"].PackAndPromotePlatformType = EditorPlatformType.Ubuntu2204;
    }

    public WrenchSettings Wrench { get; private set; }
}
