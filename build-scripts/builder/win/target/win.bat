echo off

echo %1
if "%1"=="" goto BLANK
if "%1" == "build" goto BUILD 
if "%1" == "test"  goto TEST
if "%1" == "setup"  goto SETUP

:BLANK
echo must pass at either "build", "setup" or "test" as first argument
goto done
:BUILD
echo building on windows for windows
set UTR_VERSION=0.12.0
call utr --suite=playmode --platform=StandaloneWindows64  --editor-location=.Editor --testproject=testproject --player-save-path=build/players --artifacts_path=build/logs --scripting-backend=mono --build-only --testfilter=Unity.Netcode.RuntimeTests
goto done
:TEST
echo "Starting test 1 with filter Unity.Netcode.RuntimeTests"
set UTR_VERSION=0.12.0
call utr --suite=playmode --platform=StandaloneWindows64  --player-load-path=build/players --artifacts_path=build/test-results --testfilter=Unity.Netcode.RuntimeTests
goto done
:DONE
echo done
exit /B 0
:SETUP
if not exist .git (echo Must be run from the root of the repository && exit 1)
if not exist com.unity.netcode.gameobjects (echo Must be run from the root of the repository && exit 1)

if not exist utr.bat (echo downloading utr && curl -s https://artifactory.prd.it.unity3d.com/artifactory/unity-tools-local/utr-standalone/utr.bat --output utr.bat)
unity-downloader-cli -u 2021.2 -c Editor -c Android --fast --wait
goto done