# Script to prepare AOT builds for benchmarking
param(
    [string]$Framework = "net8.0"
)

Write-Host "Building TUnit AOT version for $Framework..."
dotnet publish UnifiedTests/UnifiedTests.csproj -c Release -p:TestFramework=TUNIT -p:PublishAot=true --framework $Framework

Write-Host "AOT build complete. Output in: UnifiedTests/bin/Release/$Framework/publish/"