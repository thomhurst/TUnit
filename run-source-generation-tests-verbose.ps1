#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Framework = "net9.0",
    [string]$Configuration = "Release",
    [string]$Filter = "/*/*/*/*[EngineTest=Pass]"
)

$ErrorActionPreference = "Stop"

Write-Host "Running Source Generation tests with verbose output..." -ForegroundColor Yellow

# Change to test project directory
$testProjectDir = Join-Path $PSScriptRoot "TUnit.TestProject"
if (-not (Test-Path $testProjectDir)) {
    Write-Error "Test project directory not found: $testProjectDir"
    exit 1
}

Push-Location $testProjectDir
try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    # Capture output to analyze failures
    $output = dotnet run `
        -f $Framework `
        --configuration $Configuration `
        --treenode-filter $Filter `
        --no-build 2>&1 | Out-String
    
    # Display the full output
    Write-Host $output
    
    # Extract and highlight failed tests
    $failedTests = $output | Select-String -Pattern "^failed .*" -AllMatches
    
    if ($failedTests.Matches.Count -gt 0) {
        Write-Host "`n=== FAILED TESTS ===" -ForegroundColor Red
        foreach ($match in $failedTests.Matches) {
            Write-Host $match.Value -ForegroundColor Red
            
            # Try to extract the next few lines for context
            $startIndex = $match.Index
            $contextEnd = $output.IndexOf("`n", $startIndex + 500)
            if ($contextEnd -gt $startIndex) {
                $context = $output.Substring($startIndex, [Math]::Min(500, $contextEnd - $startIndex))
                Write-Host $context -ForegroundColor DarkRed
            }
        }
    }
    
    $success = $LASTEXITCODE -eq 0
    $stopwatch.Stop()
    
    # Extract summary from output
    $summaryMatch = $output | Select-String -Pattern "Test run summary:.*" -AllMatches
    if ($summaryMatch.Matches.Count -gt 0) {
        Write-Host "`n=== TEST SUMMARY ===" -ForegroundColor Cyan
        $summaryStart = $summaryMatch.Matches[0].Index
        $summaryEnd = $output.IndexOf("`n`n", $summaryStart)
        if ($summaryEnd -lt 0) { $summaryEnd = $output.Length }
        $summary = $output.Substring($summaryStart, $summaryEnd - $summaryStart)
        Write-Host $summary
    }
    
    if ($success) {
        Write-Host "`nSource Generation tests PASSED in $($stopwatch.Elapsed)" -ForegroundColor Green
    } else {
        Write-Host "`nSource Generation tests completed with exit code $LASTEXITCODE in $($stopwatch.Elapsed)" -ForegroundColor Yellow
    }
    
    exit $LASTEXITCODE
} catch {
    Write-Host "Source Generation tests FAILED with error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}