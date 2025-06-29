using TUnit.Core.Extensions;
using TUnit.TestProject.Attributes;

#pragma warning disable WIP

namespace TUnit.TestProject.DynamicTests;

[EngineTest(ExpectedResult.Pass)]
[RunOnDiscovery]
[Arguments(1, 2, 3)]
[Arguments(101, 202, 303)]
public class Runtime(int a, int b, int c)
{
    public void SomeMethod(int arg1, int arg2, int arg3)
    {
        Console.WriteLine(@"SomeMethod called with:");
        Console.WriteLine($@"Class args: {a}, {b}, {c}");
        Console.WriteLine($@"Method args: {arg1}, {arg2}, {arg3}");
    }

    [Test]
    [Arguments(4, 5, 6)]
    [Arguments(404, 505, 606)]
    public async Task BuildTests(int d, int e, int f)
    {
        var context = TestContext.Current!;

        await context.AddDynamicTest(new DynamicTestInstance<Runtime>
        {
            TestMethod = @class => @class.SomeMethod(0, 0, 0),
            TestClassArguments = [a + 10, b + 10, c + 10],
            TestMethodArguments = [d + 10, e + 10, f + 10],
            Attributes = [new RepeatAttribute(5)]
        });
    }
}
