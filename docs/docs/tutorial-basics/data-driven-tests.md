# Data Driven Tests

It's common to want to repeat tests but pass in different values on each execution.
We can do that with a data driven test.

Compile-time known data can be injected via `[Arguments(...)]` attributes. 
This attribute takes an array of arguments. It can take as many as you like, but your test method has to have the same number of parameters and they must be the same type.
If you include multiple `[Arguments]` attributes, your test will be repeated that many times, containing the data passed into the attribute.

When your test is executed, TUnit will pass the values provided in the attribute, into the test by the parameters.
Here's an example:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Arguments(1, 1, 2)]
    [Arguments(1, 2, 3)]
    [Arguments(2, 2, 4)]
    [Arguments(4, 3, 7)]
    [Arguments(5, 5, 10)]
    public async Task MyTest(int value1, int value2, int expectedResult)
    {
        var result = Add(value1, value2);

        await Assert.That(result).IsEqualTo(expectedResult);
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```
