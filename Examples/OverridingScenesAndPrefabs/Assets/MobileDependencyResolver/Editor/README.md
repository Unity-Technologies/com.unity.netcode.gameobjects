# External Dependency Manager for Unity

[![openupm](https://img.shields.io/npm/v/com.google.external-dependency-manager?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.google.external-dependency-manager/)
[![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.google.external-dependency-manager)](https://openupm.com/packages/com.google.external-dependency-manager/)

## Overview

The External Dependency Manager for Unity (EDM4U) (formerly Play Services
Resolver/Jar Resolver) is intended to be used by any Unity package or user that
requires:

*   Android specific libraries (e.g
    [AARs](https://developer.android.com/studio/projects/android-library.html))

*   iOS [CocoaPods](https://cocoapods.org/)

*   Version management of transitive dependencies

*   Management of Package Manager (PM) Registries

If you want to add and use iOS/Android dependencies directly in your project,
then you should to install EDM4U in your project.

If you are a package user and the plugin you are using depends on EDM4U, *and*
the package does not include EDM4U as a package dependency already, then you
should to install EDM4U in your project.

If you are a UPM package maintainer and your package requires EDM4U, then you
should add EDM4U as a
[package dependency](https://docs.unity3d.com/2019.3/Documentation/Manual/upm-dependencies.html)
in your package manifest (`package.json`):

```json
{
  "dependencies": {
    "com.google.external-dependency-manager": "1.2.178"
  }
}
```

You should still install EDM4U to test out the package during development.

If you are a legacy `.unitypackage` package maintainer and your package requires
EDM4U, please ask the user to install EDM4U separately. You should install EDM4U
to test out the package during development.

Updated releases are available on
[GitHub](https://github.com/googlesamples/unity-jar-resolver)

## Requirements

The *Android Resolver* and *iOS Resolver* components of the plugin only work
with Unity version 4.6.8 or higher.

The *Version Handler* component only works with Unity 5.x or higher as it
depends upon the `PluginImporter` UnityEditor API.

The *Package Manager Resolver* component only works with Unity 2018.4 or above,
when [scoped registry](https://docs.unity3d.com/Manual/upm-scoped.html) support
was added to the Package Manager.

## Getting Started

Check out [troubleshooting](troubleshooting-faq.md) if you need help.

### Install via OpenUPM

EDM4U is available on
[OpenUPM](https://openupm.com/packages/com.google.external-dependency-manager/):

```shell
openupm add com.google.external-dependency-manager
```

### Install via git URL
1. Open Package Manager
2. Click on the + icon on the top left corner of the "Package Manager" screen
3. Click on "Install package from git url..."
4. Paste: https://github.com/googlesamples/unity-jar-resolver.git?path=upm

### Install via Google APIs for Unity

EDM4U is available both in UPM and legacy `.unitypackage` formats on
[Google APIs for Unity](https://developers.google.com/unity/archive#external_dependency_manager_for_unity).

You may install the UPM version (.tgz) as a
[local UPM package](https://docs.unity3d.com/Manual/upm-ui-local.html).

You can also install EDM4U in your project as a `.unitypackage`. This is not
recommended due to potential conflicts.

### Conflict Resolution

For historical reasons, a package maintainer may choose to embed EDM4U in their
package for ease of installation. This will create a conflict when you try to
install EDM4U with the steps above, or with another package with embedded EDM4U.
If your project imported a `.unitypackage` that has a copy of EDM4U embedded in
it, you may safely delete it from your Assets folder. If your project depends on
another UPM package with EDM4U, please reach out to the package maintainer and
ask them to replace it with a dependency to this package. In the meantime, you
can workaround the issue by copying the package to your Packages folder (to
create an
[embedded package](https://docs.unity3d.com/Manual/upm-concepts.html#Embedded))
and perform the steps yourself to avoid a dependency conflict.

### Config file

To start adding dependencies to your project, copy and rename the
[SampleDependencies.xml](https://github.com/googlesamples/unity-jar-resolver/blob/master/sample/Assets/ExternalDependencyManager/Editor/SampleDependencies.xml)
file into your plugin and add the dependencies your project requires.

The XML file needs to be under an `Editor` directory and match the name
`*Dependencies.xml`. For example, `MyPlugin/Editor/MyPluginDependencies.xml`.

## Usages

### Android Resolver

The Android Resolver copies specified dependencies from local or remote Maven
repositories into the Unity project when a user selects Android as the build
target in the Unity editor.

For example, to add the Google Play Games library
(`com.google.android.gms:play-services-games` package) at version `9.8.0` to the
set of a plugin's Android dependencies:

```xml
<dependencies>
  <androidPackages>
    <androidPackage spec="com.google.android.gms:play-services-games:9.8.0">
      <androidSdkPackageIds>
        <androidSdkPackageId>extra-google-m2repository</androidSdkPackageId>
      </androidSdkPackageIds>
    </androidPackage>
  </androidPackages>
</dependencies>
```

The version specification (last component) supports:

*   Specific versions e.g `9.8.0`

*   Partial matches e.g `9.8.+` would match 9.8.0, 9.8.1 etc. choosing the most
    recent version

*   Latest version using `LATEST` or `+`. We do *not* recommend using this
    unless you're 100% sure the library you depend upon will not break your
    Unity plugin in future

The above example specifies the dependency as a component of the Android SDK
manager such that the Android SDK manager will be executed to install the
package if it's not found. If your Android dependency is located on Maven
central it's possible to specify the package simply using the `androidPackage`
element:

```xml
<dependencies>
  <androidPackages>
    <androidPackage spec="com.google.api-client:google-api-client-android:1.22.0" />
  </androidPackages>
</dependencies>
```

#### Auto-resolution

By default the Android Resolver automatically monitors the dependencies you have
specified and the `Plugins/Android` folder of your Unity project. The resolution
process runs when the specified dependencies are not present in your project.

The *auto-resolution* process can be disabled via the `Assets > External
Dependency Manager > Android Resolver > Settings` menu.

Manual resolution can be performed using the following menu options:

*   `Assets > External Dependency Manager > Android Resolver > Resolve`

*   `Assets > External Dependency Manager > Android Resolver > Force Resolve`

#### Deleting libraries

Resolved packages are tracked via asset labels by the Android Resolver. They can
easily be deleted using the `Assets > External Dependency Manager > Android
Resolver > Delete Resolved Libraries` menu item.

#### Android Manifest Variable Processing

Some AAR files (for example play-services-measurement) contain variables that
are processed by the Android Gradle plugin. Unfortunately, Unity does not
perform the same processing when using Unity's Internal Build System, so the
Android Resolver plugin handles known cases of this variable substitution by
exploding the AAR into a folder and replacing `${applicationId}` with the
`bundleID`.

Disabling AAR explosion and therefore Android manifest processing can be done
via the `Assets > External Dependency Manager > Android Resolver > Settings`
menu. You may want to disable explosion of AARs if you're exporting a project to
be built with Gradle/Android Studio.

#### ABI Stripping

Some AAR files contain native libraries (.so files) for each ABI supported by
Android. Unfortunately, when targeting a single ABI (e.g x86), Unity does not
strip native libraries for unused ABIs. To strip unused ABIs, the Android
Resolver plugin explodes an AAR into a folder and removes unused ABIs to reduce
the built APK size. Furthermore, if native libraries are not stripped from an
APK (e.g you have a mix of Unity's x86 library and some armeabi-v7a libraries)
Android may attempt to load the wrong library for the current runtime ABI
completely breaking your plugin when targeting some architectures.

AAR explosion and therefore ABI stripping can be disabled via the `Assets >
External Dependency Manager > Android Resolver > Settings` menu. You may want to
disable explosion of AARs if you're exporting a project to be built with
Gradle/Android Studio.

#### Resolution Strategies

By default the Android Resolver will use Gradle to download dependencies prior
to integrating them into a Unity project. This works with Unity's internal build
system and Gradle/Android Studio project export.

It's possible to change the resolution strategy via the `Assets > External
Dependency Manager > Android Resolver > Settings` menu.

##### Download Artifacts with Gradle

Using the default resolution strategy, the Android resolver executes the
following operations:

-   Remove the result of previous Android resolutions. E.g Delete all files and
    directories labeled with "gpsr" under `Plugins/Android` from the project.

-   Collect the set of Android dependencies (libraries) specified by a project's
    `*Dependencies.xml` files.

-   Run `download_artifacts.gradle` with Gradle to resolve conflicts and, if
    successful, download the set of resolved Android libraries (AARs, JARs).

-   Process each AAR/JAR so that it can be used with the currently selected
    Unity build system (e.g Internal vs. Gradle, Export vs. No Export). This
    involves patching each reference to `applicationId` in the
    `AndroidManifest.xml` with the project's bundle ID. This means resolution
    must be run again if the bundle ID has changed.

-   Move the processed AARs to `Plugins/Android` so they will be included when
    Unity invokes the Android build.

##### Integrate into mainTemplate.gradle

Unity 5.6 introduced support for customizing the `build.gradle` used to build
Unity projects with Gradle. When the *Patch mainTemplate.gradle* setting is
enabled, rather than downloading artifacts before the build, Android resolution
results in the execution of the following operations:

-   Remove the result of previous Android resolutions. E.g Delete all files and
    directories labeled with "gpsr" under `Plugins/Android` from the project and
    remove sections delimited with `// Android Resolver * Start` and `// Android
    Resolver * End` lines.

-   Collect the set of Android dependencies (libraries) specified by a project's
    `*Dependencies.xml` files.

-   Rename any `.srcaar` files in the build to `.aar` and exclude them from
    being included directly by Unity in the Android build as
    `mainTemplate.gradle` will be patched to include them instead from their
    local maven repositories.

-   Inject the required Gradle repositories into `mainTemplate.gradle` at the
    line matching the pattern `.*apply plugin:
    'com\.android\.(application|library)'.*` or the section starting at the line
    `// Android Resolver Repos Start`. If you want to control the injection
    point in the file, the section delimited by the lines `// Android Resolver
    Repos Start` and `// Android Resolver Repos End` should be placed in the
    global scope before the `dependencies` section.

-   Inject the required Android dependencies (libraries) into
    `mainTemplate.gradle` at the line matching the pattern `***DEPS***` or the
    section starting at the line `// Android Resolver Dependencies Start`. If
    you want to control the injection point in the file, the section delimited
    by the lines `// Android Resolver Dependencies Start` and `// Android
    Resolver Dependencies End` should be placed in the `dependencies` section.

-   Inject the packaging options logic, which excludes architecture specific
    libraries based upon the selected build target, into `mainTemplate.gradle`
    at the line matching the pattern `android +{` or the section starting at the
    line `// Android Resolver Exclusions Start`. If you want to control the
    injection point in the file, the section delimited by the lines `// Android
    Resolver Exclusions Start` and `// Android Resolver Exclusions End` should
    be placed in the global scope before the `android` section.

#### Dependency Tracking

The Android Resolver creates the
`ProjectSettings/AndroidResolverDependencies.xml` to quickly determine the set
of resolved dependencies in a project. This is used by the auto-resolution
process to only run the expensive resolution process when necessary.

#### Displaying Dependencies

It's possible to display the set of dependencies the Android Resolver would
download and process in your project via the `Assets > External Dependency
Manager > Android Resolver > Display Libraries` menu item.

### iOS Resolver

The iOS resolver component of this plugin manages
[CocoaPods](https://cocoapods.org/). A CocoaPods `Podfile` is generated and the
`pod` tool is executed as a post build process step to add dependencies to the
Xcode project exported by Unity.

Dependencies for iOS are added by referring to CocoaPods.

For example, to add the AdMob pod, version 7.0 or greater with bitcode enabled:

```xml
<dependencies>
  <iosPods>
    <iosPod name="Google-Mobile-Ads-SDK" version="~> 7.0" bitcodeEnabled="true"
            minTargetSdk="6.0" addToAllTargets="false" />
  </iosPods>
</dependencies>
```

#### Integration Strategies

The `CocoaPods` are either:

*   Downloaded and injected into the Xcode project file directly, rather than
    creating a separate xcworkspace. We call this `Xcode project` integration.

*   If the Unity version supports opening a xcworkspace file, the `pod` tool is
    used as intended to generate a xcworkspace which references the CocoaPods.
    We call this `Xcode workspace` integration.

The resolution strategy can be changed via the `Assets > External Dependency
Manager > iOS Resolver > Settings` menu.

##### Appending text to generated Podfile

In order to modify the generated Podfile you can create a script like this:

```csharp
using System.IO;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class PostProcessIOS : MonoBehaviour
{
    // Must be between 40 and 50 to ensure that it's not overriden by Podfile generation (40) and
    // that it's added before "pod install" (50).
    [PostProcessBuildAttribute(45)]
    private static void PostProcessBuild_iOS(BuildTarget target, string buildPath)
    {
        if (target == BuildTarget.iOS)
        {
            using (StreamWriter sw = File.AppendText(buildPath + "/Podfile"))
            {
                // E.g. add an app extension
                sw.WriteLine("\ntarget 'NSExtension' do\n  pod 'Firebase/Messaging', '6.6.0'\nend");
            }
        }
    }
}
```

### Package Manager Resolver

Adding registries to the
[Package Manager](https://docs.unity3d.com/Manual/Packages.html) (PM) is a
manual process. The Package Manager Resolver (PMR) component of this plugin
makes it easy for plugin maintainers to distribute new PM registry servers and
easy for plugin users to manage PM registry servers.

#### Adding Registries

For example, to add a registry for plugins in the scope `com.coolstuff`:

```xml
<registries>
  <registry name="Cool Stuff"
            url="https://unityregistry.coolstuff.com"
            termsOfService="https://coolstuff.com/unityregistry/terms"
            privacyPolicy="https://coolstuff.com/unityregistry/privacy">
    <scopes>
      <scope>com.coolstuff</scope>
    </scopes>
  </registry>
</registries>
```

When PMR is loaded it will prompt the developer to add the registry to their
project if it isn't already present in the `Packages/manifest.json` file.

For more information, see Unity's documentation on
[scoped package registries](https://docs.unity3d.com/Manual/upm-scoped.html).

#### Managing Registries

It's possible to add and remove registries that are specified via PMR XML
configuration files via the following menu options:

*   `Assets > External Dependency Manager > Package Manager Resolver > Add
    Registries` will prompt the user with a window which allows them to add
    registries discovered in the project to the Package Manager.

*   `Assets > External Dependency Manager > Package Manager Resolver > Remove
    Registries` will prompt the user with a window which allows them to remove
    registries discovered in the project from the Package Manager.

*   `Assets > External Dependency Manager > Package Manager Resolver > Modify
    Registries` will prompt the user with a window which allows them to add or
    remove registries discovered in the project.

#### Migration

PMR can migrate Version Handler packages installed in the `Assets` folder to PM
packages. This requires the plugins to implement the following:

*   `.unitypackage` must include a Version Handler manifests that describes the
    components of the plugin. If the plugin has no dependencies the manifest
    would just include the files in the plugin.

*   The PM package JSON provided by the registry must include a keyword (in the
    `versions.VERSION.keyword` list) that maps the PM package to a Version
    Handler package using the format `vh-name:VERSION_HANDLER_MANIFEST_NAME`
    where `VERSION_HANDLER_MANIFEST_NAME` is the name of the manifest defined in
    the `.unitypackage`. For more information see the description of the
    `gvhp_manifestname` asset label in the [Version Handler](#version-handler)
    section.

When using the `Assets > External Dependency Manager > Package Manager
Resolver > Migrate Packages` menu option, PMR then will:

*   List all Version Handler manager packages in the project.

*   Search all available packages in the PM registries and fetch keywords
    associated with each package parsing the Version Handler manifest names for
    each package.

*   Map each installed Version Handler package to a PM package.

*   Prompt the user to migrate the discovered packages.

*   Perform package migration for all selected packages if the user clicks the
    `Apply` button.

#### Configuration

PMR can be configured via the `Assets > External Dependency Manager > Package
Manager Resolver > Settings` menu option:

*   `Add package registries` when enabled, when the plugin loads or registry
    configuration files change, this will prompt the user to add registries that
    are not present in the Package Manager.

*   `Prompt to add package registries` will cause a developer to be prompted
    with a window that will ask for confirmation before adding registries. When
    this is disabled registries are added silently to the project.

*   `Prompt to migrate packages` will cause a developer to be prompted with a
    window that will ask for confirmation before migrating packages installed in
    the `Assets` directory to PM packages.

*   `Enable Analytics Reporting` when enabled, reports the use of the plugin to
    the developers so they can make imrpovements.

*   `Verbose logging` when enabled prints debug information to the console which
    can be useful when filing bug reports.

### Version Handler

The Version Handler component of this plugin manages:

*   Shared Unity plugin dependencies.

*   Upgrading Unity plugins by cleaning up old files from previous versions.

*   Uninstallation of plugins that are distributed with manifest files.

*   Restoration of plugin assets to their original install locations if assets
    are tagged with the `exportpath` label.

Since the Version Handler needs to modify Unity asset metadata (`.meta` files),
to enable/disable components, rename and delete asset files it does not work
with Package Manager installed packages. It's still possible to include EDM4U in
Package Manager packages, the Version Handler component simply won't do anything
to PM plugins in this case.

#### Using Version Handler Managed Plugins

If a plugin is imported at multiple different versions into a project, if the
Version Handler is enabled, it will automatically check all managed assets to
determine the set of assets that are out of date and assets that should be
removed. To disable automatic checking managed assets disable the `Enable
version management` option in the `Assets > External Dependency Manager >
Version Handler > Settings` menu.

If version management is disabled, it's possible to check managed assets
manually using the `Assets > External Dependency Manager > Version Handler >
Update` menu option.

##### Listing Managed Plugins

Plugins managed by the Version Handler, those that ship with manifest files, can
displayed using the `Assets > External Dependency Manager > Version Handler >
Display Managed Packages` menu option. The list of plugins are written to the
console window along with the set of files used by each plugin.

##### Uninstalling Managed Plugins

Plugins managed by the Version Handler, those that ship with manifest files, can
be removed using the `Assets > External Dependency Manager > Version Handler >
Uninstall Managed Packages` menu option. This operation will display a window
that allows a developer to select a set of plugins to remove which will remove
all files owned by each plugin excluding those that are in use by other
installed plugins.

Files managed by the Version Handler, those labeled with the `gvh` asset label,
can be checked to see whether anything needs to be upgraded, disabled or removed
using the `Assets > External Dependency Manager > Version Handler > Update` menu
option.

##### Restore Install Paths

Some developers move assets around in their project which can make it harder for
plugin maintainers to debug issues if this breaks Unity's
[special folders](https://docs.unity3d.com/Manual/SpecialFolders.html) rules. If
assets are labeled with their original install/export path (see
`gvhp_exportpath` below), Version Handler can restore assets to their original
locations when using the `Assets > External Dependency Manager > Version
Handler > Move Files To Install Locations` menu option.

##### Settings

Some behavior of the Version Handler can be configured via the `Assets >
External Dependency Manager > Version Handler > Settings` menu option.

*   `Enable version management` controls whether the plugin should automatically
    check asset versions and apply changes. If this is disabled the process
    should be run manually when installing or upgrading managed plugins using
    `Assets > External Dependency Manager > Version Handler > Update`.

*   `Rename to canonical filenames` is a legacy option that will rename files to
    remove version numbers and other labels from filenames.

*   `Prompt for obsolete file deletion` enables the display of a window when
    obsolete files are deleted allowing the developer to select which files to
    delete and those to keep.

*   `Allow disabling files via renaming` controls whether obsolete or disabled
    files should be disabled by renaming them to `myfilename_DISABLED`. Renaming
    to disable files is required in some scenarios where Unity doesn't support
    removing files from the build via the PluginImporter.

*   `Enable Analytics Reporting` enables/disables usage reporting to plugin
    developers to improve the product.

*   `Verbose logging` enables *very* noisy log output that is useful for
    debugging while filing a bug report or building a new managed plugin.

*   `Use project settings` saves settings for the plugin in the project rather
    than system-wide.

#### Redistributing a Managed Plugin

The Version Handler employs a couple of methods for managing version selection,
upgrade and removal of plugins.

*   Each plugin can ship with a manifest file that lists the files it includes.
    This makes it possible for Version Handler to calculate the difference in
    assets between the most recent release of a plugin and the previous release
    installed in a project. If a files are removed the Version Handler will
    prompt the user to clean up obsolete files.

*   Plugins can ship using assets with unique names, unique GUIDs and version
    number labels. Version numbers can be attached to assets using labels or
    added to the filename (e.g `myfile.txt` would be `myfile_version-x.y.z.txt).
    This allows the Version Handler to determine which set of files are the same
    file at different versions, select the most recent version and prompt the
    developer to clean up old versions.

Unity plugins can be managed by the Version Handler using the following steps:

1.  Add the `gvh` asset label to each asset (file) you want Version Handler to
    manage.

1.  Add the `gvh_version-VERSION` label to each asset where `VERSION` is the
    version of the plugin you're releasing (e.g 1.2.3).

1.  Add the `gvhp_exportpath-PATH` label to each asset where `PATH` is the
    export path of the file when the `.unitypackage` is created. This is used to
    track files if they're moved around in a project by developers.

1.  Optional: Add `gvh_targets-editor` label to each editor DLL in your plugin
    and disable `editor` as a target platform for the DLL. The Version Handler
    will enable the most recent version of this DLL when the plugin is imported.

1.  Optional: If your plugin is included in other Unity plugins, you should add
    the version number to each filename and change the GUID of each asset. This
    allows multiple versions of your plugin to be imported into a Unity project,
    with the Version Handler component activating only the most recent version.

1.  Create a manifest text file named `MY_UNIQUE_PLUGIN_NAME_VERSION.txt` that
    lists all the files in your plugin relative to the project root. Then add
    the `gvh_manifest` label to the asset to indicate this file is a plugin
    manifest.

1.  Optional: Add a `gvhp_manifestname-NAME` label to your manifest file to
    provide a human readable name for your package. If this isn't provided the
    name of the manifest file will be used as the package name. NAME can match
    the pattern `[0-9]+[a-zA-Z -]` where a leading integer will set the priority
    of the name where `0` is the highest priority and preferably used as the
    display name. The lowest value (i.e highest priority name) will be used as
    the display name and all other specified names will be aliases of the
    display name. Aliases can refer to previous names of the package allowing
    renaming across published versions.

1.  Redistribute EDM4U Unity plugin with your plugin. See the
    [Plugin Redistribution](#plugin-redistribution) section for details.

If you follow these steps:

*   When users import a newer version of your plugin, files referenced by the
    older version's manifest are cleaned up.

*   The latest version of the plugin will be selected when users import multiple
    packages that include your plugin, assuming the steps in
    [Plugin Redistribution](#plugin-redistribution) are followed.

## Background

Many Unity plugins have dependencies upon Android specific libraries, iOS
CocoaPods, and sometimes have transitive dependencies upon other Unity plugins.
This causes the following problems:

*   Integrating platform specific (e.g Android and iOS) libraries within a Unity
    project can be complex and a burden on a Unity plugin maintainer.
*   The process of resolving conflicting dependencies on platform specific
    libraries is pushed to the developer attempting to use a Unity plugin. The
    developer trying to use your plugin is very likely to give up when faced
    with Android or iOS specific build errors.
*   The process of resolving conflicting Unity plugins (due to shared Unity
    plugin components) is pushed to the developer attempting to use your Unity
    plugin. In an effort to resolve conflicts, the developer will very likely
    attempt to resolve problems by deleting random files in your plugin, report
    bugs when that doesn't work and finally give up.

EDM4U provides solutions for each of these problems.

### Android Dependency Management

The *Android Resolver* component of this plugin will download and integrate
Android library dependencies and handle any conflicts between plugins that share
the same dependencies.

Without the Android Resolver, typically Unity plugins bundle their AAR and JAR
dependencies, e.g. a Unity plugin `SomePlugin` that requires the Google Play
Games Android library would redistribute the library and its transitive
dependencies in the folder `SomePlugin/Android/`. When a user imports
`SomeOtherPlugin` that includes the same libraries (potentially at different
versions) in `SomeOtherPlugin/Android/`, the developer using `SomePlugin` and
`SomeOtherPlugin` will see an error when building for Android that can be hard
to interpret.

Using the Android Resolver to manage Android library dependencies:

*   Solves Android library conflicts between plugins.
*   Handles all of the various processing steps required to use Android
    libraries (AARs, JARs) in Unity 4.x and above projects. Almost all versions
    of Unity have - at best - partial support for AARs.
*   (Experimental) Supports minification of included Java components without
    exporting a project.

### iOS Dependency Management

The *iOS Resolver* component of this plugin integrates with
[CocoaPods](https://cocoapods.org/) to download and integrate iOS libraries and
frameworks into the Xcode project Unity generates when building for iOS. Using
CocoaPods allows multiple plugins to utilize shared components without forcing
developers to fix either duplicate or incompatible versions of libraries
included through multiple Unity plugins in their project.

### Package Manager Registry Setup

The [Package Manager](https://docs.unity3d.com/Manual/Packages.html) (PM) makes
use of [NPM](https://www.npmjs.com/) registry servers for package hosting and
provides ways to discover, install, upgrade and uninstall packages. This makes
it easier for developers to manage plugins within their projects.

However, installing additional package registries requires a few manual steps
that can potentially be error prone. The *Package Manager Resolver* component of
this plugin integrates with [PM](https://docs.unity3d.com/Manual/Packages.html)
to provide a way to auto-install PM package registries when a `.unitypackage` is
installed which allows plugin maintainers to ship a `.unitypackage` that can
provide access to their own PM registry server to make it easier for developers
to manage their plugins.

### Unity Plugin Version Management

Finally, the *Version Handler* component of this plugin simplifies the process
of managing transitive dependencies of Unity plugins and each plugin's upgrade
process.

For example, without the Version Handler plugin, if:

*   Unity plugin `SomePlugin` includes `EDM4U` plugin at version 1.1.
*   Unity plugin `SomeOtherPlugin` includes `EDM4U` plugin at version 1.2.

The version of `EDM4U` included in the developer's project depends upon the
order the developer imports `SomePlugin` or `SomeOtherPlugin`.

This results in:

*   `EDM4U` at version 1.2, if `SomePlugin` is imported then `SomeOtherPlugin`
    is imported.
*   `EDM4U` at version 1.1, if `SomeOtherPlugin` is imported then `SomePlugin`
    is imported.

The Version Handler solves the problem of managing transitive dependencies by:

*   Specifying a set of packaging requirements that enable a plugin at different
    versions to be imported into a Unity project.
*   Providing activation logic that selects the latest version of a plugin
    within a project.

When using the Version Handler to manage `EDM4U` included in `SomePlugin` and
`SomeOtherPlugin`, from the prior example, version 1.2 will always be the
version activated in a developer's Unity project.

Plugin creators are encouraged to adopt this library to ease integration for
their customers. For more information about integrating EDM4U into your own
plugin, see the [Plugin Redistribution](#plugin-redistribution) section of this
document.

## Analytics

The External Dependency Manager for Unity plugin by default logs usage to Google
Analytics. The purpose of the logging is to quantitatively measure the usage of
functionality, to gather reports on integration failures and to inform future
improvements to the developer experience of the External Dependency Manager
plugin. Note that the analytics collected are limited to the scope of the EDM4U
plugin’s usage.

For details of what is logged, please refer to the usage of
`EditorMeasurement.Report()` in the source code.

## Plugin Redistribution

If you are a package maintainer and your package depends on EDM4U, it is highly
recommended to use the UPM format and add EDM4U as a dependency. If you must
include it in your `.unitypackage`, redistributing `EDM4U` inside your own
plugin might ease the integration process for your users.

If you wish to redistribute `EDM4U` inside your plugin, you **must** follow
these steps when importing the `external-dependency-manager-*.unitypackage`, and
when exporting your own plugin package:

1.  Import the `external-dependency-manager-*.unitypackage` into your plugin
    project by
    [running Unity from the command line](https://docs.unity3d.com/Manual/CommandLineArguments.html),
    ensuring that you add the `-gvh_disable` option.
1.  Export your plugin by
    [running Unity from the command line](https://docs.unity3d.com/Manual/CommandLineArguments.html),
    ensuring that you:
    -   Include the contents of the `Assets/PlayServicesResolver` and
        `Assets/ExternalDependencyManager` directory.
    -   Add the `-gvh_disable` option.

You **must** specify the `-gvh_disable` option in order for the Version Handler
to work correctly!

For example, the following command will import the
`external-dependency-manager-1.2.46.0.unitypackage` into the project
`MyPluginProject` and export the entire Assets folder to
`MyPlugin.unitypackage`:

```shell
Unity -gvh_disable \
      -batchmode \
      -importPackage external-dependency-manager-1.2.46.0.unitypackage \
      -projectPath MyPluginProject \
      -exportPackage Assets MyPlugin.unitypackage \
      -quit
```

### Background

The *Version Handler* component relies upon deferring the load of editor DLLs so
that it can run first and determine the latest version of a plugin component to
activate. The build of `EDM4U` plugin has Unity asset metadata that is
configured so that the editor components are not initially enabled when it's
imported into a Unity project. To maintain this configuration when importing the
`mobile-dependency-resolver.unitypackage` into a Unity plugin project, you
*must* specify the command line option `-gvh_disable` which will prevent the
Version Handler component from running and changing the Unity asset metadata.

## Building from Source

To build this plugin from source you need the following tools installed: * Unity
2021 and below (with iOS and Android modules installed) * Java 11

You can build the plugin by running the following from your shell (Linux / OSX):

```shell
./gradlew build

```

or Windows:

```shell
./gradlew.bat build
```

If Java 11 is not your default Java command, add
`-Dorg.gradle.java.home=<PATH_TO_JAVA_HOME>` to the command above.

## Testing

You can run the tests by running the following from your shell (Linux / OSX):

```shell
./gradlew test
```

or Windows:

```shell
./gradlew.bat test
```

The following properties can be set to narrow down the tests to run or change
the test run behavior.

*   `INTERACTIVE_MODE_TESTS_ENABLED` - Default to `1`. Set to `1` to enable
    interactive mode tests, which requires GPU on the machine. Otherwise, only
    run tests in the batch mode.
*   `INCLUDE_TEST_TYPES` - Default to empty string, which means to include every
    type of the test. To narrow down the types of test to run, set this
    properties with a list of case-insensitive type strings separated by comma.
    For instance, `-PINCLUDE_TEST_TYPES="Python,NUnit"` means to include only
    Python tests and NUnit tests. See `TestTypeEnum` in `build.gradle` for
    available options.
*   `EXCLUDE_TEST_TYPES` - Default to empty string, which means to exclude none.
    To add types of tests to exclude, set this properties with a list of
    case-insensitive type strings separated by comma. For instance,
    `-PEXCLUDE_TEST_TYPES="Python,NUnit"` means to exclude Python tests and
    NUnit tests. See `TestTypeEnum` in `build.gradle` for available options.
*   `INCLUDE_TEST_MODULES` - Default to empty string, which means to include the
    tests for every modules. To narrow down modules to test, set this properties
    with a list of case-insensitive module strings separated by comma. For
    instance, `-PINCLUDE_TEST_MODULES="Tool,AndroidResolver"` means to run tests
    for tools and Android Resolver only. See `TestModuleEnum` in `build.gradle`
    for available options.
*   `EXCLUDE_TEST_MODULES` - Default to empty string, which means to exclude
    none. To add modules to exclude, set this properties with a list of
    case-insensitive module strings separated by comma. For instance,
    `-PEXCLUDE_TEST_MODULES="Tool,AndroidResolver"` means to run tests for any
    modules other than tools and Android Resolver. See `TestModuleEnum` in
    `build.gradle` for available options.
*   `EXCLUDE_TESTS` - Default to empty string, which means to exclude none. To
    add tests to exclude, set this properties with a list of case-insensitive
    test names separated by comma. For instance,
    `-PEXCLUDE_TESTS="testGenGuids,testDownloadArtifacts"` means to run tests
    except the tests with name of `testGenGuids` and `testDownloadArtifacts`.
*   `CONTINUE_ON_FAIL_FOR_TESTS_ENABLED` - Default to `1`. Set to `1` to
    continue running the next test when the current one fails. Otherwise, the
    build script stops whenever any test fails.

For instance, by running the following command, it only runs the Unity
integration tests that does not requires GPU, but exclude tests for Android
Resolver module and iOS Resolver module.

```shell
./gradlew test \
  -PINTERACTIVE_MODE_TESTS_ENABLED=0 \
  -PINCLUDE_TEST_TYPES="Integration" \
  -PEXCLUDE_TEST_MODULES="AndroidResolver,iOSResolver"
```

## Releasing

Each time a new build of this plugin is checked into the source tree you need to
do the following:

*   Bump the plugin version variable `pluginVersion` in `build.gradle`
*   Update `CHANGELOG.md` with the new version number and changes included in
    the release.
*   Build the release using `./gradlew release` which performs the following:
    *   Updates `external-dependency-manager-*.unitypackage`
    *   Copies the unpacked plugin to the `exploded` directory.
    *   Updates template metadata files in the `plugin` directory. The GUIDs of
        all asset metadata is modified due to the version number change. Each
        file within the plugin is versioned to allow multiple versions of the
        plugin to be imported into a Unity project which allows the most recent
        version to be activated by the Version Handler component.
*   Create release commit using `./gradlew gitCreateReleaseCommit` which
    performs `git commit -a -m "description from CHANGELOG.md"`
*   Once the release commit is merge, tag the release using `./gradlew
    gitTagRelease` which performs the following:
    *   `git tag -a pluginVersion -m "version RELEASE"` to tag the release.
*   Update tags on remote branch using `git push --tag REMOTE HEAD:master`
