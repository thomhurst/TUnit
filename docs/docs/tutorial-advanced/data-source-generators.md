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
    public override IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var fixture = new Fixture();
        
        yield return () => (fixture.Create<T1>(), fixture.Create<T2>(), fixture.Create<T3>());
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

Notes:
`GenerateDataSources()` could be called multiple times if you have nested loops to generate data within your tests. Because of this, you are required to return a `Func` - This means that tests can create a new object each time for a test case. Otherwise, we'd be pointing to the same object if we were in a nested loop and that could lead to unintended side-effects.

An example could be using a DataSourceGenerator on both the class and the test method, resulting with a loop within a loop.

Because this could be called multiple times, if you're subscribing to test events and storing state within the attribute, be aware of this and how this could affect disposal etc.

Instead, you can use the `yield return` pattern, and use the `TestBuilderContext` from the `DataGeneratorMetadata` object passed to you.
After each `yield`, the execution is passed back to TUnit, and TUnit will set a new `TestBuilderContext` for you - So as long as you yield each result, you'll get a unique context object for each test case.
The `TestBuilderContext` object exposes `Events` - And you can register a delegate to be invoked on them at the point in the test lifecycle that you wish.

```csharp

    public override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        dataGeneratorMetadata.TestBuilderContext.Current; // <-- Initial Context for first test
        
        yield return () => 1;
        
        dataGeneratorMetadata.TestBuilderContext.Current; // <-- This is now a different context object, as we yielded
        dataGeneratorMetadata.TestBuilderContext.Current; // <-- This is still the same as above because it'll only change on a yield
        
        yield return () => 2;
        
        dataGeneratorMetadata.TestBuilderContext.Current; // <-- A new object again
    }

```
