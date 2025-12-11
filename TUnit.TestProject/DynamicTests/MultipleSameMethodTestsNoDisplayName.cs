using TUnit.TestProject.Attributes;

#pragma warning disable TUnitWIP0001

namespace TUnit.TestProject.DynamicTests;

[EngineTest(ExpectedResult.Pass)]
public class MultipleSameMethodTestsNoDisplayName
{
    [DynamicTestBuilder]
    public void BuildDynamicTests(DynamicTestBuilderContext context)
    {
        // Add multiple tests calling the same method with different arguments
        // WITHOUT custom display names - should still work due to UniqueId
        context.AddTest(new DynamicTest<MultipleSameMethodTestsNoDisplayName>
        {
            TestMethod = @class => @class.ParametrizedTest(DynamicTestHelper.Argument<string>()),
            TestMethodArguments = ["First"],
        });

        context.AddTest(new DynamicTest<MultipleSameMethodTestsNoDisplayName>
        {
            TestMethod = @class => @class.ParametrizedTest(DynamicTestHelper.Argument<string>()),
            TestMethodArguments = ["Second"],
        });

        context.AddTest(new DynamicTest<MultipleSameMethodTestsNoDisplayName>
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
