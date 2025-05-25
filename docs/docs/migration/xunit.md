# Migrating from xUnit.net

## Using TUnit's Code Fixers

TUnit has some code fixers to help automate some of the migration for you.

Now bear in mind, these won't be perfect, and you'll likely still have to do some bits manually, but it should make life a bit easier.

If you think something could be improved, or something seemed to break, raise an issue so we can make this better and work for more people.

### Steps

#### Install the TUnit packages to your test projects
Use your IDE or the dotnet CLI to add the TUnit packages to your test projects

#### Remove the automatically added global usings
In your csproj add:

```xml
    <PropertyGroup>
        <TUnitImplicitUsings>false</TUnitImplicitUsings>
        <TUnitAssertionsImplicitUsings>false</TUnitAssertionsImplicitUsings>
    </PropertyGroup>
```

This is temporary - Just to make sure no types clash, and so the code fixers can distinguish between xUnit and TUnit types with similar names.

#### Rebuild the project
This ensures the TUnit packages have been restored and the analyzers should be loaded.

#### Run the code fixer via the dotnet CLI

`dotnet format analyzers --severity info --diagnostics TUXU0001`

#### Revert step `Remove the automatically added global usings`

#### Perform any manual bits that are still necessary
This bit's on you! You'll have to work out what still needs doing.
Raise an issue if you think it could be automated.

#### Remove the xUnit packages
Simply uninstall them once you've migrated

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

