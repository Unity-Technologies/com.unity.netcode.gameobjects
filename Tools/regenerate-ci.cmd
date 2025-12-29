@echo off

cd /d "%~dp0.."
dotnet run --project "Tools\CI\NGO.Cookbook.csproj" %*
if %errorlevel% neq 0 exit /b %errorlevel%
