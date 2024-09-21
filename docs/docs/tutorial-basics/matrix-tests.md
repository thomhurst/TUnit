---
sidebar_position: 6
---

# Matrix Tests

Combinative tests can take multiple values for different arguments, and then generate every possible combination of all of those arguments.

Now bear in mind, that as your number of arguments and/or parameters increase, that the number of test cases will grow exponentially. This means you could very quickly get into the territory of generating thousands of test cases. So use it with caution.

For our arguments, we'll add a `[Matrix]` attribute. Instead of this being added to the test method, it's added to the parameters themselves.

Here's an example:

```csharp
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Is;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    public async Task MyTest(
        [Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)] int value1,
        [Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)] int value2
        )
    {
        var result = Add(value1, value2);

        await Assert.That(result).IsPositive();
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}
```

That will generate 100 test cases. 10 different values for value1, and 10 different values for value2. 10*10 is 100.