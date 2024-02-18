using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Is;
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [CombinativeTest]
    public async Task MyTest(
        [CombinativeValues(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)] int value1,
        [CombinativeValues(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)] int value2
        )
    {
        var result = Add(value1, value2);

        await Assert.That(result).Is.Positive();
    }

    private int Add(int x, int y)
    {
        return x + y;
    }
}