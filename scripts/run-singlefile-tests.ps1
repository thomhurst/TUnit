#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Framework = "net9.0",
    [string]$Configuration = "Release",
    [string]$Filter = "/*/*/*/*[EngineTest=Pass]"
)

$ErrorActionPreference = "Stop"

Write-Host "Running SingleFile tests..." -ForegroundColor Yellow

# Publish for the SDK's own runtime identifier: a hard-coded one targeted the wrong architecture
# on Intel Macs and on Arm64 Linux/Windows.
$isWindowsPlatform = ($PSVersionTable.PSVersion.Major -lt 6) -or ((Get-Variable -Name 'IsWindows' -ErrorAction SilentlyContinue) -and $IsWindows)
$executableName = if ($isWindowsPlatform) { "TUnit.TestProject.exe" } else { "TUnit.TestProject" }

# Change to test project directory
$testProjectDir = Join-Path $PSScriptRoot "..\tests\TUnit.TestProject"
if (-not (Test-Path $testProjectDir)) {
    Write-Error "Test project directory not found: $testProjectDir"
    exit 1
}

Push-Location $testProjectDir
try {
    Write-Host "Building SingleFile version..." -ForegroundColor Yellow

    dotnet publish `
        -f $Framework `
        -c $Configuration `
        --use-current-runtime `
        -p:SingleFile=true `
        -o "TESTPROJECT_SINGLEFILE" 2>&1 | Out-String | Write-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Host "SingleFile build completed with exit code $LASTEXITCODE" -ForegroundColor Yellow
        exit $LASTEXITCODE
    }

    Write-Host "Running SingleFile tests..." -ForegroundColor Yellow
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $singleFileExecutable = Join-Path "TESTPROJECT_SINGLEFILE" $executableName
    # Quoted: on Linux/macOS PowerShell glob-expands unquoted native arguments, turning the
    # filter into a list of matching file paths.
    & $singleFileExecutable --treenode-filter "$Filter" 2>&1 | Out-String | Write-Host

    $success = $LASTEXITCODE -eq 0
    $stopwatch.Stop()

    if ($success) {
        Write-Host "SingleFile tests PASSED in $($stopwatch.Elapsed)" -ForegroundColor Green
    } else {
        Write-Host "SingleFile tests completed with exit code $LASTEXITCODE in $($stopwatch.Elapsed)" -ForegroundColor Yellow
    }

    exit $LASTEXITCODE
} catch {
    Write-Host "SingleFile tests FAILED with error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}
