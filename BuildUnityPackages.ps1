$editorFiles = @("MLAPIProfiler.cs", "NetworkedAnimatorEditor.cs", "NetworkedBehaviourEditor.cs", "NetworkedObjectEditor.cs", "NetworkingManagerEditor.cs", "TrackedObjectEditor.cs")
$installerFiles = @("MLAPIEditor.cs")

$myPath = (Get-Item -Path ".\").FullName;
$basePath = -join ($myPath, "/MLAPI-Editor/")
$builderPath = -join ($myPath, "/Libraries/Internal/UnityPackager/UnityPackager.exe")

$editorOutPath = -join ($myPath, "/MLAPI-Editor.unitypackage")
$installerOutPath = -join ($myPath, "/MLAPI-Installer.unitypackage")

$windows = "true"

$editorBuildArgs = ""
if ($windows -ne @("true")) {
    $editorBuildArgs += -join ($builderPath, " ")
}


$editorBuildArgs += -join ($basePath, " ", $editorOutPath, " ")

For ($i=0; $i -lt $editorFiles.Count; $i++)  
{
    $editorBuildArgs += -join ($basePath, $editorFiles.Get($i), " ")
    $editorBuildArgs += -join ("Assets/", $editorFiles.Get($i), " ")
}

$installerBuildArgs = ""
if ($windows -ne "true") {
    $installerBuildArgs += -join ($builderPath, " ")
}
$installerBuildArgs += -join ($basePath, " ", $installerOutPath, " ")

For ($i=0; $i -lt $installerFiles.Count; $i++)  
{
    $installerBuildArgs += -join ($basePath, $installerFiles.Get($i), " ")
    $installerBuildArgs += -join ("Assets/", $installerFiles.Get($i), " ")
}

$myBuilderPath = "";
if ($windows -ne "true") {
    $myBuilderPath = "mono"
} else {
    $myBuilderPath = $builderPath
}

Write-Host $myBuilderPath

Start-Process -FilePath $myBuilderPath -ArgumentList $editorBuildArgs
Start-Process -FilePath $myBuilderPath -ArgumentList $installerBuildArgs