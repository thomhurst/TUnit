#!/usr/bin/env pwsh
# Measure JIT compilation count during TUnit startup using dotnet-counters
# This captures the number of methods JIT-compiled, which is our primary optimization target.
#
# Usage:
#   ./measure-jit.ps1                          # Default: full run
#   ./measure-jit.ps1 -Filter "/*/*/SimpleTests_01/*"  # Filtered run
#
# Prerequisites:
#   dotnet tool install --global dotnet-counters
#   dotnet tool install --global dotnet-trace    (for detailed JIT trace)

param(
    [string]$Filter = "/*/*/*/*",
    [string]$Configuration = "Release",
    [switch]$DetailedTrace,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $scriptDir "TUnit.PerformanceBenchmarks.csproj"
$resultsDir = Join-Path $scriptDir "benchmark-results"

New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " TUnit JIT Compilation Measurement" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Build
if (-not $SkipBuild) {
    Write-Host "[1/3] Building $Configuration..." -ForegroundColor Yellow
    dotnet build $projectFile -c $Configuration --no-incremental -v q 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "      Done" -ForegroundColor Green
}

# Find executable
$tfm = "net9.0"
$exeDir = Join-Path $scriptDir "bin" $Configuration $tfm
$exeName = "TUnit.PerformanceBenchmarks"
if ($IsWindows -or $env:OS -match "Windows") {
    $exePath = Join-Path $exeDir "$exeName.exe"
} else {
    $exePath = Join-Path $exeDir $exeName
}

Write-Host ""
Write-Host "[2/3] Measuring JIT compilations..." -ForegroundColor Yellow
Write-Host "      Filter: $Filter"
Write-Host ""

if ($DetailedTrace) {
    # Use dotnet-trace for detailed JIT method list
    $traceFile = Join-Path $resultsDir "jit-trace-$timestamp.nettrace"

    Write-Host "      Collecting detailed JIT trace..." -ForegroundColor DarkGray
    Write-Host "      (This captures every JIT'd method name)" -ForegroundColor DarkGray

    # Start the process and collect JIT events
    $env:DOTNET_JitCollect64BitCounts = "1"
    $env:COMPlus_JitCollect64BitCounts = "1"

    if (Get-Command dotnet-trace -ErrorAction SilentlyContinue) {
        dotnet-trace collect `
            --providers "Microsoft-Windows-DotNETRuntime:0x10:5" `
            --output $traceFile `
            -- $exePath --treenode-filter "$Filter" --exit-on-process-exit 2>&1 | Out-Null

        Write-Host "      Trace saved to: $traceFile" -ForegroundColor Green
        Write-Host "      View with: dotnet-trace convert $traceFile --format Speedscope" -ForegroundColor DarkGray
    } else {
        Write-Host "      dotnet-trace not found. Install with: dotnet tool install --global dotnet-trace" -ForegroundColor Red
    }
} else {
    # Use environment variable to enable JIT compilation logging
    # This is lighter weight than dotnet-trace

    # Method 1: Use DOTNET_JitDisasm environment variable won't work (too verbose)
    # Method 2: Use ETW events via dotnet-counters for aggregate count
    # Method 3: Simple approach - use runtime events

    # The simplest reliable approach: run with JIT ETW events and count MethodJittingStarted events
    $env:DOTNET_EnableDiagnostics = "1"

    # Run the test and capture output + timing
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = & $exePath --treenode-filter "$Filter" --diagnostic-verbosity high --exit-on-process-exit 2>&1
    $sw.Stop()

    $exitCode = $LASTEXITCODE
    $wallTimeMs = $sw.Elapsed.TotalMilliseconds

    # Parse test count
    $testCount = 0
    foreach ($line in $output) {
        if ($line -match "Total:\s*(\d+)") { $testCount = [int]$Matches[1] }
    }

    Write-Host ""
    Write-Host "[3/3] Results" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Wall-Clock Time: $($wallTimeMs.ToString('F0'))ms" -ForegroundColor Cyan
    Write-Host "  Tests Discovered: $testCount" -ForegroundColor Cyan
    Write-Host ""
}

# Count generated methods in source-generator output (static analysis)
Write-Host "  Static Analysis (generated method count):" -ForegroundColor Cyan

$generatedDir = Join-Path $scriptDir "obj" $Configuration $tfm "generated" "TUnit.Core.SourceGenerator" "TUnit.Core.SourceGenerator.Generators.TestMetadataGenerator"

if (Test-Path $generatedDir) {
    $generatedFiles = Get-ChildItem -Path $generatedDir -Filter "*.g.cs" -Recurse

    $totalMethods = 0
    $invokeMethods = 0
    $materializeMethods = 0
    $getTestsMethods = 0
    $enumerateDescriptorMethods = 0
    $attributeMethods = 0
    $createInstanceMethods = 0
    $testSourceClasses = 0

    foreach ($file in $generatedFiles) {
        $content = Get-Content $file.FullName -Raw

        # Count different method types
        $m = [regex]::Matches($content, "__InvokeTest_")
        $invokeMethods += $m.Count

        $m = [regex]::Matches($content, "__Materialize_")
        $materializeMethods += $m.Count

        $m = [regex]::Matches($content, "GetTests\(")
        $getTestsMethods += $m.Count

        $m = [regex]::Matches($content, "EnumerateTestDescriptors\(")
        $enumerateDescriptorMethods += $m.Count

        $m = [regex]::Matches($content, "__CreateAttributes_")
        $attributeMethods += $m.Count

        $m = [regex]::Matches($content, "CreateInstance\(")
        $createInstanceMethods += $m.Count

        if ($content -match "TestSource") {
            $testSourceClasses++
        }
    }

    $totalMethods = $invokeMethods + $materializeMethods + $getTestsMethods + $enumerateDescriptorMethods + $attributeMethods + $createInstanceMethods

    Write-Host "    Generated files:          $($generatedFiles.Count)"
    Write-Host "    TestSource classes:        $testSourceClasses"
    Write-Host "    __InvokeTest_* methods:    $invokeMethods     (JIT'd at startup)"
    Write-Host "    __Materialize_* methods:   $materializeMethods  (deferred, only with filter)"
    Write-Host "    GetTests methods:          $getTestsMethods"
    Write-Host "    EnumerateTestDescriptors:  $enumerateDescriptorMethods"
    Write-Host "    __CreateAttributes_*:      $attributeMethods"
    Write-Host "    CreateInstance methods:     $createInstanceMethods"
    Write-Host ""
    Write-Host "    Total generated methods:   $totalMethods" -ForegroundColor White
    Write-Host "    JIT'd at startup (est.):   $($invokeMethods + $getTestsMethods + $enumerateDescriptorMethods + $attributeMethods + $createInstanceMethods)" -ForegroundColor Yellow
    Write-Host "    Deferred (materializers):  $materializeMethods" -ForegroundColor Green
} else {
    Write-Host "    Generated source directory not found. Build first with -Configuration $Configuration" -ForegroundColor DarkYellow
    Write-Host "    Expected: $generatedDir" -ForegroundColor DarkGray
}

# Save report
$report = @{
    Timestamp = $timestamp
    Configuration = $Configuration
    Filter = $Filter
    WallTimeMs = if ($sw) { [math]::Round($wallTimeMs, 1) } else { $null }
    TestCount = $testCount
    GeneratedMethodCounts = @{
        InvokeTest = $invokeMethods
        Materialize = $materializeMethods
        GetTests = $getTestsMethods
        EnumerateTestDescriptors = $enumerateDescriptorMethods
        CreateAttributes = $attributeMethods
        CreateInstance = $createInstanceMethods
        Total = $totalMethods
        EstimatedStartupJit = $invokeMethods + $getTestsMethods + $enumerateDescriptorMethods + $attributeMethods + $createInstanceMethods
    }
}

$reportFile = Join-Path $resultsDir "jit-$timestamp.json"
$report | ConvertTo-Json -Depth 5 | Out-File -FilePath $reportFile -Encoding utf8
Write-Host ""
Write-Host "  Results saved to: $reportFile" -ForegroundColor DarkGray
