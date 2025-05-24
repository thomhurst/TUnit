---
sidebar_position: 1
---

# Installing TUnit

## Easily

Assuming you have the .NET SDK installed, simply run:

`dotnet new install TUnit.Templates`
`dotnet new TUnit -n "YourProjectName"`

A new test project will be created for you with some samples of different test types and tips. When you're ready to get going, delete them and create your own!

## Manually

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
      <PackageReference Include="TUnit" Version="$(TUnitVersion)" />
    </ItemGroup>

</Project>
```

:::danger

If you're used to other testing frameworks, you're probably used to the package `Microsoft.NET.Test.Sdk`.
This should NOT be used with TUnit. It'll stop test discovery from working properly.

:::

## .NET Framework
If you are still targeting .NET Framework, TUnit will try to Polyfill some missing types that are used by the compiler, such as the `ModuleInitialiserAttribute`.

If you have issues with other Polyfill libraries also defining them, in your project files, you can define the property `<EnableTUnitPolyfills>false</EnableTUnitPolyfills>` to stop TUnit generating them for you.
