#!/usr/bin/env pwsh
# Count generated source-generator methods in a compiled TUnit assembly
# Uses binary string scanning — no assembly loading needed
#
# Usage:
#   ./count-generated-methods.ps1                                    # Auto-detect from benchmark build
#   ./count-generated-methods.ps1 -AssemblyPath path/to/assembly.dll

param(
    [string]$AssemblyPath = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $AssemblyPath) {
    $AssemblyPath = Join-Path $scriptDir "bin" $Configuration "net9.0" "TUnit.PerformanceBenchmarks.dll"
}

if (-not (Test-Path $AssemblyPath)) {
    Write-Host "Assembly not found: $AssemblyPath" -ForegroundColor Red
    Write-Host "Build first: dotnet build -c $Configuration" -ForegroundColor Yellow
    exit 1
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Generated Method Count Analysis" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Assembly: $(Split-Path -Leaf $AssemblyPath)" -ForegroundColor White
Write-Host "Size:     $([math]::Round((Get-Item $AssemblyPath).Length / 1MB, 2)) MB"
Write-Host ""

# Read the binary and extract ASCII strings (method names are in the metadata string heap)
$content = [System.IO.File]::ReadAllText($AssemblyPath, [System.Text.Encoding]::ASCII)

# Count method patterns in the string table
# These patterns are unique to source-generated code

# __InvokeTest_ appears once per test method
$invokeTestMatches = [regex]::Matches($content, "__InvokeTest_[A-Za-z0-9_]+")
$invokeTestMethods = ($invokeTestMatches | Select-Object -ExpandProperty Value -Unique).Count

# __Materialize_ appears once per test method (deferred)
$materializeMatches = [regex]::Matches($content, "__Materialize_[A-Za-z0-9_]+")
$materializeMethods = ($materializeMatches | Select-Object -ExpandProperty Value -Unique).Count

# __CreateAttributes_ appears for each unique attribute group
$createAttrMatches = [regex]::Matches($content, "__CreateAttributes_\d+")
$createAttributeMethods = ($createAttrMatches | Select-Object -ExpandProperty Value -Unique).Count

# Count TestSource class names (they appear as type names in metadata)
$testSourceMatches = [regex]::Matches($content, "[A-Za-z0-9_]+__TestSource")
$testSourceTypes = ($testSourceMatches | Select-Object -ExpandProperty Value -Unique).Count

# Count _r_ registration fields
$regFieldMatches = [regex]::Matches($content, "_r_[A-Za-z0-9_]+__TestSource")
$registrationFields = ($regFieldMatches | Select-Object -ExpandProperty Value -Unique).Count

# GetTests and EnumerateTestDescriptors are standard names — count by TestSource types
# (each TestSource class has exactly 1 GetTests and 1 EnumerateTestDescriptors)
$getTestsMethods = $testSourceTypes
$enumerateDescriptorMethods = $testSourceTypes

# CreateInstance — one per TestSource class
$createInstanceMethods = $testSourceTypes

$totalGenerated = $invokeTestMethods + $materializeMethods + $getTestsMethods +
    $enumerateDescriptorMethods + $createAttributeMethods + $createInstanceMethods

$startupJit = $invokeTestMethods + $getTestsMethods + $enumerateDescriptorMethods +
    $createAttributeMethods + $createInstanceMethods

Write-Host "  Type Counts:" -ForegroundColor Yellow
Write-Host "    TestSource classes:         $testSourceTypes"
Write-Host "    Registration fields:        $registrationFields"
Write-Host ""
Write-Host "  Method Counts:" -ForegroundColor Yellow
Write-Host "    __InvokeTest_* methods:     $invokeTestMethods     " -NoNewline
Write-Host "(JIT'd at startup)" -ForegroundColor Red
Write-Host "    __Materialize_* methods:    $materializeMethods     " -NoNewline
Write-Host "(deferred)" -ForegroundColor Green
Write-Host "    GetTests methods:           $getTestsMethods     " -NoNewline
Write-Host "(JIT'd at startup)" -ForegroundColor Red
Write-Host "    EnumerateTestDescriptors:   $enumerateDescriptorMethods     " -NoNewline
Write-Host "(JIT'd at startup)" -ForegroundColor Red
Write-Host "    __CreateAttributes_*:       $createAttributeMethods     " -NoNewline
Write-Host "(JIT'd at startup)" -ForegroundColor Red
Write-Host "    CreateInstance methods:      $createInstanceMethods     " -NoNewline
Write-Host "(JIT'd at startup)" -ForegroundColor Red
Write-Host ""
Write-Host "  Summary:" -ForegroundColor Cyan
Write-Host "    Total generated methods:    $totalGenerated" -ForegroundColor White
Write-Host "    Est. JIT'd at startup:      $startupJit" -ForegroundColor Red
Write-Host "    Deferred (materializers):   $materializeMethods" -ForegroundColor Green
Write-Host ""

# Target comparison
$targetStartupJit = 3  # After optimization: ~3 methods at startup
$reduction = if ($startupJit -gt 0) { [math]::Round((1 - $targetStartupJit / $startupJit) * 100, 1) } else { 0 }
Write-Host "  Target (after optimization):" -ForegroundColor Yellow
Write-Host "    Current startup JIT:        $startupJit methods"
Write-Host "    Target startup JIT:         ~$targetStartupJit methods"
Write-Host "    Targeted reduction:         $reduction%"
Write-Host ""

# Save as JSON
$resultsDir = Join-Path $scriptDir "benchmark-results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$report = @{
    Timestamp = $timestamp
    Assembly = Split-Path -Leaf $AssemblyPath
    AssemblySizeMB = [math]::Round((Get-Item $AssemblyPath).Length / 1MB, 2)
    Configuration = $Configuration
    TypeCounts = @{
        TestSourceTypes = $testSourceTypes
        RegistrationFields = $registrationFields
    }
    MethodCounts = @{
        InvokeTest = $invokeTestMethods
        Materialize = $materializeMethods
        GetTests = $getTestsMethods
        EnumerateTestDescriptors = $enumerateDescriptorMethods
        CreateAttributes = $createAttributeMethods
        CreateInstance = $createInstanceMethods
        TotalGenerated = $totalGenerated
        EstimatedStartupJit = $startupJit
    }
}

$reportFile = Join-Path $resultsDir "method-count-$timestamp.json"
$report | ConvertTo-Json -Depth 5 | Out-File -FilePath $reportFile -Encoding utf8
Write-Host "  Results saved to: $reportFile" -ForegroundColor DarkGray
