#!/usr/bin/env pwsh
# Measure TUnit source-gen vs reflection startup performance
# Usage:
#   ./measure-startup.ps1                    # Default: 3 iterations, Release build
#   ./measure-startup.ps1 -Iterations 5      # More iterations for precision
#   ./measure-startup.ps1 -SkipBuild         # Skip rebuild if already built
#   ./measure-startup.ps1 -Filter "/*/*/SimpleTests_01/*"  # Filtered run

param(
    [int]$Iterations = 3,
    [switch]$SkipBuild,
    [string]$Filter = "/*/*/*/*",
    [string]$Configuration = "Release",
    [switch]$IncludeJitStats
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = $scriptDir
$projectFile = Join-Path $projectDir "TUnit.PerformanceBenchmarks.csproj"
$resultsDir = Join-Path $projectDir "benchmark-results"

# Ensure results directory exists
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$resultsFile = Join-Path $resultsDir "startup-$timestamp.json"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " TUnit Startup Performance Measurement" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Config:       $Configuration"
Write-Host "Iterations:   $Iterations"
Write-Host "Filter:       $Filter"
Write-Host "JIT Stats:    $IncludeJitStats"
Write-Host ""

# Step 1: Build
if (-not $SkipBuild) {
    Write-Host "[1/3] Building $Configuration..." -ForegroundColor Yellow
    $buildSw = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet build $projectFile -c $Configuration --no-incremental -v q 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    $buildSw.Stop()
    Write-Host "      Build completed in $($buildSw.Elapsed.TotalSeconds.ToString('F1'))s" -ForegroundColor Green
} else {
    Write-Host "[1/3] Skipping build (--SkipBuild)" -ForegroundColor DarkGray
}

# Step 2: Find the built executable
$tfm = "net9.0"
$exeDir = Join-Path $projectDir "bin" $Configuration $tfm
$exeName = "TUnit.PerformanceBenchmarks"
if ($IsWindows -or $env:OS -match "Windows") {
    $exePath = Join-Path $exeDir "$exeName.exe"
} else {
    $exePath = Join-Path $exeDir $exeName
}

if (-not (Test-Path $exePath)) {
    # Fall back to dotnet run
    Write-Host "      Executable not found at $exePath, falling back to dotnet run" -ForegroundColor DarkYellow
    $useDotnetRun = $true
} else {
    $useDotnetRun = $false
    Write-Host "      Using pre-built exe: $exePath" -ForegroundColor Green
}

# Step 3: Run measurements
Write-Host ""
Write-Host "[2/3] Running $Iterations iterations..." -ForegroundColor Yellow

$results = @()

for ($i = 1; $i -le $Iterations; $i++) {
    Write-Host "      Iteration $i/$Iterations... " -NoNewline

    # Force GC before each run to reduce noise
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
    [GC]::Collect()

    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    if ($useDotnetRun) {
        $output = dotnet run --project $projectFile -c $Configuration --no-build -- --treenode-filter "$Filter" --report-trx --timeout 120 2>&1
    } else {
        $output = & $exePath --treenode-filter "$Filter" --report-trx --timeout 120 2>&1
    }

    $sw.Stop()
    $exitCode = $LASTEXITCODE
    $wallTimeMs = $sw.Elapsed.TotalMilliseconds

    # Parse test count from output
    $testCount = 0
    $passedCount = 0
    $failedCount = 0
    foreach ($line in $output) {
        if ($line -match "Passed:\s*(\d+)") { $passedCount = [int]$Matches[1] }
        if ($line -match "Failed:\s*(\d+)") { $failedCount = [int]$Matches[1] }
        if ($line -match "Total:\s*(\d+)") { $testCount = [int]$Matches[1] }
    }

    $result = @{
        Iteration = $i
        WallTimeMs = [math]::Round($wallTimeMs, 1)
        TestCount = $testCount
        Passed = $passedCount
        Failed = $failedCount
        ExitCode = $exitCode
    }
    $results += $result

    $statusColor = if ($exitCode -eq 0) { "Green" } else { "Yellow" }
    Write-Host "$($wallTimeMs.ToString('F0'))ms ($testCount tests)" -ForegroundColor $statusColor
}

# Step 4: Calculate statistics
Write-Host ""
Write-Host "[3/3] Results" -ForegroundColor Yellow
Write-Host ""

$times = $results | ForEach-Object { $_.WallTimeMs }
$avg = ($times | Measure-Object -Average).Average
$min = ($times | Measure-Object -Minimum).Minimum
$max = ($times | Measure-Object -Maximum).Maximum
$sorted = $times | Sort-Object
$median = if ($sorted.Count % 2 -eq 0) {
    ($sorted[$sorted.Count/2 - 1] + $sorted[$sorted.Count/2]) / 2
} else {
    $sorted[[math]::Floor($sorted.Count/2)]
}

# Standard deviation
$sumSquares = 0
foreach ($t in $times) { $sumSquares += ($t - $avg) * ($t - $avg) }
$stddev = [math]::Sqrt($sumSquares / $times.Count)

Write-Host "  Wall-Clock Time:" -ForegroundColor Cyan
Write-Host "    Median:  $($median.ToString('F0'))ms"
Write-Host "    Average: $($avg.ToString('F0'))ms"
Write-Host "    Min:     $($min.ToString('F0'))ms"
Write-Host "    Max:     $($max.ToString('F0'))ms"
Write-Host "    StdDev:  $($stddev.ToString('F0'))ms"
Write-Host ""
Write-Host "  Tests: $($results[0].TestCount) discovered" -ForegroundColor Cyan
Write-Host ""

# Save results
$report = @{
    Timestamp = $timestamp
    Configuration = $Configuration
    Filter = $Filter
    Iterations = $Iterations
    TestCount = $results[0].TestCount
    Summary = @{
        MedianMs = [math]::Round($median, 1)
        AverageMs = [math]::Round($avg, 1)
        MinMs = [math]::Round($min, 1)
        MaxMs = [math]::Round($max, 1)
        StdDevMs = [math]::Round($stddev, 1)
    }
    Runs = $results
    Environment = @{
        DotnetVersion = (dotnet --version 2>$null) ?? "unknown"
        OS = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        ProcessorCount = [Environment]::ProcessorCount
        MachineName = [Environment]::MachineName
    }
}

$report | ConvertTo-Json -Depth 5 | Out-File -FilePath $resultsFile -Encoding utf8
Write-Host "  Results saved to: $resultsFile" -ForegroundColor DarkGray
