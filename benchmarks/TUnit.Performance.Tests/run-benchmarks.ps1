#!/usr/bin/env pwsh

<#
.SYNOPSIS
Runs the TUnit performance benchmarks with various configurations

.PARAMETER Filter
Filter to apply to benchmark selection (e.g., "*Discovery*", "*Execution*", "*DataSource*")

.PARAMETER Job
Specify which runtime to benchmark ("Net80", "NativeAot80", or "All")

.PARAMETER Quick
Run quick benchmarks with reduced iterations

.EXAMPLE
./run-benchmarks.ps1
./run-benchmarks.ps1 -Filter "*Discovery*"
./run-benchmarks.ps1 -Job NativeAot80
./run-benchmarks.ps1 -Quick
#>

param(
    [string]$Filter = "*",
    [string]$Job = "All",
    [switch]$Quick
)

Write-Host "Building TUnit Performance Tests..." -ForegroundColor Green
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

$args = @()

if ($Filter -ne "*") {
    $args += "--filter"
    $args += $Filter
}

if ($Job -eq "Net80") {
    $args += "--job"
    $args += "Net80"
} elseif ($Job -eq "NativeAot80") {
    $args += "--job"
    $args += "NativeAot80"
}

if ($Quick) {
    $args += "--iterationCount"
    $args += "3"
    $args += "--warmupCount"
    $args += "1"
}

Write-Host "Running benchmarks with arguments: $args" -ForegroundColor Cyan
dotnet run -c Release -- @args

Write-Host "`nBenchmark results are saved in BenchmarkDotNet.Artifacts/results/" -ForegroundColor Yellow