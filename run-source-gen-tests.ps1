#!/usr/bin/env pwsh

Write-Host "Running Source Generation tests..." -ForegroundColor Cyan

Push-Location TUnit.TestProject
try {
    dotnet run -f net9.0 --configuration Release --treenode-filter "/*/*/*/*[EngineTest=Pass]"
    exit $LASTEXITCODE
} finally {
    Pop-Location
}