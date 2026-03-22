#Requires -Version 7.0
<#
.SYNOPSIS
    Build and profile a TUnit test project using dotnet-trace and dotnet-counters.

.DESCRIPTION
    Produces:
      <output-dir>/trace.nettrace      - Full execution trace (open in PerfView, VS, or speedscope)
      <output-dir>/trace.speedscope    - Speedscope JSON (open at https://speedscope.app)
      <output-dir>/counters.csv        - Runtime counters (GC, threadpool, CPU, etc.)
      <output-dir>/dump.dmp            - (optional) Full memory dump for heap analysis

    Prerequisites:
      dotnet tool install -g dotnet-trace
      dotnet tool install -g dotnet-counters
      dotnet tool install -g dotnet-dump  (optional, for -Dump)

.EXAMPLE
    # Profile specific tests
    .\scripts\profile-tunit.ps1 -Filter "/*/*/BasicTests/*"

.EXAMPLE
    # Profile a different project with a memory dump
    .\scripts\profile-tunit.ps1 -Project TUnit.PerformanceBenchmarks -Dump

.EXAMPLE
    # Use GC-verbose tracing on net9.0
    .\scripts\profile-tunit.ps1 -TraceProfile gc-verbose -Framework net9.0
#>

[CmdletBinding()]
param(
    [string]$Project = "TUnit.TestProject",
    [string]$Framework = "net10.0",
    [string]$Configuration = "Release",
    [string]$Filter = "",
    [string]$OutputDir = "",
    [ValidateSet("cpu-sampling", "gc-verbose", "gc-collect", "none")]
    [string]$TraceProfile = "cpu-sampling",
    [ValidateSet("speedscope", "chromium", "nettrace")]
    [string]$TraceFormat = "speedscope",
    [int]$CountersInterval = 1,
    [switch]$Dump,
    [switch]$NoBuild,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$ExtraArgs
)

$ErrorActionPreference = "Stop"

# ── Resolve paths ─────────────────────────────────────────────────────────────

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $RepoRoot) { $RepoRoot = Split-Path -Parent $PSScriptRoot }
# Handle running from repo root
if (Test-Path (Join-Path $PSScriptRoot "..\$Project")) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

$ProjectDir = Join-Path $RepoRoot $Project
if (-not (Test-Path $ProjectDir)) {
    Write-Error "Project directory not found: $ProjectDir"
    return
}

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
if (-not $OutputDir) {
    $OutputDir = Join-Path $RepoRoot ".profile" "$Project-$Timestamp"
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$ExePath = Join-Path $ProjectDir "bin" $Configuration $Framework "$Project.exe"

Write-Host ""
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  TUnit Profiler" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  Project:       $Project"
Write-Host "  Framework:     $Framework"
Write-Host "  Configuration: $Configuration"
Write-Host "  Filter:        $(if ($Filter) { $Filter } else { '<none - all tests>' })"
Write-Host "  Trace profile: $TraceProfile"
Write-Host "  Output:        $OutputDir"
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Build ─────────────────────────────────────────────────────────────

if (-not $NoBuild) {
    Write-Host ">> Building $Project ($Configuration | $Framework)..." -ForegroundColor Yellow
    dotnet build $ProjectDir `
        -c $Configuration `
        -f $Framework `
        --nologo `
        -v quiet `
        -p:TreatWarningsAsErrors=false

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        return
    }
    Write-Host "   Build complete" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ">> Skipping build (--NoBuild)" -ForegroundColor Yellow
    Write-Host ""
}

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at: $ExePath`nTry building without -NoBuild, or check -Framework and -Configuration."
    return
}

# ── Build test command args ───────────────────────────────────────────────────

$TestArgs = @()
if ($Filter) {
    $TestArgs += "--treenode-filter"
    $TestArgs += $Filter
}
if ($ExtraArgs) {
    $TestArgs += $ExtraArgs
}

# ── Step 2: dotnet-trace ──────────────────────────────────────────────────────

$TraceFile = Join-Path $OutputDir "trace.nettrace"

if ($TraceProfile -ne "none") {
    Write-Host ">> Collecting trace (profile: $TraceProfile)..." -ForegroundColor Yellow

    $traceArgs = @(
        "collect"
        "--output", $TraceFile
        "--profile", $TraceProfile
        "--format", "NetTrace"
        "--"
        $ExePath
    ) + $TestArgs

    & dotnet-trace @traceArgs 2>&1 | Tee-Object -FilePath (Join-Path $OutputDir "trace.log")

    if (Test-Path $TraceFile) {
        Write-Host "   Trace saved: $TraceFile" -ForegroundColor Green

        # Convert to requested format
        if ($TraceFormat -ne "nettrace") {
            Write-Host "   Converting to $TraceFormat..."
            $ConvertedFile = Join-Path $OutputDir "trace.$TraceFormat"
            & dotnet-trace convert $TraceFile --format $TraceFormat --output $ConvertedFile 2>$null
            if (Test-Path $ConvertedFile) {
                Write-Host "   Converted: $ConvertedFile" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "   Warning: Trace file was not created" -ForegroundColor DarkYellow
    }
    Write-Host ""
} else {
    Write-Host ">> Skipping trace collection (-TraceProfile none)" -ForegroundColor Yellow
    Write-Host ""
}

# ── Step 3: dotnet-counters ──────────────────────────────────────────────────

$CountersFile = Join-Path $OutputDir "counters.csv"
Write-Host ">> Collecting runtime counters (interval: ${CountersInterval}s)..." -ForegroundColor Yellow

# Start the test process
$testProc = Start-Process -FilePath $ExePath -ArgumentList $TestArgs -PassThru -NoNewWindow

Start-Sleep -Seconds 1

if (-not $testProc.HasExited) {
    $counterProviders = "System.Runtime,Microsoft.AspNetCore.Hosting,Microsoft-Extensions-DependencyInjection"

    # Start counters collection in background
    $counterJob = Start-Job -ScriptBlock {
        param($pid, $file, $interval, $providers)
        & dotnet-counters collect `
            --process-id $pid `
            --output $file `
            --format csv `
            --refresh-interval $interval `
            --counters $providers 2>&1
    } -ArgumentList $testProc.Id, $CountersFile, $CountersInterval, $counterProviders

    # Wait for test process to finish
    $testProc | Wait-Process

    # Give counters time to flush
    Start-Sleep -Seconds 2
    Stop-Job $counterJob -ErrorAction SilentlyContinue
    Remove-Job $counterJob -Force -ErrorAction SilentlyContinue

    if (Test-Path $CountersFile) {
        Write-Host "   Counters saved: $CountersFile" -ForegroundColor Green
    } else {
        Write-Host "   Warning: Counter file was not created" -ForegroundColor DarkYellow
    }
} else {
    Write-Host "   Test process exited too quickly for counter collection" -ForegroundColor DarkYellow
}
Write-Host ""

# ── Step 4: Memory dump (optional) ───────────────────────────────────────────

if ($Dump) {
    $DumpFile = Join-Path $OutputDir "dump.dmp"
    Write-Host ">> Collecting memory dump..." -ForegroundColor Yellow

    # Run test exe again
    $dumpProc = Start-Process -FilePath $ExePath -ArgumentList $TestArgs -PassThru -NoNewWindow

    Start-Sleep -Seconds 3

    if (-not $dumpProc.HasExited) {
        & dotnet-dump collect `
            --process-id $dumpProc.Id `
            --output $DumpFile `
            --type Full 2>&1 | Tee-Object -FilePath (Join-Path $OutputDir "dump.log")

        if (Test-Path $DumpFile) {
            Write-Host "   Dump saved: $DumpFile" -ForegroundColor Green
        }

        $dumpProc | Wait-Process -ErrorAction SilentlyContinue
    } else {
        Write-Host "   Test process exited before dump could be captured" -ForegroundColor DarkYellow
        Write-Host "   Try using a filter that selects more/slower tests" -ForegroundColor DarkYellow
    }
    Write-Host ""
}

# ── Summary ───────────────────────────────────────────────────────────────────

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  Profiling complete! Output: $OutputDir" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Files:" -ForegroundColor White

Get-ChildItem -Path $OutputDir -File | ForEach-Object {
    $size = if ($_.Length -ge 1MB) { "{0:N1} MB" -f ($_.Length / 1MB) }
            elseif ($_.Length -ge 1KB) { "{0:N1} KB" -f ($_.Length / 1KB) }
            else { "$($_.Length) B" }
    Write-Host "    $($size.PadLeft(10))  $($_.Name)"
}

Write-Host ""
Write-Host "  How to analyze:" -ForegroundColor White
Write-Host ""
Write-Host "  Trace (.nettrace):" -ForegroundColor Gray
Write-Host "    - Visual Studio: File > Open > trace.nettrace"
Write-Host "    - PerfView:      perfview.exe trace.nettrace"
Write-Host "    - speedscope:    https://speedscope.app (open trace.speedscope)"
Write-Host ""
Write-Host "  Counters (.csv):" -ForegroundColor Gray
Write-Host "    - Excel: Open counters.csv"
Write-Host "    - Python: pandas.read_csv('counters.csv')"
Write-Host ""
if ($Dump) {
    Write-Host "  Dump (.dmp):" -ForegroundColor Gray
    Write-Host "    - Visual Studio: File > Open > dump.dmp"
    Write-Host "    - dotnet-dump:   dotnet-dump analyze dump.dmp"
    Write-Host "      > dumpheap -stat        (heap statistics)"
    Write-Host "      > dumpheap -type <Type> (find specific types)"
    Write-Host "      > gcroot <addr>         (find GC roots)"
    Write-Host ""
}
Write-Host "===================================================================" -ForegroundColor Cyan
