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

## .NET 8 and up
As TUnit is a newer framework, it has been built with next-generation .NET applications in mind. So it will support .NET 8 and later. This allows the framework to utilise newer language and framework capabilities, and not have to deal with legacy code.