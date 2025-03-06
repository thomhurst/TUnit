---
sidebar_position: 1
---

# Migrating from xUnit.net

## Using TUnit's Code Fixers

TUnit has some code fixers to help automate some of the migration for you.

Now bear in mind, these won't be perfect, and you'll likely still have to do some bits manually, but it should make life a bit easier.

If you think something could be improved, or something seemed to break, raise an issue so we can make this better and work for more people.

### Steps

#### Install the TUnit packages to your test projects
Use your IDE or the dotnet CLI to add the TUnit packages to your test projects

#### Remove the automatically added global usings
If you have the TUnit, TUnit.Engine or TUnit.Core package installed, in your csproj add:

```
    <ItemGroup>
        <Using Remove="TUnit.Core.HookType" Static="True" />
        <Using Remove="TUnit.Core" />
    </ItemGroup>
```

If you have the TUnit or TUnit.Assertions package installed, in your csproj add:

```
    <ItemGroup>
        <Using Remove="TUnit.Assertions" />
        <Using Remove="TUnit.Assertions.Extensions" />
    </ItemGroup>
```

This is temporary - Just to make sure no types clash, and so the code fixers can distinguish between xUnit and TUnit types with similar names.

#### Run the code fixers via the dotnet CLI

Running them in a specific order is recommended.
So try the following:

`dotnet format analyzers --severity info --diagnostics TUnit0052`

`dotnet format analyzers --severity info --diagnostics TUnit0053`

`dotnet format analyzers --severity info --diagnostics TUnitAssertions0009`

`dotnet format analyzers --severity info --diagnostics TUnitAssertions0002`

`dotnet format analyzers --severity info --diagnostics TUnit0054`

The diagnostics that have "Assertions" in them are if you're switching to TUnit assertions.
The last one attempts to remove all `using Xunit;` directives in your code.

#### Perform any manual bits that are still necessary
This bit's on you! You'll have to work out what still needs doing.
Raise an issue if you think it could be automated.

#### Remove the xUnit packages
Simply uninstall them once you've migrated

#### Revert step 1
Undo step 1, and you won't have to have `using TUnit.Core` or `using TUnit.Assertions` in every file.

#### Done! (Hopefully)

## Manually

`[Fact]` becomes `[Test]`

`[Theory]` becomes `[Test]`

`[Trait]` becomes `[Property]`

`[InlineData]` becomes `[Arguments]`

`[MemberData]` becomes `[MethodDataSource]`

`[ClassData]` becomes `[MethodDataSource]` and point to the GetEnumerator method. Objects will need to be converted from `object` to their actual expected types

`[Collection]` becomes `[ClassDataSource<>(Shared = SharedType.Keyed/PerTestSession)]`

`[AssemblyFixture]` becomes `[ClassDataSource<>(Shared = SharedType.PerAssembly)]`

Interfaces:

`IClassFixture<>` becomes an attribute `[ClassDataSource<>(Shared = SharedType.PerClass)]`

`IAsyncLifetime` on a test class becomes a method attributed with `[Before(Test)]`

`IAsyncLifetime` on injected data becomes `IAsyncInitializer`

`I(Async)Disposable` on a test class can remain, or be converted to a method attributed with `[After(Test)]`
