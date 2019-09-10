$editorFiles = @("MLAPIProfiler.cs", "NetworkedAnimatorEditor.cs", "NetworkedBehaviourEditor.cs", "NetworkedObjectEditor.cs", "NetworkingManagerEditor.cs", "TrackedObjectEditor.cs", "PostProcessScene.cs")
$libraryFiles = @("MLAPI.dll", "MLAPI.xml", "MLAPI.pdb")
$installerFiles = @("MLAPIEditor.cs")

$myPath = (Get-Item -Path ".\").FullName;
$myPath = $myPath.Replace("\", "/")

$libraryBasePath = -join ($myPath, "/MLAPI/bin/")
$editorBasePath = -join ($myPath, "/MLAPI-Editor/")
$builderPath = -join ($myPath, "/Libraries/Internal/UnityPackager/UnityPackager.exe")

$packageOutPath = -join ($myPath, "/MLAPI.unitypackage")
$installerOutPath = -join ($myPath, "/MLAPI-Installer.unitypackage")

# Args for library generation
$libraryBuildArgs = -join ("null", " ", $packageOutPath, " ")

# Args for installer generation
$installerBuildArgs = -join ("null", " ", $installerOutPath, " ")

# Add editor files to library package
For ($i=0; $i -lt $editorFiles.Count; $i++)  
{
    $libraryBuildArgs += -join ($editorBasePath, $editorFiles.Get($i), " ", "Assets/Editor/MLAPI/", $editorFiles.Get($i), " ")
}

# Add library files to library package
For ($i=0; $i -lt $libraryFiles.Count; $i++)  
{
    $libraryBuildArgs += -join ($libraryBasePath, $libraryFiles.Get($i), " ", "Assets/MLAPI/Lib/", $libraryFiles.Get($i), " ")
}

# Add installer files to installer package
For ($i=0; $i -lt $installerFiles.Count; $i++)  
{
    $installerBuildArgs += -join ($editorBasePath, $installerFiles.Get($i), " ", "Assets/Editor/MLAPI/", $installerFiles.Get($i), " ")
}

Write-Host $builderPath
Write-Host $libraryBuildArgs

Start-Process -FilePath $builderPath -ArgumentList $libraryBuildArgs
Start-Process -FilePath $builderPath -ArgumentList $installerBuildArgs