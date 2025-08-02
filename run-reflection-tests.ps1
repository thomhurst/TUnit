#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Framework = "net9.0",
    [string]$Configuration = "Release",
    [string]$Filter = "/*/*/*/*[EngineTest=Pass]"
)

$ErrorActionPreference = "Stop"

Write-Host "Running Reflection tests..." -ForegroundColor Yellow

# Change to test project directory
$testProjectDir = Join-Path $PSScriptRoot "TUnit.TestProject"
if (-not (Test-Path $testProjectDir)) {
    Write-Error "Test project directory not found: $testProjectDir"
    exit 1
}

Push-Location $testProjectDir
try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    dotnet run `
        -f $Framework `
        --configuration $Configuration `
        --treenode-filter $Filter `
        --reflection `
        --no-build 2>&1 | Out-String | Write-Host
    
    $success = $LASTEXITCODE -eq 0
    $stopwatch.Stop()
    
    if ($success) {
        Write-Host "Reflection tests PASSED in $($stopwatch.Elapsed)" -ForegroundColor Green
    } else {
        Write-Host "Reflection tests completed with exit code $LASTEXITCODE in $($stopwatch.Elapsed)" -ForegroundColor Yellow
    }
    
    exit $LASTEXITCODE
} catch {
    Write-Host "Reflection tests FAILED with error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}