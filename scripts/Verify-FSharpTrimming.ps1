#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagesPath,
    [Parameter(Mandatory)][string]$Version
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$packages = (Resolve-Path -LiteralPath $PackagesPath).Path
$root = Join-Path $repositoryRoot 'artifacts/fsharp-trimming'
if (Test-Path $root) { Remove-Item -Recurse -Force $root }
New-Item -ItemType Directory -Force $root | Out-Null

# Inspect the archive, rather than source pack items, to catch packaging regressions.
$package = Get-Item (Join-Path $packages "TUnit.Core.$Version.nupkg")
$archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
try {
    $targets = @($archive.Entries | Where-Object { $_.FullName -match '^build(Transitive)?/.*/TUnit.Core.targets$' })
    if (!($targets.FullName -like 'build/*') -or !($targets.FullName -like 'buildTransitive/*')) {
        throw 'Both build and buildTransitive targets must be packaged.'
    }
    foreach ($target in $targets) {
        $descriptor = $target.FullName -replace 'TUnit.Core.targets$', 'TUnit.Core.FSharp.xml'
        if (!$archive.GetEntry($descriptor)) { throw "Missing packaged descriptor: $descriptor" }
    }
} finally { $archive.Dispose() }

# Isolate consumers from repository build settings, central versions and cached TUnit packages.
'<Project />' | Set-Content (Join-Path $root 'Directory.Build.props')
'<Project />' | Set-Content (Join-Path $root 'Directory.Build.targets')
'<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>' |
    Set-Content (Join-Path $root 'Directory.Packages.props')
@"
<configuration><packageSources><clear />
<add key="produced" value="$([Security.SecurityElement]::Escape($packages))" />
<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources><packageSourceMapping>
<packageSource key="produced"><package pattern="TUnit.*" /></packageSource>
<packageSource key="nuget.org"><package pattern="*" /></packageSource>
</packageSourceMapping></configuration>
"@ | Set-Content (Join-Path $root 'NuGet.Config')
[xml]$centralVersions = Get-Content (Join-Path $repositoryRoot 'Directory.Packages.props') -Raw
$fsharpVersion = ($centralVersions.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'FSharp.Core').Version

function Invoke-DotNet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $Arguments failed ($LASTEXITCODE)." }
}

$library = Join-Path $root 'Workflows'
New-Item -ItemType Directory $library | Out-Null
@"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net10.0</TargetFramework><DisableImplicitFSharpCoreReference>true</DisableImplicitFSharpCoreReference></PropertyGroup>
<ItemGroup><Compile Include="Workflows.fs" /><PackageReference Include="FSharp.Core" Version="$fsharpVersion" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $library 'Workflows.fsproj')
@'
namespace Smoke

type Workflows =
    static let mutable didRun = false
    static member DidRun = didRun
    static member Success() : obj = box (async {
        do! Async.Sleep 1
        didRun <- true
    })
    static member Failure() : obj = box (async {
        do! Async.Sleep 1
        failwith "ASYNC_EXCEPTION_PROPAGATED"
    })
'@ | Set-Content (Join-Path $library 'Workflows.fs')
Invoke-DotNet @('build', (Join-Path $library 'Workflows.fsproj'), '-c', 'Release')

foreach ($scenario in @('compile', 'runtime-only', 'no-fsharp', 'disabled')) {
    $directory = Join-Path $root $scenario
    New-Item -ItemType Directory $directory | Out-Null
    $fsharpReferences = ''
    if ($scenario -ne 'no-fsharp') {
        $exclude = if ($scenario -eq 'runtime-only') { ' ExcludeAssets="compile"' } else { '' }
        $fsharpReferences = @"
<Reference Include="Workflows"><HintPath>../Workflows/bin/Release/net10.0/Workflows.dll</HintPath></Reference>
<PackageReference Include="FSharp.Core" Version="$fsharpVersion"$exclude />
"@
    }
    @"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>
<PublishTrimmed>true</PublishTrimmed><PublishAot>false</PublishAot><SelfContained>true</SelfContained>
<RestorePackagesPath>../packages</RestorePackagesPath><TrimmerSingleWarn>false</TrimmerSingleWarn>
</PropertyGroup>
<ItemGroup><PackageReference Include="TUnit.Core" Version="$Version" />$fsharpReferences</ItemGroup>
</Project>
"@ | Set-Content (Join-Path $directory 'Smoke.csproj')
    $program = if ($scenario -eq 'no-fsharp') {
        'await TUnit.Core.AsyncConvert.ConvertObject(Task.CompletedTask); Console.WriteLine("SMOKE_PASSED");'
    } else {
        @'
await TUnit.Core.AsyncConvert.ConvertObject(Smoke.Workflows.Success());
if (!Smoke.Workflows.DidRun) throw new Exception("ASYNC_BODY_SKIPPED");
bool propagated = false;
try { await TUnit.Core.AsyncConvert.ConvertObject(Smoke.Workflows.Failure()); }
catch (Exception e) when (e.Message == "ASYNC_EXCEPTION_PROPAGATED") { propagated = true; }
if (!propagated) throw new Exception("ASYNC_EXCEPTION_SWALLOWED");
Console.WriteLine("SMOKE_PASSED");
'@
    }
    $program | Set-Content (Join-Path $directory 'Program.cs')
    if ($scenario -eq 'disabled') {
        # Remove only TUnit's descriptor after its target runs; leave trimming enabled.
        @'
<Project><Target Name="DisablePreservation" AfterTargets="_TUnitPreserveFSharpAsync">
<ItemGroup><TrimmerRootDescriptor Remove="@(TrimmerRootDescriptor)" Condition="'%(Filename)' == 'TUnit.Core.FSharp'" /></ItemGroup>
</Target></Project>
'@ | Set-Content (Join-Path $directory 'Directory.Build.targets')
    }
    $publish = Join-Path $directory 'publish'
    $output = & dotnet publish (Join-Path $directory 'Smoke.csproj') -c Release -o $publish 2>&1
    $publishExit = $LASTEXITCODE
    $output | Set-Content (Join-Path $directory 'publish.log')
    if ($publishExit -ne 0) { throw "Publish failed for $scenario`: $output" }
    if ($scenario -eq 'no-fsharp' -and ($output | Select-String '\bwarning\b')) {
        throw "C# publish without FSharp.Core produced warnings: $output"
    }
    $executable = Join-Path $publish $(if ($IsWindows) { 'Smoke.exe' } else { 'Smoke' })
    $execution = & $executable 2>&1
    $executionExit = $LASTEXITCODE
    $execution | Set-Content (Join-Path $directory 'execution.log')
    if ($scenario -eq 'disabled') {
        if ($executionExit -eq 0 -or !($execution | Select-String 'ASYNC_BODY_SKIPPED')) {
            throw "Negative control did not detect skipped async execution: $execution"
        }
    } elseif ($executionExit -ne 0 -or !($execution | Select-String '^SMOKE_PASSED$')) {
        throw "Execution failed for $scenario`: $execution"
    }
    Write-Host "Passed: $scenario"
}
