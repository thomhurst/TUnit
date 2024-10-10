---
sidebar_position: 13
---

# Property Injection

TUnit makes it easy to initialise some properties on your test class instead of passing them into the constructor.

Your properties must be marked with the `required` keyword and then simply place a data attribute on it.
The required keyword keeps your code clean and correct. If a property isn't passed in, you'll get a compiler warning, so you know something has gone wrong. It also gets rid of any pesky nullability warnings.

Supported attributes for properties are:
- Argument
- MethodDataSource
- ClassDataSource
- DataSourceGeneratorAttribute (though limited to the first item returned)

This can help simplify base classes with common behaviour and avoid having to write boilerplate constructors everywhere.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class PropertySetterTests
{
    [Arguments("1")]
    public required string Property1 { get; init; }
        
    [MethodDataSource(nameof(MethodData))]
    public required string Property2 { get; init; }
        
    [ClassDataSource<InnerModel>]
    public required InnerModel Property3 { get; init; }
    
    [ClassDataSource<InnerModel>(Shared = SharedType.Globally)]
    public required InnerModel Property4 { get; init; }
    
    [ClassDataSource<InnerModel>(Shared = SharedType.ForClass)]
    public required InnerModel Property5 { get; init; }
    
    [ClassDataSource<InnerModel>(Shared = SharedType.Keyed, Key = "Key")]
    public required InnerModel Property6 { get; init; }
        
    [DataSourceGeneratorTests.AutoFixtureGenerator<string>]
    public required string Property7 { get; init; }
    
    [Test]
    public void Test()
    {
        Console.WriteLine(Property7);
    }
}
```
