@echo off
REM Script to prepare AOT builds for benchmarking

set FRAMEWORK=%1
if "%FRAMEWORK%"=="" set FRAMEWORK=net8.0

echo Building TUnit AOT version for %FRAMEWORK%...
dotnet publish UnifiedTests\UnifiedTests.csproj -c Release -p:TestFramework=TUNIT -p:PublishAot=true --framework %FRAMEWORK%

echo AOT build complete. Output in: UnifiedTests\bin\Release\%FRAMEWORK%\publish\