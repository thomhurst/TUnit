---
sidebar_position: 1
---

# Installing TUnit

First create an empty .NET console application:

```powershell
dotnet new console --name YourTestProjectNameHere
```

To that project add the `TUnit` package:

```powershell
cd YourTestProjectNameHere
dotnet add package TUnit --prerelease
```

And then remove any automatically generated `Program.cs` or main method, as this'll be taken care of by the TUnit package.

That's it. We're ready to write our first test.

Your `.csproj` should be as simple as something like:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="TUnit" VersionOverride="$(TUnitVersion)" />
    </ItemGroup>

</Project>
```

## Incompatibilities

If you're used to other testing frameworks, you're probably used to the package `Microsoft.NET.Test.Sdk`.
This should NOT be used with TUnit. It'll stop test discovery from working properly.

## .NET 8 and up
As TUnit is a newer framework, it has been built with next-generation .NET applications in mind. So it will support .NET 8 and later. This allows the framework to utilise newer language and framework capabilities, and not have to deal with legacy code.