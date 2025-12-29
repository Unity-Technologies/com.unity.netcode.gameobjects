@echo off

dotnet run -c Release --project %~dp0\CI\NGO.Cookbook.csproj %*
if %errorlevel% neq 0 exit /b %errorlevel%
