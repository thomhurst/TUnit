using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.DynamicTests;

/// <summary>
/// Tests that validate DynamicTestIndex generates unique test IDs
/// when multiple dynamic tests target the same method.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DynamicTestIndexTests
{
    public void TestMethod(int value)
    {
        Console.WriteLine($"TestMethod called with value: {value}");
    }

#pragma warning disable TUnitWIP0001
    [DynamicTestBuilder]
#pragma warning restore TUnitWIP0001
    public void BuildTests(DynamicTestBuilderContext context)
    {
        // Add 5 dynamic tests all targeting the SAME method with different arguments.
        // Before the DynamicTestIndex fix, these would generate duplicate test IDs
        // and only one would execute.
        for (var i = 1; i <= 5; i++)
        {
            var value = i;
            context.AddTest(new DynamicTest<DynamicTestIndexTests>
            {
                TestMethod = c => c.TestMethod(value),
                TestMethodArguments = [value],
                Attributes = []
            });
        }
    }
}
