using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Helpers;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Platforms;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Unity.Abstractions.Editors;

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
                ValidationOptions = validationOptions
            }
        }
    };

    public NGOSettings()
    {
        Wrench = new WrenchSettings(packagesRootPaths, PackageOptions);
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
        Wrench.Packages["com.unity.netcode.gameobjects"].PackAndPromotePlatformType = EditorPlatformType.Ubuntu2204;

        // com.unity.services.multiplayer's Entities integration doesn't yet compile against the N4E 7.0.0 that NGO now depends on, so skip it in Preview APV until a compatible version ships.
        Wrench.Packages["com.unity.netcode.gameobjects"].DependantsToIgnoreInPreviewApv = new Dictionary<Editor, ISet<string>>
        {
            [new EditorVersion("6000.7")] = new HashSet<string> { "com.unity.services.multiplayer" }
        };
    }

    public WrenchSettings Wrench { get; private set; }
}
