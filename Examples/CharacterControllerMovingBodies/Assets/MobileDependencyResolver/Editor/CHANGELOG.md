# Version 1.2.185 - Feb 3, 2025
* Android Resolver - Reverse conditional checker for `packaging` keyword in maintemplate based on android gradle plugin version. Fixes #715

# Version 1.2.184 - Jan 28, 2025
* Android Resolver - Update and resolve `packaging` keyword in maintemplate
  based on android gradle plugin version.
  Fixes #715

# Version 1.2.183 - Sep 18, 2024
* Android Resolver - Handle package paths that don't include a version hash,
  which is no longer present with Unity 6. Fixes #697
* Android Resolver - Handle packages referenced using local file paths.
  Fixes #701

# Version 1.2.182 - Aug 2, 2024
* General - Check for gradle version instead of Unity version when determining
  the template files to modify.

# Version 1.2.181 - Jun 26, 2024
* General - Disable `EditorMeasurement` reporting that relied on the
  Measurement Protocol for Universal Analytics.

# Version 1.2.180 - Jun 4, 2024
* General - Fix project settings resetting on domain reload.
  Fixes #524

# Version 1.2.179 - Feb 12, 2024
* Android Resolver - Added logic to automatically turn on `mainTemplate.gradle`
  for new projects, and prompt users to enable it on projects that have previously
  had the resolver run.

# Version 1.2.178 - Dec 20, 2023
* Added [OpenUPM support](https://openupm.com/packages/com.google.external-dependency-manager/).

# Version 1.2.177 - Aug 14, 2023
* iOS Resolver - Added `/opt/homebrew/bin` to Cocoapod executable search path.
  Fixes #627

# Version 1.2.176 - Apr 27, 2023
* Android Resolver - Added two Android Resolver settings to determine whether
  EDM4U injects custom local Maven repo path as a relative path or full path.
  Fixes #537
* Android Resolver - Inject Maven Repo to `settingTemplate.gradle` from
  Unity 2022.2+
  Fixes #594
* Android Resolver - Jetifier option is enabled by default now.
* Android Resolver - `Explode Aar` option applies to all cases, whether the
  project will be exported or not.
  Fixes #584
  Fixes #287

# Version 1.2.175 - Nov 16, 2022
* General - Added tvOS podfile support to the iOS resolver.

# Version 1.2.174 - Oct 06, 2022
* General - Added tvOS support to the iOS resolver.
* General - Fixed #484 - Changed `EditorMeasurement` to use secure connection.
* Android Resolver - Fixed Android Resolver unable to resolve
  `mainTemplate.gradle` in Unity `2022.2+` or `2023.1+`.

# Version 1.2.173 - Sep 28, 2022
* General - Added tvOS library support to the export unity package scripts.

# Version 1.2.172 - Jun 23, 2022
* iOS Resolver - Stop forcing `ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES` to `YES`,
  which seems to cause problem for some when submitting apps. See #526 for more
  information.

# Version 1.2.171 - May 11, 2022
* iOS Resolver - Change `Enable Swift Framework Support Workaround` setting to
  be `ON` by default since more pods are using Swift Framework now.

# Version 1.2.170 - Apr 4, 2022
* Android Resolver - Fixes #498 - Fix the path separator of the Maven repo
  injected to `mainTemplate.gradle`.
* iOS Resolver - Fixes #470 - Switch default Cocoapods master repo from Github
  to CDN.
* iOS Resolver - `Link Framework Statically` setting is now default to `true`.
  That is, `use_frameworks! :linkage => static` will be added to `Podfile` by
  default instead of `use_frameworks!`. This can be changed in iOS Resolver
  settings. This fixes odd behaviors when pods include static libraries, ex.
  Firebase Analytics.
* iOS Resolver - Added a workaround when app crashes on launch due to
  `Library not loaded: @rpath/libswiftCore.dylib` when some pods includes Swift
  framework. This is turned `OFF` by default and can be changed in iOS Resolver
  settings.

# Version 1.2.169 - Jan 20, 2022
* General - Fixes #425 - Change to save `GvhProjectSettings.xml` without
  Unicode byte order mark (BoM).
* Android Resolver - Remove reference to `jcenter()`
* iOS Resolver - Force setting `LANG` when executing Cocoapods in shell mode on
  Mac.

# Version 1.2.168 - Dec 9, 2021
* All - Fixes #472 by removing the use of `System.Diagnostics.Debug.Assert`
* All - Fixed #477 by properly enabling EDM4U libraries for Unity 2021.2+ when
  the package is installed through `.tgz`

# Version 1.2.167 - Oct 6, 2021
* All - Moved versioned `.dll` in EDM4U to a versioned folder and remove their
  version postfix in their filename. For instance, `IOSResolver.dll` will be
  placed at `ExternalDependencyManager/Editor/1.2.167/Google.IOSResolver.dll`.
* Android Resolver - Fixed #243 by only using the highest version in
  `mainTemplate.gradle` when duplicated dependencies are presented.
* Android Resolver - Added supports to x86_64 to ABI list for Android apps on
  Chrome OS.

# Version 1.2.166 - Jun 30, 2021
* All - Fixed #440 and fixed #447 by specifying the parameter type while calling
  `GetApplicationIdentifier()` Unity API using reflection, due to a new
  overloaded method introduced in Unity 2021.2.
* Android Resolver - Fixed #442 by patching `Dependency.IsGreater()` when the
  version strings end '+'.

# Version 1.2.165 - Apr 28, 2021
## Bug Fixes
* Version Handler - Fixed #431 by replacing the use of `HttpUtility.UrlEncode()`
  which causes NullReferenceException in certain version of Unity.
* Android Resolver - Check that androidSdkRootPath directory exists before using
  as sdkPath.
* Android Resolver - Fixed Android Resolver integration tests with Unity
  2019.3+.

# Version 1.2.164 - Feb 4, 2021
## New Features
* Android Resolver - Added support for Android packages with classifier in their
  namespaces.
* iOS Resolver - Added new settings in iOS Resolver to configure generated
  Podfile.
* iOS Resolver - Added a new attribute `addToAllTargets` in Dependencies.xml.

## Bug Fixes
* iOS Resolver - Fixed XML parsing for `bitcodeEnabled` attribute in
  Dependencies.xml.

# Version 1.2.163 - Dec 15, 2020
## Bug Fixes
* Version Handler - Fixed measurement reporting

# Version 1.2.162 - Nov 19, 2020
## Bug Fixes
* Version Handler - Improved #413 by preventing Version Handler from running
  from static constructor when it is disabled.
* Package Manager Resolver - Remove GPR

# Version 1.2.161 - Oct 12, 2020
## Bug Fixes
* Android Resolver - Fixed the issue that Android Resolver does not resolve
  again before build in Unity 2020 if it failed to resolve previously.

# Version 1.2.160 - Sep 30, 2020
## Bug Fixes
* Android Resolver - Fixed a regression that gradleResolver can be null until
  Initialize() is called.
* Android Resolver - Fixed a regression that Android Resolver failed in Unity
  2019.3+ due to `gradleTemplate.properties` not enabled when
  `mainTemplate.gradle` is not enabled at all.

# Version 1.2.159 - Sep 11, 2020
## Bug Fixes
* Android Resolver - Fixed #322 where the Unity editor will lose its target SDK
  setting between Unity restarts if `>28` is selected in 2019.  This is due to
  Unity AndroidSdkVersions enum does not contain values above 28.
* Android Resolver - Fixed #360 where building Android app with Untiy 2019.3+
  may fail due to Jetifier and AndroidX not enabled properly in generated
  Gradle project. This fix requires the user to enable
  `Custom Gradle Properties Template` found under
  `Player Settings > Settings for Android > Publishing Settings`.

# Version 1.2.158 - Sep 3, 2020
## Bug Fixes
* Version Handler: Fixed editor freeze when `-executeMethod` is used in
  non-batch mode.
* Android Resolver: Normalized file paths when generating local Maven repo
  since the path may contains a mix of forward and backward slash on Windows.
* Export Unity Package: Fixed generation of .unitypackage with tarfile on
  Windows.

# Version 1.2.157 - Aug 6, 2020
## Bug Fixes
* Android Resolver: Delay initialization until active build target is Android
  and the editor is not in play mode.
* iOS Resolver: Delay initialization until active build target is iOS
  and the editor is not in play mode.
* Export Unity Package: Workaround directory creation racy if multiple export
  operations are spawned at the same time.

# Version 1.2.156 - June 10, 2020
## Bug Fixes
* Android Resolver: Fixed that the generated local repo assets contains
  redundent labels which are causing Version Handler to failed while
  uninstalling packages.
* Android Resolver: Fixed that the repo url injected into mainTemplate.gradle
  is incorrect when Unity is configured to export gradle project.
* Android Resolver: Limited to only create local Maven repo when the source
  repo contains ".srcaar" file.

## Changes
* All: Described EDM4U analytics data usage in readme.

# Version 1.2.155 - May 14, 2020
## Bug Fixes
* All: Fixed compiler error when build with Unity 5.4 or below due to the
  usage of Rect.zero.
* All: Ignore cases when checking command line arguments.

# Version 1.2.154 - May 14, 2020
## Bug Fixes
* All: Make each MultiSelectWindow for different purposes to have its own
  unique window.

## Changes
* All: Replace all dialog with DialogWindow which is implemented from
  EditorWindow.
* Package Manager Resolver: Clarify how manifest.json will be changed in Package
  Manager Resolver window.

# Version 1.2.153 - Apr 24, 2020
## Bug Fixes
* Android Resolver: Fixed an exception when repainting the Android resolution
  window in Unity 2019.3.x.

# Version 1.2.152 - Apr 17, 2020
## Bug Fixes
* Version Handler: Fixed exception when waiting for enabled editor DLLs to
  load.
* Android Resolver: Fixed regression when using a Custom Gradle Template
  on Windows.

# Version 1.2.151 - Apr 16, 2020
## Bug Fixes
* Version Handler: When waiting for newly enabled editor DLLs to load, ignore
  all DLLs that do not have a file-system location.
* Android Resolver: Fixed resolution when using a Custom Gradle Template with
  libraries stored in a local maven repository distributed with a plugin
  installed with the Unity Package Manager.

# Version 1.2.150 - Apr 9, 2020
## Bug Fixes
* All: The new packaging script when run on MacOS was generating a
  .unitypackage archive that could not be read by Unity on Windows.
  This release simply repackages the plugin with tar/gzip to fix the problem.

# Version 1.2.149 - Apr 8, 2020
## Bug Fixes
* Package Manager Resolver: Fixed spurious error message when resuming
  migration after installing a UPM package.

# Version 1.2.148 - Apr 8, 2020
## Bug Fixes
* Package Manager Resolver: Fixed an exception when resuming migration
  after installing a UPM package.

# Version 1.2.147 - Apr 8, 2020
## Bug Fixes
* Version Handler: Fixed alias traversal bug which caused problems when
  migrating from installed .unitypackage files to UPM packages.

# Version 1.2.146 - Apr 8, 2020
## Bug Fixes
* Version Handler: Fixed exception in manifest parsing when a manifest is
  detected with no aliases.

# Version 1.2.145 - Apr 2, 2020
## New Features
* Package Manager Resolver: Added a method to migrate Version Handler
  managed packages installed via `.unitypackage` to Unity Package Manager
  packages. This is initially used to migrate the External Dependency Manager
  to UPM.

## Changes
* All: Verbose logging is now no longer automatically enabled in batch mode
  across all components. Instead logging can be configured using each
  component's verbose logging setting or by using the `-gvh_log_debug` command
  line flag when starting Unity.
* Version Handler: Sped up version handler updates when the app domain isn't
  reloaded.

## Bug Fixes
* Version Handler: Fixed the display of the obsolete files clean up dialog
  when the asset database refreshes.
* Version Handler: Improved reliability of callback from
  the VersionHandler.UpdateCompleteMethods event when an asset database
  refresh occurs.
* Version Handler: Fixed duplicate exportPath labels when 'Assets/' is the
  root of paths assigned to files.
* Version Handler: Handle empty lines in manifest files.

# Version 1.2.144 - Mar 23, 2020
## Changed
* iOS Resolver: Removed the ability to configure the Xcode target a Cocoapod
  is added to.

## Bug Fixes
* iOS Resolver: Reverted support for adding Cocoapods to multiple targets as
  it caused a regression (exception thrown during post-build step) in some
  versions of Unity.

# Version 1.2.143 - Mar 20, 2020
## Bug Fixes
* Android Resolver: Fixed caching of resolution state which was causing
  the resolver to always run when no dependencies had changed.

# Version 1.2.142 - Mar 19, 2020
## Changes
* Package Manager Resolver: Enabled auto-add by default.

# Version 1.2.141 - Mar 19, 2020
## Bug Fixes
* Fixed a bug when retrieving project settings. If a plugin was configured
  to fetch project settings, if a setting was fetched (e.g "foo") and this
  setting existed in the system settings but not the project settings the
  system value would override the default value leading to unexpected
  behavior.
* Fixed a warning when caching web request classes in Unity 5.6.

# Version 1.2.140 - Mar 19, 2020
## Bug Fixes
* Fixed measurement reporting in Unity 5.x.
* Version Handler: Fixed NullReferenceException when an asset doesn't have
  an AssetImporter.

# Version 1.2.139 - Mar 18, 2020
## Changed
* Added documentation to the built plugin.

# Version 1.2.138 - Mar 17, 2020
## New Features
* Package Manager Resolver: Added the Package Manager Resolver
  component that allows developers to easily boostrap Unity Package Manager
  (UPM) registry addition using unitypackage plugins.
* Version Handler: Added a window that allows plugins to managed by the
  Version Handler to be uninstalled.
* Version Handler: Added support for displaying installed plugins.
* Version Handler: Added support for moving files in plugins to their install
  locations (if the plugin has been configured to support this).
* iOS Resolver: Added the ability to configure the Xcode target a Cocoapod is
  added to.

## Bug Fixes
* Fixed upgrade from version 1.2.137 and below after the plugin rename to
  EDM4U broke the upgrade process.
* Android Resolver: Worked around PlayerSettings.Android.targetSdkVersion
  returning empty names for some values in 2019.x.
* Version Handler: Fixed the display of the obsolete files clean up window.
* Version Handler: Fixed managed file check when assets are modified in the
  project after plugin import.

# Version 1.2.137 - Mar 6, 2020
## Changed
* Renamed package to External Package Manager for Unity (EDM4U).
  We changed this to reflect what this plugin is doing today which is far more
  than the original scope which just consisted of importing jar files from the
  Android SDK maven repository.
  Scripts that used to pull `play-services-resolver*.unitypackage` will now have
  to request `external-dependency-manager*.unitypackage` instead.
  We'll still be shipping a `play-services-resolver*_manifest.txt` file to
  handle upgrading from older versions of the plugin.

## New Features
* All Components: Added reporting of usage so that we can remotely detect
  errors and target improvements.
* Android Resolver: Added support for *Dependencies.xml files in Unity Package
  Manager packages.
* iOS Resolver: Added support for *Dependencies.xml files in Unity Package
  Manager packages.

## Bug Fixes
* Version Handler: Disabled attempts to disable asset metadata modification
  when assets are in a Unity Package Manager managed package.

# Version 1.2.136 - Feb 19, 2019
## Bug Fixes
* Android Resolver: Fixed OpenJDK path discovery in Unity 2019.3.1.

# Version 1.2.135 - Dec 5, 2019
## Bug Fixes
* All Components: Fixed stack overflow when loading project settings.

# Version 1.2.134 - Dec 4, 2019
## Bug Fixes
* All Components: Fixed an issue which caused project settings to be cleared
  when running in batch mode.

# Version 1.2.133 - Nov 18, 2019
## Bug Fixes
* All Components: Failure to save project settings will now report an error
  to the log rather than throwing an exception.

# Version 1.2.132 - Nov 11, 2019
## Bug Fixes
* Android Resolver: Worked around expansion of DIR_UNITYPROJECT on Windows
  breaking Gradle builds when used as part of a file URI.
* Android Resolver: mainTemplate.gradle is only written if it needs to be
  modified.

# Version 1.2.131 - Oct 29, 2019
## Bug Fixes
* Version Handler: Improved execution of events on the main thread in batch
  mode.
* Version Handler: Improved log level configuration at startup.
* Version Handler: Improved performance of class lookup in deferred method
  calls.
* Version Handler: Fixed rename to enable / disable for editor assets.
* iOS Resolver: Improved log level configuration at startup.
* Android Resolver: Improved local maven repo path reference in
  mainTemplate.gradle using DIR_UNITYPROJECT.  DIR_UNITYPROJECT by Unity
  to point to the local filesystem path of the Unity project when Unity
  generates the Gradle project.

# Version 1.2.130 - Oct 23, 2019
## New Features
* iOS Resolver: Added support for modifying the Podfile before `pod install`
  is executed.

## Bug Fixes
* Version Handler: Fixed invalid classname error when calling
  `VersionHandler.UpdateVersionedAssets()`.

# Version 1.2.129 - Oct 2, 2019
## Bug Fixes
* iOS Resolver: Changed Cocoapod integration in Unity 2019.3+ to
  only add Pods to the UnityFramework target.

# Version 1.2.128 - Oct 1, 2019
## Bug Fixes
* iOS Resolver: Fixed Cocoapod project integration mode with Unity
  2019.3+.

# Version 1.2.127 - Sep 30, 2019
## Changes
* Android Resolver: All Android Resolver settings File paths are now
  serialized with POSIX directory separators.

# Version 1.2.126 - Sep 27, 2019
## Changes
* Android Resolver: File paths are now serialized with POSIX directory
  separators.
## Bug Fixes
* Android Resolver: Fixed resolution when the parent directory of a Unity
  project contains a Gradle project (i.e `settings.gradle` file).

# Version 1.2.125 - Sep 23, 2019
## Bug Fixes
* All components: Silenced a warning about not being able to set the console
  encoding to UTF8.
* Android Resolver: Worked around broken AndroidSDKTools class in some
  versions of Unity.
* iOS Resolver: Fixed iOS target SDK version check
* Version Handler: Changed clean up obsolete files window so that it doesn't
  exceed the screen size.

# Version 1.2.124 - Jul 28, 2019
## Bug Fixes
* All components: Fixed regression with source control integration when using
  Unity 2019.1+.

# Version 1.2.123 - Jul 23, 2019
## New Features
* All components: Source control integration for project settings.
## Changes
* Android Resolver: Removed AAR cache as it now makes little difference to
  incremental resolution performance.
* Android Resolver: Improved embedded resource management so that embedded
  resources should upgrade when the plugin is updated without restarting
  the Unity editor.
## Bug Fixes
* Version Handler: Fixed InvokeMethod() and InvokeStaticMethod() when calling
  methods that have interface typed arguments.

# Version 1.2.122 - Jul 2, 2019
## Bug Fixes
* iOS Resolver: Worked around Unity not loading the iOS Resolver DLL as it
  referenced the Xcode extension in a public interface.  The iOS Resolver
  DLL still references the Xcode extension internally and just handles
  missing type exceptions dynamically.

# Version 1.2.121 - Jun 27, 2019
## Bug Fixes
* Android Resolver: Fixed warning about missing Packages folder when loading
  XML dependencies files in versions of Unity without the package manager.
* Android Resolver: Fixed resolution window progress bar exceeding 100%.
* Android Resolver: If AndroidX is detected in the set of resolved libraries,
  the user will be prompted to enable the Jetifier.
* Android Resolver: Improved text splitting in text area windows.
* iOS Resolver: Added support for Unity's breaking changes to the Xcode API
  in 2019.3.+. Cocoapods are now added to build targets, Unity-iPhone and
  UnityFramework in Unity 2019.3+.

# Version 1.2.120 - Jun 26, 2019
## New Features
* Android Resolver: Added support for loading *Dependencies.xml files from
  Unity Package Manager packages.
* Android Resolver: Resolution window is now closed if resolution runs as
  a pre-build step.
* iOS Resolver: Added support for loading *Dependencies.xml files from
  Unity Package Manager packages.
## Bug Fixes
* Android Resolver: Fixed generation of relative repo paths when using
  mainTemplate.gradle resolver.
* Android Resolver: Fixed copy of .srcaar to .aar files in repos embedded in a
  project when a project path has characters (e.g whitespace) that are escaped
  during conversion to URIs.
* Android Resolver: Fixed auto-resolution always running if the Android SDK
  is managed by Unity Hub.

# Version 1.2.119 - Jun 19, 2019
## Bug Fixes
* Android Resolver: Fixed error reported when using Jetifier integration
  in Unity 2018+ if the target SDK is set to "highest installed".

# Version 1.2.118 - Jun 18, 2019
## New Features
* Android Resolver: Added initial
  [Jetifier](https://developer.android.com/studio/command-line/jetifier)
  integration which simplifies
  [migration](ttps://developer.android.com/jetpack/androidx/migrate)
  to Jetpack ([AndroidX](https://developer.android.com/jetpack/androidx))
  libraries in cases where all dependencies are managed by the Android
  Resolver.
  This can be enabled via the `Use Jetifier` option in the
  `Assets > Play Services Resolver > Android Resolver > Settings` menu.
  Caveats:
  - If your project contains legacy Android Support Library .jar and .aar
    files, these files will need to be removed and replaced with references to
    artifacts on Maven via `*Dependencies.xml` files so that the Jetifier
    can map them to Jetpack (AndroidX) libraries.
    For example, remove the file `support-v4-27.0.2.jar` and replace it with
    `<androidPackage spec="com.android.support:support-v4:27.0.2"/>` in a
    `*Dependencies.xml` file.
  - If your project contains .jar or .aar files that use the legacy Android
    Support Libraries, these will need to be moved into a local Maven repo
    [See this guide](https://maven.apache.org/guides/mini/guide-3rd-party-jars-local.html)
    and then these files should be removed from your Unity project and instead
    referenced via `*Dependencies.xml` files so that the Jetifier can
    patch them to reference the Jetpack lirbaries.

## Bug Fixes
* Android Resolver: Disabled version locking of com.android.support:multidex
  does not use the same versioning scheme as other legacy Android support
  libraries.
* Version Handler: Made Google.VersionHandler.dll's asset GUID stable across
  releases.  This faciliates error-free import into projects where
  Google.VersionHandler.dll is moved from the default install location.

# Version 1.2.117 - Jun 12, 2019
## Bug Fixes
* Android Resolver: Fix copying of .srcaar to .aar files for
  mainTemplate.gradle resolution.  PluginImporter configuration was previously
  not being applied to .aar files unless the Unity project was saved.

# Version 1.2.116 - Jun 7, 2019
## Bug Fixes
* Android Resolver: Fixed resolution of Android dependencies without version
  specifiers.
* Android Resolver: Fixed Maven repo not found warning in Android Resolver.
* Android Resolver: Fixed Android Player directory not found exception in
  Unity 2019.x when the Android Player isn't installed.

# Version 1.2.115 - May 28, 2019
## Bug Fixes
* Android Resolver: Fixed exception due to Unity 2019.3.0a4 removing
  x86 from the set of supported ABIs.

# Version 1.2.114 - May 27, 2019
## New Features
* Android Resolver: Added support for ABI stripping when using
  mainTemplate.gradle. This only works with AARs stored in repos
  on the local filesystem.

# Version 1.2.113 - May 24, 2019
## New Features
* Android Resolver: If local repos are moved, the plugin will search the
  project for matching directories in an attempt to correct the error.
* Version Handler: Files can be now targeted to multiple build targets
  using multiple "gvh_" asset labels.
## Bug Fixes
* Android Resolver: "implementation" or "compile" are now added correctly
  to mainTemplate.gradle in Unity versions prior to 2019.

# Version 1.2.112 - May 22, 2019
## New Features
* Android Resolver: Added option to disable addition of dependencies to
  mainTemplate.gradle.
  See `Assets > Play Services Resolver > Android Resolver > Settings`.
* Android Resolver: Made paths to local maven repositories in
  mainTemplate.gradle relative to the Unity project when a project is not
  being exported.
## Bug Fixes
* Android Resolver: Fixed builds with mainTemplate.gradle integration in
  Unity 2019.
* Android Resolver: Changed dependency inclusion in mainTemplate.gradle to
  use "implementation" or "compile" depending upon the version of Gradle
  included with Unity.
* Android Resolver: Gracefully handled exceptions if the console encoding
  can't be modified.
* Android Resolver: Now gracefully fails if the AndroidPlayer directory
  can't be found.

# Version 1.2.111 - May 9, 2019
## Bug Fixes
* Version Handler: Fixed invocation of methods with named arguments.
* Version Handler: Fixed occasional hang when the editor is compiling
  while activating plugins.

# Version 1.2.110 - May 7, 2019
## Bug Fixes
* Android Resolver: Fixed inclusion of some srcaar artifacts in builds with
  Gradle builds when using mainTemplate.gradle.

# Version 1.2.109 - May 6, 2019
## New Features:
* Added links to documentation from menu.
* Android Resolver: Added option to auto-resolve Android libraries on build.
* Android Resolver: Added support for packaging specs of Android libraries.
* Android Resolver: Pop up a window when displaying Android dependencies.

## Bug Fixes
* Android Resolver: Support for Unity 2019 Android SDK and JDK install locations
* Android Resolver: e-enable AAR explosion if internal builds are enabled.
* Android Resolver: Gracefully handle exceptions on file deletion.
* Android Resolver: Fixed Android Resolver log spam on load.
* Android Resolver: Fixed save of Android Resolver PromptBeforeAutoResolution
  setting.
* Android Resolver: Fixed AAR processing failure when an AAR without
  classes.jar is found.
* Android Resolver: Removed use of EditorUtility.DisplayProgressBar which
  was occasionally left displayed when resolution had completed.
* Version Handler: Fixed asset rename to disable when a disabled file exists.

# Version 1.2.108 - May 3, 2019
## Bug Fixes:
* Version Handler: Fixed occasional hang on startup.

# Version 1.2.107 - May 3, 2019
## New Features:
* Version Handler: Added support for enabling / disabling assets that do not
  support the PluginImporter, based upon build target selection.
* Android Resolver: Added support for the global specification of maven repos.
* iOS Resolver: Added support for the global specification of Cocoapod sources.

# Version 1.2.106 - May 1, 2019
## New Features
* iOS Resolver: Added support for development pods in Xcode project integration
  mode.
* iOS Resolver: Added support for source pods with resources in Xcode project
  integration mode.

# Version 1.2.105 - Apr 30, 2019
## Bug fixes
* Android Resolver: Fixed reference to Java tool path in logs.
* Android and iOS Resolvers: Changed command line execution to emit a warning
  rather than throwing an exception and failing, when it is not possible to
  change the console input and output encoding to UTF-8.
* Android Resolver: Added menu option and API to delete resolved libraries.
* Android Resolver: Added menu option and API to log the repos and libraries
  currently included in the project.
* Android Resolver: If Plugins/Android/mainTemplate.gradle file is present and
  Gradle is selected as the build type, resolution will simply patch the file
  with Android dependencies specified by plugins in the project.

# Version 1.2.104 - Apr 10, 2019
## Bug Fixes
* Android Resolver: Changed Android ABI selection method from using whitelisted
  Unity versions to type availability.  This fixes an exception on resolution
  in some versions of Unity 2017.4.

# Version 1.2.103 - Apr 2, 2019
## Bug Fixes
* Android Resolver: Whitelisted Unity 2017.4 and above with ARM64 support.
* Android Resolver: Fixed Java version check to work with Java SE 12 and above.

# Version 1.2.102 - Feb 13, 2019
## Bug Fixes
* Android Resolver: Fixed the text overflow on the Android Resolver
  prompt before initial run to fit inside the buttons for
  smaller screens.

# Version 1.2.101 - Feb 12, 2019
## New Features
* Android Resolver: Prompt the user before the resolver runs for the
  first time and allow the user to elect to disable from the prompt.
* Android Resolver: Change popup warning when resolver is disabled
  to be a console warning.

# Version 1.2.100 - Jan 25, 2019
## Bug Fixes
* Android Resolver: Fixed AAR processing sometimes failing on Windows
  due to file permissions.

# Version 1.2.99 - Jan 23, 2019
## Bug Fixes
* Android Resolver: Improved performance of project property polling.
* Version Handler: Fixed callback of VersionHandler.UpdateCompleteMethods
  when the update process is complete.

# Version 1.2.98 - Jan 9, 2019
## New Features
* iOS Resolver: Pod declaration properties can now be set via XML pod
  references.  For example, this can enable pods for a subset of build
  configurations.
## Bug Fixes
* iOS Resolver: Fixed incremental builds after local pods support caused
  regression in 1.2.96.

# Version 1.2.97 - Dec 17, 2018
## Bug Fixes
* Android Resolver: Reduced memory allocation for logic that monitors build
  settings when auto-resolution is enabled.  If auto-resolution is disabled,
  almost all build settings are no longer polled for changes.

# Version 1.2.96 - Dec 17, 2018
## Bug Fixes
* Android Resolver: Fixed repacking of AARs to exclude .meta files.
* Android Resolver: Only perform auto-resolution on the first scene while
  building.
* Android Resolver: Fixed parsing of version ranges that include whitespace.
* iOS Resolver: Added support for local development pods.
* Version Handler: Fixed Version Handler failing to rename some files.

# Version 1.2.95 - Oct 23, 2018
## Bug Fixes:
* Android Resolver: Fixed auto-resolution running in a loop in some scenarios.

# Version 1.2.94 - Oct 22, 2018
## Bug Fixes
* iOS Resolver: Added support for PODS_TARGET_SRCROOT in source Cocoapods.

# Version 1.2.93 - Oct 22, 2018
## Bug Fixes
* Android Resolver: Fixed removal of Android libraries on auto-resolution when
  `*Dependencies.xml` files are deleted.

# Version 1.2.92 - Oct 2, 2018
## Bug Fixes
* Android Resolver: Worked around auto-resolution hang on Windows if
  resolution starts before compilation is finished.

# Version 1.2.91 - Sep 27, 2018
## Bug Fixes
* Android Resolver: Fixed Android Resolution when the selected build target
  isn't Android.
* Added C# assembly symbols the plugin to simplify debugging bug reports.

# Version 1.2.90 - Sep 21, 2018
## Bug Fixes
* Android Resolver: Fixed transitive dependency selection of version locked
  packages.

# Version 1.2.89 - Aug 31, 2018
## Bug Fixes
* Fixed FileLoadException in ResolveUnityEditoriOSXcodeExtension an assembly
  can't be loaded.

# Version 1.2.88 - Aug 29, 2018
## Changed
* Improved reporting of resolution attempts and conflicts found in the Android
  Resolver.
## Bug Fixes
* iOS Resolver now correctly handles sample code in CocoaPods.  Previously it
  would add all sample code to the project when using project level
  integration.
* Android Resolver now correctly handles Gradle conflict resolution when the
  resolution results in a package that is compatible with all requested
  dependencies.

# Version 1.2.87 - Aug 23, 2018
## Bug Fixes
* Fixed Android Resolver "Processing AARs" dialog getting stuck in Unity 5.6.

# Version 1.2.86 - Aug 22, 2018
## Bug Fixes
* Fixed Android Resolver exception in OnPostProcessScene() when the Android
  platform isn't selected.

# Version 1.2.85 - Aug 17, 2018
## Changes
* Added support for synchronous resolution in the Android Resolver.
  PlayServicesResolver.ResolveSync() now performs resolution synchronously.
* Auto-resolution in the Android Resolver now results in synchronous resolution
  of Android dependencies before the Android application build starts via
  UnityEditor.Callbacks.PostProcessSceneAttribute.

# Version 1.2.84 - Aug 16, 2018
## Bug Fixes
* Fixed Android Resolver crash when the AndroidResolverDependencies.xml
  file can't be written.
* Reduced log spam when a conflicting Android library is pinned to a
  specific version.

# Version 1.2.83 - Aug 15, 2018
## Bug Fixes
* Fixed Android Resolver failures due to an in-accessible AAR / JAR explode
  cache file.  If the cache can't be read / written the resolver now continues
  with reduced performance following recompilation / DLL reloads.
* Fixed incorrect version number in plugin manifest on install.
  This was a minor issue since the version handler rewrote the metadata
  after installation.

# Version 1.2.82 - Aug 14, 2018
## Changed
* Added support for alphanumeric versions in the Android Resolver.

## Bug Fixes
* Fixed Android Resolver selection of latest duplicated library.
* Fixed Android Resolver conflict resolution when version locked and non-version
  locked dependencies are specified.
* Fixed Android Resolver conflict resolution when non-existent artifacts are
  referenced.

# Version 1.2.81 - Aug 9, 2018
## Bug Fixes
* Fixed editor error that would occur when when
  `PlayerSettings.Android.targetArchitectures` was set to
  `AndroidArchitecture.All`.

# Version 1.2.80 - Jul 24, 2018
## Bug Fixes
* Fixed project level settings incorrectly falling back to system wide settings
  when default property values were set.

# Version 1.2.79 - Jul 23, 2018
## Bug Fixes
* Fixed AndroidManifest.xml patching on Android Resolver load in Unity 2018.x.

# Version 1.2.78 - Jul 19, 2018
## Changed
* Added support for overriding conflicting dependencies.

# Version 1.2.77 - Jul 19, 2018
## Changed
* Android Resolver now supports Unity's 2018 ABI filter (i.e arm64-v8a).
* Reduced Android Resolver build option polling frequency.
* Disabled Android Resolver auto-resolution in batch mode.  Users now need
  to explicitly kick off resolution through the API.
* All Android Resolver and Version Handler dialogs are now disabled in batch
  mode.
* Verbose logging for all plugins is now enabled by default in batch mode.
* Version Handler bootstrapper has been improved to no longer call
  UpdateComplete multiple times.  However, since Unity can still reload the
  app domain after plugins have been enabled, users still need to store their
  plugin state to persistent storage to handle reloads.

## Bug Fixes
* Android Resolver no longer incorrectly adds MANIFEST.MF files to AARs.
* Android Resolver auto-resolution jobs are now unscheduled when an explicit
  resolve job is started.

# Version 1.2.76 - Jul 16, 2018
## Bug Fixes
* Fixed variable replacement in AndroidManifest.xml files in the Android
  Resolver.
  Version 1.2.75 introduced a regression which caused all variable replacement
  to replace the *entire* property value rather than the component of the
  property that referenced a variable.  For example,
  given "applicationId = com.my.app", "${applicationId}.foo" would be
  incorrectly expanded as "com.my.app" rather than "com.my.app.foo".  This
  resulted in numerous issues for Android builds where content provider
  initialization would fail and services may not start.

## Changed
* Gradle prebuild experimental feature has been removed from the Android
  Resolver.  The feature has been broken for some time and added around 8MB
  to the plugin size.
* Added better support for execution of plugin components in batch mode.
  In batch mode UnityEditor.update is sometimes never called - like when a
  single method is executed - so the new job scheduler will execute all jobs
  synchronously from the main thread.

# Version 1.2.75 - Jun 20, 2018
## New Features
* Android Resolver now monitors the Android SDK path when
  auto-resolution is enabled and triggers resolution when the path is
  modified.

## Changed
* Android auto-resolution is now delayed by 3 seconds when the following build
  settings are changed:
  - Target ABI.
  - Gradle build vs. internal build.
  - Project export.
* Added a progress bar display when AARs are being processed during Android
  resolution.

## Bug Fixes
* Fixed incorrect Android package version selection when a mix of
  version-locked and non-version-locked packages are specified.
* Fixed non-deterministic Android package version selection to select
  the highest version of a specified package rather than the last
  package specification passed to the Gradle resolution script.

# Version 1.2.74 - Jun 19, 2018
## New Features
* Added workaround for broken AndroidManifest.xml variable replacement in
  Unity 2018.x.  By default ${applicationId} variables will be replaced by
  the bundle ID in the Plugins/Android/AndroidManifest.xml file.  The
  behavior can be disabled via the Android Resolver settings menu.

# Version 1.2.73 - May 30, 2018
## Bug Fixes
* Fixed spurious warning message about missing Android plugins directory on
  Windows.

# Version 1.2.72 - May 23, 2018
## Bug Fixes
* Fixed spurious warning message about missing Android plugins directory.

# Version 1.2.71 - May 10, 2018
## Bug Fixes
* Fixed resolution of Android dependencies when the `Assets/Plugins/Android`
  directory is named in a different case e.g `Assets/plugins/Android`.

# Version 1.2.70 - May 7, 2018
## Bug Fixes
* Fixed bitcode flag being ignored for iOS pods.

# Version 1.2.69 - May 7, 2018
## Bug Fixes
* Fixed escaping of local repository paths in Android Resolver.

# Version 1.2.68 - May 3, 2018
## Changes
* Added support for granular builds of Google Play Services.

# Version 1.2.67 - May 1, 2018
## Changes
* Improved support for iOS source-only pods in Unity 5.5 and below.

# Version 1.2.66 - April 27, 2018
## Bug Fixes
* Fixed Version Handler renaming of Linux libraries with hyphens in filenames.
  Previously, libraries named Foo-1.2.3.so were not being renamed to
  libFoo-1.2.3.so on Linux which could break native library loading on some
  versions of Unity.

# Version 1.2.65 - April 26, 2018
## Bug Fixes
* Fix CocoaPods casing in logs and comments.

# Version 1.2.64 - Mar 16, 2018
## Bug Fixes
* Fixed bug in download_artifacts.gradle (used by Android Resolver) which
  reported a failure if required artifacts already exist.

# Version 1.2.63 - Mar 15, 2018
## Bug Fixes
* Fixed iOS Resolver include search paths taking precedence over system headers
  when using project level resolution.
* Fixed iOS Resolver includes relative to library root, when using project level
  resolution.

# Version 1.2.62 - Mar 12, 2018
## Changes
* Improved error reporting when a file can't be moved to trash by the
  Version Handler.
## Bug Fixes
* Fixed Android Resolver throwing NullReferenceException when the Android SDK
  path isn't set.
* Fixed Version Handler renaming files with underscores if the
  "Rename to Canonical Filenames" setting is enabled.

# Version 1.2.61 - Jan 22, 2018
## Bug Fixes
* Fixed Android Resolver reporting non-existent conflicting dependencies when
  Gradle build system is enabled.

# Version 1.2.60 - Jan 12, 2018
## Changes
* Added support for Maven / Ivy version specifications for Android packages.
* Added support for Android SNAPSHOT packages.

## Bug Fixes
* Fixed Openjdk version check.
* Fixed non-deterministic Android package resolution when two packages contain
  an artifact with the same name.

# Version 1.2.59 - Oct 19, 2017
## Bug Fixes
* Fixed execution of Android Gradle resolution script when it's located
  in a path with whitespace.

# Version 1.2.58 - Oct 19, 2017
## Changes
* Removed legacy resolution method from Android Resolver.
  It is now only possible to use the Gradle or Gradle prebuild resolution
  methods.

# Version 1.2.57 - Oct 18, 2017
## Bug Fixes
* Updated Gradle wrapper to 4.2.1 to fix issues using Gradle with the
  latest Openjdk.
* Android Gradle resolution now also uses gradle.properties to pass
  parameters to Gradle in an attempt to workaround problems with
  command line argument parsing on Windows 10.

# Version 1.2.56 - Oct 12, 2017
## Bug Fixes
* Fixed Gradle artifact download with non-version locked artifacts.
* Changed iOS resolver to only load dependencies at build time.

# Version 1.2.55 - Oct 4, 2017
## Bug Fixes
* Force Android Resolution when the "Install Android Packages" setting changes.

# Version 1.2.54 - Oct 4, 2017
## Bug Fixes
* Fixed execution of command line tools on Windows when the path to the tool
  contains a single quote (apostrophe).  In this case we fallback to executing
  the tool via the system shell.

# Version 1.2.53 - Oct 2, 2017
## New Features
* Changed Android Resolver "resolution complete" dialog so that it now displays
  failures.
* Android Resolver now detects conflicting libraries that it does not manage
  warning the user if they're newer than the managed libraries and prompting
  the user to clean them up if they're older or at the same version.

## Bug Fixes
* Improved Android Resolver auto-resolution speed.
* Fixed bug in the Gradle Android Resolver which would result in resolution
  succeeding when some dependencies are not found.

# Version 1.2.52 - Sep 25, 2017
## New Features
* Changed Android Resolver's Gradle resolution to resolve conflicting
  dependencies across Google Play services and Android Support library packages.

# Version 1.2.51 - Sep 20, 2017
## Changes
* Changed iOS Resolver to execute the CocoaPods "pod" command via the shell
  by default.  Some developers customize their shell environment to use
  custom ssh certs to access internal git repositories that host pods so
  executing "pod" via the shell will work for these scenarios.
  The drawback of executing "pod" via the shell could potentially cause
  users problems if they break their shell environment.  Though users who
  customize their shell environments will be able to resolve these issues.

# Version 1.2.50 - Sep 18, 2017
## New Features
* Added option to disable the Gradle daemon in the Android Resolver.
  This daemon is now disabled by default as some users are getting into a state
  where multiple daemon instances are being spawned when changing dependencies
  which eventually results in Android resolution failing until all daemon
  processes are manually killed.

## Bug Fixes
* Android resolution is now always executed if the user declines the update
  of their Android SDK.  This ensure users can continue to use out of date
  Android SDK packages if they desire.

# Version 1.2.49 - Sep 18, 2017
## Bug Fixes
* Removed modulemap parsing in iOS Resolver.
  The framework *.modulemap did not need to be parsed by the iOS Resolver
  when injecting Cocoapods into a Xcode project.  Simply adding a modular
  framework to a Xcode project results in Xcode's Clang parsing the associated
  modulemap and injecting any compile and link flags into the build process.

# Version 1.2.48 - Sep 12, 2017
## New Features
* Changed settings to be per-project by default.

## Bug Fixes
* Added Google maven repository to fix GradlePrebuild resolution with Google
  components.
* Fixed Android Resolution failure with spaces in paths.

# Version 1.2.47 - Aug 29, 2017
## New Features
* Android and iOS dependencies can now be specified using *Dependencies.xml
  files.  This is now the preferred method for registering dependencies,
  we may remove the API for dependency addition in future.
* Added "Reset to Defaults" button to each settings dialog to restore default
  settings.
* Android Resolver now validates the configured JDK is new enough to build
  recently released Android libraries.
## Bug Fixes
* Fixed a bug that caused dependencies with the "LATEST" version specification
  to be ignored when using the Gradle mode of the Android Resolver.
* Fixed a race condition when running Android Resolution.
* Fixed Android Resolver logging if a PlayServicesSupport instance is created
  with no logging enabled before the Android Resolver is initialized.
* Fixed iOS resolver dialog in Unity 4.
* Fixed iOS Cocoapod Xcode project integration in Unity 4.

# Version 1.2.46 - Aug 22, 2017
## Bug Fixes
* GradlePrebuild Android resolver on Windows now correctly locates dependent
  data files.

# Version 1.2.45 - Aug 22, 2017
## Bug Fixes
* Improved Android package auto-resolution and fixed clean up of stale
  dependencies when using Gradle dependency resolution.

# Version 1.2.44 - Aug 21, 2017
## Bug Fixes
* Enabled autoresolution for Gradle Prebuild.
* Made the command line dialog windows have selectable text.
* Fixed incorrect "Android Settings" dialog disabled groups.
* Updated PlayServicesResolver android platform detection to use the package
  manager instead of the 'android' tool.
* UnityCompat reflection methods 'GetAndroidPlatform' and
  'GetAndroidBuildToolsVersion' are now Obsolete due to dependence on the
  obsolete 'android' build tool.

# Version 1.2.43 - Aug 18, 2017
## Bug Fixes
* Fixed Gradle resolution in the Android Resolver when running
  PlayServicesResolver.Resolve() in parallel or spawning multiple
  resolutions before the previous resolve completed.

# Version 1.2.42 - Aug 17, 2017
## Bug Fixes
* Fixed Xcode project level settings not being applied by IOS Resolver when
  Xcode project pod integration is enabled.

# Version 1.2.41 - Aug 15, 2017
## Bug Fixes
* IOS Resolver's Xcode workspace pod integration is now disabled when Unity
  Cloud Build is detected.  Unity Cloud Build does not follow the same build
  process as the Unity editor and fails to open the generated xcworkspace at
  this time.

# Version 1.2.40 - Aug 15, 2017
## Bug Fixes
* Moved Android Resolver Gradle Prebuild scripts into Google.JarResolver.dll.
  They are now extracted from the DLL when required.
* AARs / JARs are now cleaned up when switching the Android resolution
  strategy.

# Version 1.2.39 - Aug 10, 2017
## New Features
* Android Resolver now supports resolution with Gradle.  This enables support
  for non-local artifacts.
## Bug Fixes
* Android Resolver's Gradle Prebuild now uses Android build tools to determine
  the Android platform tools version rather than relying upon internal Unity
  APIs.
* Android Resolver's Gradle Prebuild now correctly strips binaries that are
  not required for the target ABI.

# Version 1.2.38 - Aug 7, 2017
## Bug Fixes
* Fixed an issue in VersionHandler where disabled targets are ignored if
  the "Any Platform" flag is set on a plugin DLL.

# Version 1.2.37 - Aug 3, 2017
## New Features
* Exposed GooglePlayServices.PlayServicesResolver.Resolve() so that it's
  possible for a script to be notified when AAR / Jar resolution is complete.
  This makes it easier to setup a project to build from the command line.

# Version 1.2.36 - Aug 3, 2017
## New Features
* VersionHandler.UpdateCompleteMethods allows a user to provide a list of
  methods to be called when VersionHandlerImpl has completed an update.
  This makes it easier to import a plugin and wait for VersionHandler to
  execute prior executing a build.

# Version 1.2.35 - Jul 28, 2017
## New Features
* VersionHandler will now rename Linux libraries so they can target Unity
  versions that require different file naming.  Libraries need to be labelled
  gvh_linuxlibname-${basename} in order to be considered for renaming.
  e.g gvh\_linuxlibname-MyLib will be named MyLib.so in Unity 5.5 and below and
  libMyLib.so in Unity 5.6 and above.

# Version 1.2.34 - Jul 28, 2017
## Bug Fixes
* Made VersionHandler bootstrap module more robust when calling static
  methods before the implementation DLL is loaded.

# Version 1.2.33 - Jul 27, 2017
## New Features
* Added a bootstrap module for VersionHandler so the implementation
  of the VersionHandler module can be versioned without resulting in
  a compile error when imported at different versions across multiple
  plugins.

# Version 1.2.32 - Jul 20, 2017
## New Features
* Added support for build target selection based upon .NET framework
  version in the VersionHandler.
  When applying either gvh\_dotnet-3.5 or gvh\_dotnet-4.5 labels to
  assets, the VersionHandler will only enable the asset for the
  specified set of build targets when the matching .NET framework version
  is selected in Unity 2017's project settings.  This allows assets
  to be provided in a plugin that need to differ based upon .NET version.

# Version 1.2.31 - Jul 5, 2017
## Bug Fixes
* Force expansion of AARs with native components when using Unity 2017
  with the internal build system.  In contrast to Unity 5.x, Unity 2017's
  internal build system does not include native libraries included in AARs.
  Forcing expansion of AARs with native components generates an
  Ant / Eclipse project for each AAR which is correctly included by Unity
  2017's internal build system.

# Version 1.2.30 - Jul 5, 2017
## Bug Fixes
* Fixed Cocoapods being installed when the build target isn't iOS.
* Added support for malformed AARs with missing classes.jar.

# Version 1.2.29 - Jun 16, 2017
## New Features
* Added support for the Android sdkmanager tool.

# Version 1.2.28 - Jun 8, 2017
## Bug Fixes
* Fixed non-shell command line execution (regression from
  Cocoapod installation patch).

# Version 1.2.27 - Jun 7, 2017
## Bug Fixes
* Added support for stdout / stderr redirection when executing
  commands in shell mode.
  This fixes CocoaPod tool installation when shell mode is
  enabled.
* Fixed incremental builds when additional sources are specified
  in the Podfile.

# Version 1.2.26 - Jun 7, 2017
## Bug Fixes
* Fixed a crash when importing Version Handler into Unity 4.7.x.

# Version 1.2.25 - Jun 7, 2017
## Bug Fixes
* Fixed an issue in the Jar Resolver which incorrectly notified
  event handlers of bundle ID changes when the currently selected
  (not active) build target changed in Unity 5.6 and above.

# Version 1.2.24 - Jun 6, 2017
## New Features
* Added option to control file renaming in Version Handler settings.
  Disabling file renaming (default option) significantly increases
  the speed of file version management operations with the downside
  that any files that are referenced directly by canonical filename
  rather than asset ID will no longer be valid.
* Improved logging in the Version Handler.
## Bug Fixes
* Fixed an issue in the Version Handler which caused it to not
  re-enable plugins when re-importing a custom package with disabled
  version managed files.

# Version 1.2.23 - May 26, 2017
## Bug Fixes
* Fixed a bug with gradle prebuild resolver on windows.

# Version 1.2.22 - May 19, 2017
## Bug Fixes
* Fixed a bug in the iOS resolver with incremental builds.
* Fixed misdetection of Cocoapods support with Unity beta 5.6.

# Version 1.2.21 - May 8, 2017
## Bug Fixes
* Fix for https://github.com/googlesamples/unity-jar-resolver/issues/48
  Android dependency version number parsing when "-alpha" (etc.) are
  included in dependency (AAR / JAR) versions.

# Version 1.2.20 - May 8, 2017
## Bug Fixes
* Attempted to fix
  https://github.com/googlesamples/unity-jar-resolver/issues/48
  where a NullReferenceException could occur if a target file does not
  have a valid version string.

# Version 1.2.19 - May 4, 2017
## Bug Fixes
* Fixed Jar Resolver exploding and deleting AAR files it isn't managing.

# Version 1.2.18 - May 4, 2017
## New Features
* Added support for preserving Unity pods such as when GVR is enabled.

# Version 1.2.17 - Apr 20, 2017
## Bug Fixes
* Fixed auto-resolution when an Android application ID is modified.

# Version 1.2.16 - Apr 17, 2017
## Bug Fixes
* Fixed Unity version number parsing on machines with a locale that uses
  "," for decimal points.
* Fixed null reference exception if JDK path isn't set.

# Version 1.2.15 - Mar 17, 2017
## New Features
* Added warning when the Jar Resolver's background resolution is disabled.
## Bug Fixes
* Fixed support of AARs with native libraries when using Gradle.
* Fixed extra repository paths when resolving dependencies.

# Version 1.2.14 - Mar 7, 2017
## New Features
* Added experimental Android resolution using Gradle.
  This alternative resolver supports proguard stripping with Unity's
  internal build system.
* Added Android support for single ABI builds when using AARs include
  native libraries.
* Disabled Android resolution on changes to all .cs and .js files.
  File patterns that are monitored for auto-resolution can be added
  using PlayServicesResolver.AddAutoResolutionFilePatterns().
* Added tracking of resolved AARs and JARs so they can be cleaned up
  if they're no longer referenced by a project.
* Added persistence of AAR / JAR version replacement for each Unity
  session.
* Added settings dialog to the iOS resolver.
* Integrated Cocoapod tool installation in the iOS resolver.
* Added option to run pod tool via the shell.
## Bug Fixes
* Fixed build of some source Cocoapods (e.g Protobuf).
* VersionHandler no longer prompts to delete obsolete manifests.
* iOS resolver handles Cocoapod installation when using Ruby < 2.2.2.
* Added workaround for package version selection when including
  Google Play Services on Android.
* Fixed support for pods that reference static libraries.
* Fixed support for resource-only pods.

# Version 1.2.12 - Feb 14, 2017
## Bug Fixes
* Fixed re-explosion of AARs when the bundle ID is modified.

# Version 1.2.11 - Jan 30, 2017
## New Features
* Added support for Android Studio builds.
* Added support for native (C/C++) shared libraries in AARs.

# Version 1.2.10 - Jan 11, 2017
## Bug Fixes
* Fixed SDK manager path retrieval.
* Also, report stderr when it's not possible to run the "pod" tool.
* Handle exceptions thrown by Unity.Cecil on asset rename
* Fixed IOSResolver to handle PlayerSettings.iOS.targetOSVersionString

# Version 1.2.9 - Dec 7, 2016
## Bug Fixes
* Improved error reporting when "pod repo update" fails.
* Added detection of xml format xcode projects generated by old Cocoapods
  installations.

# Version 1.2.8 - Dec 6, 2016
## Bug Fixes
* Increased speed of JarResolver resolution.
* Fixed JarResolver caches getting out of sync with requested dependencies
  by removing the caches.
* Fixed JarResolver explode cache always being rewritten even when no
  dependencies change.

# Version 1.2.7 - Dec 2, 2016
## Bug Fixes
* Fixed VersionHandler build errors with Unity 5.5, due to the constantly
  changing BuildTarget enum.
* Added support for Unity configured JDK Path rather than requiring
  JAVA_HOME to be set in the Jar Resolver.

# Version 1.2.6 - Nov 15, 2016
## Bug Fixes
* Fixed IOSResolver errors when iOS support is not installed.
* Added fallback to "pod" executable search which queries the Ruby Gems
  package manager for the binary install location.

# Version 1.2.5 - Nov 3, 2016
## Bug Fixes
* Added crude support for source only Cocoapods to the IOSResolver.

# Version 1.2.4 - Oct 27, 2016
## Bug Fixes
* Automated resolution of out of date pod repositories.

# Version 1.2.3 - Oct 25, 2016
## Bug Fixes
* Fixed exception when reporting conflicting dependencies.

# Version 1.2.2 - Oct 17, 2016
## Bug Fixes
* Fixed issue working with Unity 5.5
* Fixed issue with PlayServicesResolver corrupting other iOS dependencies.
* Updated build script to use Unity distributed tools for building.

# Version 1.2.1 - Jul 25, 2016
## Bug Fixes
* Removed 1.2 Resolver and hardcoded whitelist of AARs to expand.
* Improved error reporting when the "jar" executable can't be found.
* Removed the need to set JAVA_HOME if "jar" is in the user's path.
* Fixed spurious copying of partially matching AARs.
* Changed resolver to only copy / expand when source AARs change.
* Auto-resolution of dependencies is now performed when the Android
  build target is selected.

## New Features
* Expand AARs that contain manifests with variable expansion like
  ${applicationId}.
* Added optional logging in the JarResolverLib module.
* Integration with the Android SDK manager for dependencies that
  declare required Android SDK packages.

# Version 1.2.0 - May 11 2016
## Bug Fixes
* Handles resolving dependencies when the artifacts are split across 2 repos.
* #4 Misdetecting version for versions like 1.2-alpha.  These are now string
  compared if alphanumeric
* Removed resolver creation via reflection since it did not work all the time.
  Now a resolver needs to be loaded externally (which is existing behavior).

## New Features
* Expose PlayServicesResolver properties to allow for script access.
* Explodes firebase-common and firebase-measurement aar files to support
  ${applicationId} substitution.

# Version 1.1.1 - 25 Feb 2016
## Bug Fixes
* #1 Spaces in project path not handled when exploding Aar file.
* #2 Script compilation error: TypeLoadException.

# Version 1.1.0 - 5 Feb 2016
## New Features
* Adds friendly alert when JAVA_HOME is not set on Windows platforms.
* Adds flag for disabling background resolution.
* Expands play-services-measurement and replaces ${applicationId} with the
  bundle Id.

 ## Bug Fixes
* Fixes infinite loop of resolution triggered by resolution.
