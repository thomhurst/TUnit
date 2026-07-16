#!/usr/bin/env pwsh
# Compare TUnit startup performance between two git refs (branches/commits/tags)
# Usage:
#   ./compare-branches.ps1 -Baseline main -Current HEAD
#   ./compare-branches.ps1 -Baseline main -Current 001-sourcegen-startup-perf -Iterations 5
#   ./compare-branches.ps1 -BaselineFile benchmark-results/startup-baseline.json  # Compare against saved baseline

param(
    [string]$Baseline = "main",
    [string]$Current = "HEAD",
    [string]$BaselineFile = "",
    [int]$Iterations = 3,
    [string]$Filter = "/*/*/*/*",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$resultsDir = Join-Path $scriptDir "benchmark-results"

New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " TUnit Branch Performance Comparison" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

function Measure-Branch {
    param(
        [string]$Ref,
        [string]$Label
    )

    Write-Host "--- Measuring: $Label ($Ref) ---" -ForegroundColor Yellow

    # Save current state
    $originalBranch = git rev-parse --abbrev-ref HEAD 2>$null
    $originalCommit = git rev-parse HEAD 2>$null
    $hasStash = $false

    # Stash any uncommitted changes
    $status = git status --porcelain 2>$null
    if ($status) {
        Write-Host "  Stashing uncommitted changes..." -ForegroundColor DarkGray
        git stash push -m "compare-branches temp stash" --quiet 2>$null
        $hasStash = $true
    }

    try {
        # Checkout the target ref
        Write-Host "  Checking out $Ref..." -ForegroundColor DarkGray
        git checkout $Ref --quiet 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  Failed to checkout $Ref" -ForegroundColor Red
            return $null
        }

        # Run measurement
        $measureScript = Join-Path $scriptDir "measure-startup.ps1"
        & $measureScript -Iterations $Iterations -Filter $Filter -Configuration $Configuration

        # Find the latest results file
        $latestResult = Get-ChildItem -Path $resultsDir -Filter "startup-*.json" |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($latestResult) {
            $data = Get-Content $latestResult.FullName | ConvertFrom-Json
            return $data
        }
        return $null
    }
    finally {
        # Restore original state
        Write-Host "  Restoring to $originalBranch..." -ForegroundColor DarkGray
        git checkout $originalBranch --quiet 2>$null
        if ($hasStash) {
            git stash pop --quiet 2>$null
        }
    }
}

# Run measurements
if ($BaselineFile -and (Test-Path $BaselineFile)) {
    Write-Host "Using saved baseline: $BaselineFile" -ForegroundColor Green
    $baselineData = Get-Content $BaselineFile | ConvertFrom-Json
} else {
    $baselineData = Measure-Branch -Ref $Baseline -Label "Baseline"
}

Write-Host ""
$currentData = Measure-Branch -Ref $Current -Label "Current"

if (-not $baselineData -or -not $currentData) {
    Write-Host "Failed to collect measurements for one or both branches." -ForegroundColor Red
    exit 1
}

# Display comparison
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Comparison Results" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$bMedian = $baselineData.Summary.MedianMs
$cMedian = $currentData.Summary.MedianMs
$diff = $cMedian - $bMedian
$pct = if ($bMedian -gt 0) { ($diff / $bMedian) * 100 } else { 0 }

$changeSymbol = if ($diff -lt 0) { "faster" } elseif ($diff -gt 0) { "slower" } else { "same" }
$changeColor = if ($diff -lt 0) { "Green" } elseif ($diff -gt 0) { "Red" } else { "Yellow" }

Write-Host "  Metric          Baseline        Current         Change" -ForegroundColor White
Write-Host "  ─────────────── ─────────────── ─────────────── ───────────────"
Write-Host ("  Median          {0,10}ms   {1,10}ms   " -f $bMedian.ToString('F0'), $cMedian.ToString('F0')) -NoNewline
Write-Host ("{0}{1}ms ({2}%)" -f $(if($diff -ge 0){"+"}else{""}), $diff.ToString('F0'), $pct.ToString('F1')) -ForegroundColor $changeColor

Write-Host ("  Average         {0,10}ms   {1,10}ms" -f $baselineData.Summary.AverageMs.ToString('F0'), $currentData.Summary.AverageMs.ToString('F0'))
Write-Host ("  Min             {0,10}ms   {1,10}ms" -f $baselineData.Summary.MinMs.ToString('F0'), $currentData.Summary.MinMs.ToString('F0'))
Write-Host ("  Max             {0,10}ms   {1,10}ms" -f $baselineData.Summary.MaxMs.ToString('F0'), $currentData.Summary.MaxMs.ToString('F0'))
Write-Host ("  Tests           {0,10}       {1,10}" -f $baselineData.TestCount, $currentData.TestCount)

Write-Host ""
Write-Host "  Verdict: " -NoNewline
Write-Host "$($[math]::Abs($pct).ToString('F1'))% $changeSymbol" -ForegroundColor $changeColor
Write-Host ""

# Save comparison
$comparison = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
    Baseline = @{ Ref = $Baseline; Summary = $baselineData.Summary; TestCount = $baselineData.TestCount }
    Current = @{ Ref = $Current; Summary = $currentData.Summary; TestCount = $currentData.TestCount }
    Change = @{
        MedianDiffMs = [math]::Round($diff, 1)
        MedianDiffPercent = [math]::Round($pct, 1)
        Direction = $changeSymbol
    }
}

$compFile = Join-Path $resultsDir "comparison-$($comparison.Timestamp).json"
$comparison | ConvertTo-Json -Depth 5 | Out-File -FilePath $compFile -Encoding utf8
Write-Host "  Saved to: $compFile" -ForegroundColor DarkGray
