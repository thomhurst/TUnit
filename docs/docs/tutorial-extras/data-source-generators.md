---
sidebar_position: 13
---

# Data Source Generators

TUnit exposes an `abstract` `DataSourceGeneratorAttribute` class that you can inherit from and implement your own logic for creating values.

The `DataSourceGeneratorAttribute` class uses generic `Type` arguments to keep your data strongly typed.

This attribute can be useful to easily populate data in a generic way, and without having to define lots of specific `MethodDataSources`

If you just need to generate data for a single parameter, you simply return `T`.

If you need to generate data for multiple parameters, you must use a `Tuple<>` return type. E.g. `return (T1, T2, T3)`

Here's an example that uses AutoFixture to generate arguments:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class AutoFixtureGeneratorAttribute<T1, T2, T3> : DataSourceGeneratorAttribute<T1, T2, T3>
{
    public override IEnumerable<(T1, T2, T3)> GenerateDataSources()
    {
        var fixture = new Fixture();
        
        yield return (fixture.Create<T1>(), fixture.Create<T2>(), fixture.Create<T3>());
    }
}

[AutoFixtureGenerator<SomeClass1, SomeClass2, SomeClass3>]
public class MyTestClass(SomeClass1 someClass1, SomeClass2 someClass2, SomeClass3 someClass3)
{
    [Test]
    [AutoFixtureGenerator<int, string, bool>]
    public async Task Test((int value, string value2, bool value3))
    {
        // ...
    }
}
```
