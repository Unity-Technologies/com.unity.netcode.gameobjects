using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Helpers;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Platforms;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Unity.Abstractions.Editors;

namespace NGO.Cookbook.Settings;

public class NgoWrenchSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] packagesRootPaths = {"."};

    static ValidationOptions validationOptions = new ValidationOptions()
    {
        ProjectPath = "testproject",
        UtrTestingYamatoTimeout = 90
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

    public NgoWrenchSettings()
    {
        Wrench = new WrenchSettings(packagesRootPaths, PackageOptions);
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
        Wrench.Packages["com.unity.netcode.gameobjects"].PackAndPromotePlatformType = EditorPlatformType.Ubuntu2204;

        // com.unity.services.multiplayer tests reference NGO test helpers that aren't exposed in the APV project, so they fail to compile there.
        var apvDependantsToIgnore = new HashSet<string> { "com.unity.services.multiplayer" };
        Wrench.Packages["com.unity.netcode.gameobjects"].DependantsToIgnoreInPreviewApv = new Dictionary<Editor, ISet<string>>
        {
            [new EditorVersion("6000.7")] = apvDependantsToIgnore,
            [new EditorVersion("7000.0")] = apvDependantsToIgnore
        };
    }

    public WrenchSettings Wrench { get; private set; }
}
