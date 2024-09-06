---
sidebar_position: 3
---

# Running your tests

As TUnit is built on-top of the newer Microsoft.Testing.Platform, and combined with the fact that TUnit tests are source generated, running your tests is available in a variety of ways. 

## dotnet run

For a simple execution of a project, `dotnet run` is the preferred method, allowing easier passing in of command line flags.

```powershell
cd 'C:/Your/Test/Directory'
dotnet run -c Release --report-trx --coverage
```

## dotnet test

`dotnet test` requires any command line flags to be specified as application arguments, meaning after a `--` - Otherwise you'll get an error about unknown switches.

```powershell
cd 'C:/Your/Test/Directory'
dotnet test -c Release -- --report-trx --coverage
```

## dotnet exec

If your test project has already been built, you can use `dotnet exec` with the `.dll` path

```powershell
cd 'C:/Your/Test/Directory/bin/Release/net8.0'
dotnet exec YourTestProject.dll --report-trx --coverage
```

## Published Test Project

When you publish your test project, you'll be given an executable.
On windows this'll be a `.exe` and on Linux/MacOS there'll be no extension.

This can be invoked directly and passed any flags.

```powershell
cd 'C:/Your/Test/Directory/bin/Release/net8.0/win-x64/publish'
./YourTestProject.exe --report-trx --coverage
```

# IDE
As of August 2024, IDE support is still in development

## Visual Studio
If you install Visual Studio, you can navigate to the preview/experimental options and enable the testing server option. Restart Visual Studio, build your test project, and the test explorer should show your tests.

## Rider
Not yet supported, but in development I believe.