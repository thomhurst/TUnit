# Profile TUnit.PerformanceBenchmarks with dotnet-trace (CPU)
# Usage: .\profile-cpu.ps1 [-Scale 1000]

param(
    [int]$Scale = 1000,
    [string]$OutputDir = ".\Results"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

Push-Location $projectDir
try {
    # Ensure output directory exists
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

    # Regenerate tests for the specified scale
    Write-Host "Generating tests for scale: $Scale" -ForegroundColor Cyan
    & pwsh -ExecutionPolicy Bypass -File "$projectDir\generate-tests.ps1" -Scale $Scale

    # Build the project
    Write-Host "Building project..." -ForegroundColor Cyan
    dotnet build -c Release --no-restore

    # Generate timestamp for output files
    $timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
    $traceFile = "$OutputDir\cpu-trace-$Scale-$timestamp.nettrace"

    Write-Host "Starting CPU profile capture..." -ForegroundColor Cyan
    Write-Host "Trace will be saved to: $traceFile" -ForegroundColor Yellow

    # Run with dotnet-trace
    dotnet-trace collect `
        --output $traceFile `
        -- dotnet run -c Release --no-build

    Write-Host "Profile captured: $traceFile" -ForegroundColor Green

    # Convert to SpeedScope format
    $speedscopeFile = $traceFile -replace "\.nettrace$", ".speedscope.json"
    Write-Host "Converting to SpeedScope format..." -ForegroundColor Cyan
    dotnet-trace convert $traceFile --format speedscope

    Write-Host "SpeedScope file: $speedscopeFile" -ForegroundColor Green
    Write-Host "Open https://speedscope.app and drag the .speedscope.json file to view" -ForegroundColor Yellow
}
finally {
    Pop-Location
}
