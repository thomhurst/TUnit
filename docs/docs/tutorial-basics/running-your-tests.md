---
sidebar_position: 3
---

# Running your tests

Simply execute `dotnet run` or `dotnet test` !

`dotnet run` is the preferred method, allowing easier passing in of command line flags.

```powershell
cd YourTestProjectNameHere
dotnet run --report-trx --coverage
```

# IDE
As of August 2024, IDE support is still in development

## Visual Studio
If you install Visual Studio, you can navigate to the preview/experimental options and enable the testing server option. Restart Visual Studio, build your test project, and the test explorer should show your tests.

## Rider
Not yet supported, but in development I believe.