using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.DynamicTests;

/// <summary>
/// Tests that validate custom DisplayName is correctly applied to dynamic tests.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DynamicTestDisplayNameTests
{
    public async Task TestWithCustomDisplayName()
    {
        await Assert.That(TestContext.Current!.Metadata.DisplayName).IsEqualTo("My Custom Dynamic Test Name");
    }

    public async Task TestWithParameterizedDisplayName(int value)
    {
        await Assert.That(TestContext.Current!.Metadata.DisplayName).IsEqualTo($"Dynamic Test with value {value}");
    }

    public async Task TestWithDefaultDisplayName()
    {
        // When DisplayName is not set, it should fall back to the default generated name
        await Assert.That(TestContext.Current!.Metadata.DisplayName).Contains("TestWithDefaultDisplayName");
    }

#pragma warning disable TUnitWIP0001
    [DynamicTestBuilder]
#pragma warning restore TUnitWIP0001
    public void BuildTests(DynamicTestBuilderContext context)
    {
        // Test with a custom display name
        context.AddTest(new DynamicTest<DynamicTestDisplayNameTests>
        {
            TestMethod = c => c.TestWithCustomDisplayName(),
            DisplayName = "My Custom Dynamic Test Name"
        });

        // Test with parameterized display name
        context.AddTest(new DynamicTest<DynamicTestDisplayNameTests>
        {
            TestMethod = c => c.TestWithParameterizedDisplayName(42),
            TestMethodArguments = [42],
            DisplayName = "Dynamic Test with value 42"
        });

        // Another parameterized test with different value
        context.AddTest(new DynamicTest<DynamicTestDisplayNameTests>
        {
            TestMethod = c => c.TestWithParameterizedDisplayName(100),
            TestMethodArguments = [100],
            DisplayName = "Dynamic Test with value 100"
        });

        // Test without custom display name (should use default)
        context.AddTest(new DynamicTest<DynamicTestDisplayNameTests>
        {
            TestMethod = c => c.TestWithDefaultDisplayName()
            // DisplayName intentionally not set
        });
    }
}
