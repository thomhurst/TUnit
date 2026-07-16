# Run baseline benchmarks at all scale tiers
# Usage: .\run-baseline.ps1

param(
    [string]$OutputDir = ".\Results"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

$scales = @(100, 500, 1000, 5000, 10000)
$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$resultsFile = "$OutputDir\baseline-$timestamp.md"

Push-Location $projectDir
try {
    # Ensure output directory exists
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

    # Initialize results file
    @"
# TUnit Performance Baseline
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Environment
- OS: $([System.Environment]::OSVersion.VersionString)
- CPU: $((Get-CimInstance Win32_Processor).Name)
- .NET: $(dotnet --version)
- TUnit: $(git describe --tags --always 2>$null || 'unknown')

## Results

| Scale | Tests | Discovery (ms) | Execution (ms) | Total (ms) | GC Gen0 | GC Gen1 | GC Gen2 |
|-------|-------|----------------|----------------|------------|---------|---------|---------|
"@ | Out-File -FilePath $resultsFile -Encoding utf8

    foreach ($scale in $scales) {
        Write-Host "`n========================================" -ForegroundColor Cyan
        Write-Host "Running benchmark at scale: $scale" -ForegroundColor Cyan
        Write-Host "========================================`n" -ForegroundColor Cyan

        # Regenerate tests for the specified scale
        & pwsh -ExecutionPolicy Bypass -File "$projectDir\generate-tests.ps1" -Scale $scale

        # Build the project
        Write-Host "Building project..." -ForegroundColor Yellow
        dotnet build -c Release --no-restore 2>&1 | Out-Null

        # Run and capture timing
        Write-Host "Running tests..." -ForegroundColor Yellow
        $sw = [System.Diagnostics.Stopwatch]::StartNew()

        # Capture GC stats before
        $gcBefore = @(
            [GC]::CollectionCount(0),
            [GC]::CollectionCount(1),
            [GC]::CollectionCount(2)
        )

        $output = dotnet run -c Release --no-build 2>&1

        $sw.Stop()

        # Capture GC stats after
        $gcAfter = @(
            [GC]::CollectionCount(0),
            [GC]::CollectionCount(1),
            [GC]::CollectionCount(2)
        )

        $gcDelta = @(
            $gcAfter[0] - $gcBefore[0],
            $gcAfter[1] - $gcBefore[1],
            $gcAfter[2] - $gcBefore[2]
        )

        $totalMs = $sw.ElapsedMilliseconds

        # Parse test count from output
        $testCount = if ($output -match "(\d+) test[s]? passed") { $matches[1] } else { "?" }

        Write-Host "Completed in ${totalMs}ms" -ForegroundColor Green

        # Append to results file
        "| $scale | $testCount | - | - | $totalMs | $($gcDelta[0]) | $($gcDelta[1]) | $($gcDelta[2]) |" |
            Out-File -FilePath $resultsFile -Encoding utf8 -Append
    }

    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Baseline complete! Results saved to:" -ForegroundColor Green
    Write-Host $resultsFile -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Green

    # Display results
    Get-Content $resultsFile
}
finally {
    Pop-Location
}
