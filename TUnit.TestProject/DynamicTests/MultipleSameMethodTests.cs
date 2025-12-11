using TUnit.TestProject.Attributes;

#pragma warning disable TUnitWIP0001

namespace TUnit.TestProject.DynamicTests;

[EngineTest(ExpectedResult.Pass)]
public class MultipleSameMethodTests
{
    [DynamicTestBuilder]
    public void BuildDynamicTests(DynamicTestBuilderContext context)
    {
        // Add multiple tests calling the same method with different arguments
        context.AddTest(new DynamicTest<MultipleSameMethodTests>
        {
            TestMethod = @class => @class.ParametrizedTest(DynamicTestHelper.Argument<string>()),
            TestMethodArguments = ["First"],
        });

        context.AddTest(new DynamicTest<MultipleSameMethodTests>
        {
            TestMethod = @class => @class.ParametrizedTest(DynamicTestHelper.Argument<string>()),
            TestMethodArguments = ["Second"],
        });

        context.AddTest(new DynamicTest<MultipleSameMethodTests>
        {
            TestMethod = @class => @class.ParametrizedTest(DynamicTestHelper.Argument<string>()),
            TestMethodArguments = ["Third"],
        });
    }

    public async Task ParametrizedTest(string value)
    {
        Console.WriteLine($"Test called with: {value}");
        await Assert.That(value).IsNotNull();
        await Assert.That(value).IsNotEmpty();
    }
}
