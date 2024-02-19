---
sidebar_position: 1
---

# Installing TUnit

First create an empty .NET class library application:

`dotnet new classlib --name MyTestProject`

To that project add the `TUnit` and `Microsoft.NET.Test.Sdk` packages:

```
cd MyTestProject
dotnet add package TUnit --prerelease
dotnet add package Microsoft.NET.Test.Sdk
```

That's it. We're ready to write our first test.