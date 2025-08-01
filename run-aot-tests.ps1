#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$Framework = "net9.0",
    [string]$Configuration = "Release",
    [string]$Filter = "/*/*/*/*[EngineTest=Pass]"
)

$ErrorActionPreference = "Stop"

Write-Host "Running AOT tests..." -ForegroundColor Yellow

# Get runtime identifier for platform-specific builds
function Get-RuntimeIdentifier {
    # For PowerShell 5.x compatibility on Windows
    if ($PSVersionTable.PSVersion.Major -lt 6) {
        # Windows PowerShell 5.x
        return "win-x64"
    }
    # PowerShell Core 6+
    if ($IsWindows) {
        return "win-x64"
    } elseif ($IsLinux) {
        return "linux-x64"
    } elseif ($IsMacOS) {
        return "osx-x64"
    } else {
        # Default to Windows if platform detection fails
        return "win-x64"
    }
}

$rid = Get-RuntimeIdentifier
$isWindowsPlatform = ($PSVersionTable.PSVersion.Major -lt 6) -or ((Get-Variable -Name 'IsWindows' -ErrorAction SilentlyContinue) -and $IsWindows)
$executableName = if ($isWindowsPlatform) { "TUnit.TestProject.exe" } else { "TUnit.TestProject" }

# Change to test project directory
$testProjectDir = Join-Path $PSScriptRoot "TUnit.TestProject"
if (-not (Test-Path $testProjectDir)) {
    Write-Error "Test project directory not found: $testProjectDir"
    exit 1
}

Push-Location $testProjectDir
try {
    Write-Host "Building AOT version..." -ForegroundColor Yellow
    
    dotnet publish `
        -f $Framework `
        -c $Configuration `
        -r $rid `
        -p:Aot=true `
        -o "TESTPROJECT_AOT" 2>&1 | Out-String | Write-Host
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "AOT build completed with exit code $LASTEXITCODE" -ForegroundColor Yellow
        exit $LASTEXITCODE
    }
    
    Write-Host "Running AOT tests..." -ForegroundColor Yellow
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    $aotExecutable = Join-Path "TESTPROJECT_AOT" $executableName
    & $aotExecutable --treenode-filter $Filter 2>&1 | Out-String | Write-Host
    
    $success = $LASTEXITCODE -eq 0
    $stopwatch.Stop()
    
    if ($success) {
        Write-Host "AOT tests PASSED in $($stopwatch.Elapsed)" -ForegroundColor Green
    } else {
        Write-Host "AOT tests completed with exit code $LASTEXITCODE in $($stopwatch.Elapsed)" -ForegroundColor Yellow
    }
    
    exit $LASTEXITCODE
} catch {
    Write-Host "AOT tests FAILED with error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}