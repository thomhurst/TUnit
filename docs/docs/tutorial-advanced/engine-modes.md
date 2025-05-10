---
sidebar_position: 30
---

# Engine Modes

In some scenarios, source generation may not be available for you.

For example, TUnit source generates C# code, so if you're using F# or VB.NET, then you wouldn't discover any tests.

To resolve this, TUnit can instead be set to reflection mode, where it'll try to discover and execute your tests using reflection instead.

TUnit will try its best to detect F# and VB projects and automatically set them to reflection mode.

However, if you need to manually set this, then in your `.csproj` add the following property:

```xml
    <PropertyGroup>
        <TUnitReflectionScanner>true</TUnitReflectionScanner>
    </PropertyGroup>
```