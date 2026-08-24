<#
.SYNOPSIS
Verifies that Unity's obsolete API updater rewrites NGO 2.x editor API references to their
NGO 3.x `Unity.Netcode.GameObjects.Editor` equivalents.

.DESCRIPTION
Runs the editor over this project in batch mode with -accept-apiupdate, then asserts that every
`Unity.Netcode.Editor` reference under Assets/Editor was rewritten and that no stale reference
survived. The sources are restored afterwards so the test can be re-run, unless -KeepUpdatedSources
is passed (useful for eyeballing exactly what the updater produced).

.EXAMPLE
.\run-upgrade-test.ps1
.EXAMPLE
.\run-upgrade-test.ps1 -UnityExe "C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe" -Clean
#>
[CmdletBinding()]
param(
    # Editor to run. Defaults to $env:UNITY_EDITOR_PATH, then to the hub install matching
    # ProjectSettings/ProjectVersion.txt.
    [string]$UnityExe,

    # Delete Library first, so the updater runs against a cold import.
    [switch]$Clean,

    # Leave the rewritten sources in place instead of restoring the 2.x originals.
    [switch]$KeepUpdatedSources
)

$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$sourceDir = Join-Path $projectPath 'Assets\Editor'
$logFile = Join-Path $projectPath 'upgrade-test.log'

function Remove-DirectoryRobust {
    # Library/PackageCache holds paths past MAX_PATH that Remove-Item cannot delete - and a partial
    # delete leaves a project that fails to compile for unrelated reasons. Empty the tree with
    # robocopy /MIR first, which is not subject to the limit, then drop the (now shallow) root.
    param([string]$Path)

    $empty = Join-Path ([System.IO.Path]::GetTempPath()) ('empty-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $empty | Out-Null
    try {
        # robocopy returns 0-7 for success; anything higher is a real failure.
        & robocopy $empty $Path /MIR /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed purging $Path (exit $LASTEXITCODE)" }
        Remove-Item -Recurse -Force $Path
    }
    finally {
        Remove-Item -Recurse -Force $empty
    }
}

function Resolve-UnityExe {
    param([string]$Explicit)

    if ($Explicit) {
        if (-not (Test-Path $Explicit)) { throw "Editor not found: $Explicit" }
        return $Explicit
    }
    if ($env:UNITY_EDITOR_PATH) {
        if (-not (Test-Path $env:UNITY_EDITOR_PATH)) { throw "UNITY_EDITOR_PATH does not exist: $($env:UNITY_EDITOR_PATH)" }
        return $env:UNITY_EDITOR_PATH
    }

    $versionFile = Join-Path $projectPath 'ProjectSettings\ProjectVersion.txt'
    $version = (Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
    $candidate = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path $candidate) { return $candidate }

    throw "No editor found for $version. Pass -UnityExe or set UNITY_EDITOR_PATH."
}

# Every 2.x type the sources reference, and what the updater is expected to turn it into.
# Frozen: this is the public editor API of develop-2.0.0, which is released and will not change.
# Extend it by hand if a public editor type is ever relocated again within 3.x.
$expected = @(
    @{ Old = 'Unity.Netcode.Editor.HiddenScriptEditor';                                 New = 'Unity.Netcode.GameObjects.Editor.HiddenScriptEditor' }
    @{ Old = 'Unity.Netcode.Editor.UnityTransportEditor';                               New = 'Unity.Netcode.GameObjects.Editor.UnityTransportEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkAnimatorEditor';                              New = 'Unity.Netcode.GameObjects.Editor.NetworkAnimatorEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkRigidbodyEditor';                             New = 'Unity.Netcode.GameObjects.Editor.NetworkRigidbodyEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkRigidbody2DEditor';                           New = 'Unity.Netcode.GameObjects.Editor.NetworkRigidbody2DEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetcodeEditorBase';                                  New = 'Unity.Netcode.GameObjects.Editor.NetcodeEditorBase' }
    @{ Old = 'Unity.Netcode.Editor.NetworkBehaviourEditor';                              New = 'Unity.Netcode.GameObjects.Editor.NetworkBehaviourEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkManagerEditor';                               New = 'Unity.Netcode.GameObjects.Editor.NetworkManagerEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkManagerHelper';                               New = 'Unity.Netcode.GameObjects.Editor.NetworkManagerHelper' }
    @{ Old = 'Unity.Netcode.Editor.NetworkObjectEditor';                                New = 'Unity.Netcode.GameObjects.Editor.NetworkObjectEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkRigidbodyBaseEditor';                         New = 'Unity.Netcode.GameObjects.Editor.NetworkRigidbodyBaseEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkTransformEditor';                             New = 'Unity.Netcode.GameObjects.Editor.NetworkTransformEditor' }
    @{ Old = 'Unity.Netcode.Editor.NetworkPrefabsEditor';                               New = 'Unity.Netcode.GameObjects.Editor.NetworkPrefabsEditor' }
    @{ Old = 'Unity.Netcode.Editor.Configuration.NetcodeForGameObjectsProjectSettings'; New = 'Unity.Netcode.GameObjects.Editor.Configuration.NetcodeForGameObjectsProjectSettings' }
    @{ Old = 'Unity.Netcode.Editor.Configuration.NetworkPrefabProcessor';               New = 'Unity.Netcode.GameObjects.Editor.Configuration.NetworkPrefabProcessor' }
)

$unity = Resolve-UnityExe -Explicit $UnityExe
Write-Host "Editor:  $unity"
Write-Host "Project: $projectPath"

$backupDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ngo-apiupdater-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $backupDir | Out-Null
Copy-Item -Path (Join-Path $sourceDir '*') -Destination $backupDir -Recurse

try {
    if ($Clean) {
        foreach ($stale in @('Library', 'Temp')) {
            $target = Join-Path $projectPath $stale
            if (Test-Path $target) {
                Write-Host "Removing $stale ..."
                Remove-DirectoryRobust -Path $target
            }
        }
    }

    if (Test-Path $logFile) { Remove-Item -Force $logFile }

    # Start-Process joins -ArgumentList into one command line without quoting the individual values,
    # and ProcessStartInfo.ArgumentList does not exist on the .NET Framework that Windows PowerShell
    # runs on - so quote the two paths here or a checkout under "C:\Users\Jane Doe\..." splits at the
    # space and Unity receives an invalid -projectPath/-logFile.
    $unityArgs = @(
        '-batchmode', '-nographics', '-quit',
        '-accept-apiupdate',
        '-ignoreCompilerErrors',
        '-burst-disable-compilation',
        '-projectPath', "`"$projectPath`"",
        '-logFile', "`"$logFile`""
    )

    Write-Host 'Running the editor (this imports the project and runs the API updater)...'
    $process = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru -Wait -NoNewWindow
    Write-Host "Editor exit code: $($process.ExitCode)"

    $allText = (Get-ChildItem -Path $sourceDir -Filter *.cs | ForEach-Object { Get-Content -Raw $_.FullName }) -join "`n"

    $results = foreach ($entry in $expected) {
        # A rewritten reference contains the new name; the old name only ever survives as a distinct
        # token, so require at least one new hit and no old hit that is not part of a longer name.
        $newHits = ([regex]::Matches($allText, [regex]::Escape($entry.New))).Count
        $oldHits = ([regex]::Matches($allText, [regex]::Escape($entry.Old) + '(?![\w.])')).Count
        if ($newHits -gt 0 -and $oldHits -eq 0) { $result = 'PASS' } else { $result = 'FAIL' }
        [pscustomobject]@{
            Type    = $entry.Old
            Updated = $newHits
            Stale   = $oldHits
            Result  = $result
        }
    }

    $results | Format-Table -AutoSize
    $failures = @($results | Where-Object { $_.Result -eq 'FAIL' })

    Write-Host ''
    if ($failures.Count -eq 0) {
        Write-Host "PASS: all $($expected.Count) deprecated editor types were rewritten." -ForegroundColor Green
    }
    else {
        Write-Host "FAIL: $($failures.Count) of $($expected.Count) types were not rewritten. See $logFile" -ForegroundColor Red
    }

    if ($KeepUpdatedSources) {
        Write-Host "Rewritten sources left in place under Assets/Editor (backup: $backupDir)."
    }

    # Explicit, or the exit code falls through to the last native command (robocopy, which reports
    # non-zero for ordinary success).
    if ($failures.Count -ne 0) { exit 1 } else { exit 0 }
}
finally {
    if (-not $KeepUpdatedSources) {
        Copy-Item -Path (Join-Path $backupDir '*') -Destination $sourceDir -Recurse -Force
        Remove-Item -Recurse -Force $backupDir
    }
}
