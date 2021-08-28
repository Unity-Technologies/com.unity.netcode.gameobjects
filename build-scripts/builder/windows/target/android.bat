echo off

echo %1
if "%1"=="" goto BLANK
if "%1" == "build" goto BUILD 
if "%1" == "test"  goto TEST

:BLANK
echo must pass at either "build" or "test" as first argument
goto done
:BUILD
echo building on windows for android
call utr --suite=playmode --platform=Android --editor-location=.Editor --testproject=testproject --player-save-path=build/players --artifacts_path=build/logs --scripting-backend=mono --build-only
goto done
:TEST
SET ANDROID_SDK_ROOT=%cd%\.Editor\Data\PlaybackEngines\AndroidPlayer\SDK
call utr --suite=playmode --platform=Android --player-load-path=build/players --artifacts_path=build/test-results
goto done
:DONE
echo done
exit /B 0
:SETUP
if not exist .git (echo Must be run from the root of the repository && exit 1)
if not exist com.unity.netcode.gameobjects (echo Must be run from the root of the repository && exit 1)

if not exist utr.bat (echo downloading utr && curl -s https://artifactory.prd.it.unity3d.com/artifactory/unity-tools-local/utr-standalone/utr.bat --output utr.bat)
unity-downloader-cli -u 2021.2 -c Editor -c Android --fast --wait
