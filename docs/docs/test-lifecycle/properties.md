# Properties

Custom properties can be added to a test using the `[PropertyAttribute]`.

Custom properties can be used for test filtering: `dotnet run --treenode-filter /*/*/*/*[PropertyName=PropertyValue]`

Custom properties can also be viewed in the `TestContext` - Which can be used in logic during setups or cleanups.

This can be used on base classes and inherited to affect all tests in sub-classes.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Property("PropertyName", "PropertyValue")]
    public async Task MyTest(CancellationToken cancellationToken)
    {
        
    }
}
```
