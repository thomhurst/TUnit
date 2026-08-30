[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagesPath,

    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path $PSScriptRoot -Parent
$resolvedPackagesPath = (Resolve-Path -LiteralPath $PackagesPath).Path
$generatedDirectory = Join-Path $repositoryRoot 'artifacts/doc-tests/generated'
$localPackagesDirectory = Join-Path $generatedDirectory 'packages'
$nugetConfigPath = Join-Path $generatedDirectory 'NuGet.config'
$generatorProjectPath = Join-Path $repositoryRoot 'tools/TUnit.DocSnippetGenerator/TUnit.DocSnippetGenerator.csproj'
$consumerProjectPath = Join-Path $repositoryRoot 'tests/TUnit.DocTests/TUnit.DocTests.csproj'

function Assert-StrictWarningPolicy([string]$projectPath)
{
    $propertyOutput = & dotnet msbuild $projectPath -nologo `
        '-getProperty:NoWarn;TreatWarningsAsErrors;WarningsNotAsErrors'
    if ($LASTEXITCODE -ne 0)
    {
        throw "Could not evaluate warning policy for '$projectPath'."
    }

    $properties = ($propertyOutput | Out-String | ConvertFrom-Json).Properties
    if ($properties.TreatWarningsAsErrors -ne 'true' -or
        $properties.NoWarn -or
        $properties.WarningsNotAsErrors)
    {
        throw "Documentation builds require TreatWarningsAsErrors=true with empty NoWarn and WarningsNotAsErrors. Project: '$projectPath'."
    }
}

Assert-StrictWarningPolicy $generatorProjectPath
Assert-StrictWarningPolicy $consumerProjectPath

$requiredPackages = @(
    'TUnit'
    'TUnit.AspNetCore'
    'TUnit.AspNetCore.Core'
    'TUnit.Aspire'
    'TUnit.Aspire.Core'
    'TUnit.Assertions'
    'TUnit.Assertions.Should'
    'TUnit.Core'
    'TUnit.Engine'
    'TUnit.FsCheck'
    'TUnit.Logging.Microsoft'
    'TUnit.Mocks'
    'TUnit.Mocks.Assertions'
    'TUnit.Mocks.Http'
    'TUnit.Mocks.Logging'
    'TUnit.OpenTelemetry'
    'TUnit.Playwright'
)

function Get-PackagePath([string]$packageId)
{
    $packageVersion = if ($packageId -eq 'TUnit.Assertions.Should') { "$Version-beta" } else { $Version }
    $fileName = "$packageId.$packageVersion.nupkg"
    $matches = @(Get-ChildItem -LiteralPath $resolvedPackagesPath -Recurse -File -Filter $fileName)
    if ($matches.Count -eq 0)
    {
        throw "Could not find '$fileName' beneath '$resolvedPackagesPath'."
    }

    return $matches[0]
}

New-Item -ItemType Directory -Force -Path $generatedDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $localPackagesDirectory | Out-Null
Get-ChildItem -LiteralPath $localPackagesDirectory -File -Filter '*.nupkg' | Remove-Item -Force

foreach ($packageId in $requiredPackages)
{
    Copy-Item -LiteralPath (Get-PackagePath $packageId).FullName -Destination $localPackagesDirectory
}

& dotnet run --project $generatorProjectPath -c Release -- $repositoryRoot $generatedDirectory
if ($LASTEXITCODE -ne 0)
{
    throw "Documentation snippet generation failed with exit code $LASTEXITCODE."
}

[IO.File]::WriteAllText(
    $nugetConfigPath,
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-packages" value="$([Security.SecurityElement]::Escape($localPackagesDirectory))" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@)

& dotnet restore $consumerProjectPath --configfile $nugetConfigPath `
    "-p:TUnitPackageVersion=$Version" `
    "-p:TUnitAssertionsShouldPackageVersion=$Version-beta" `
    "-p:GeneratedSnippetsDirectory=$generatedDirectory"
if ($LASTEXITCODE -ne 0)
{
    throw "Documentation snippet restore failed with exit code $LASTEXITCODE."
}

& dotnet build $consumerProjectPath -c Release --no-restore --nologo '-clp:ErrorsOnly' `
    "-p:TUnitPackageVersion=$Version" `
    "-p:TUnitAssertionsShouldPackageVersion=$Version-beta" `
    "-p:GeneratedSnippetsDirectory=$generatedDirectory"
if ($LASTEXITCODE -ne 0)
{
    throw "Documentation snippet build failed with exit code $LASTEXITCODE."
}

$isolatedDirectory = Join-Path $generatedDirectory 'isolated'
foreach ($snippetDirectory in Get-ChildItem -LiteralPath $isolatedDirectory -Directory)
{
    & dotnet build $consumerProjectPath -c Release --no-restore --nologo '-clp:ErrorsOnly' `
        "-p:TUnitPackageVersion=$Version" `
        "-p:TUnitAssertionsShouldPackageVersion=$Version-beta" `
        "-p:GeneratedSnippetsDirectory=$($snippetDirectory.FullName)"
    if ($LASTEXITCODE -ne 0)
    {
        throw "Documentation snippet build failed for $($snippetDirectory.Name) with exit code $LASTEXITCODE."
    }
}
