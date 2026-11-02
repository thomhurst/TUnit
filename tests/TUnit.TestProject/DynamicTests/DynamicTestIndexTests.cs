using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.DynamicTests;

/// <summary>
/// Tests that validate DynamicTestIndex generates unique test IDs
/// when multiple dynamic tests target the same method.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DynamicTestIndexTests
{
    private static int _builderInvocations;

    public async Task TestMethod(int value)
    {
        Console.WriteLine($"TestMethod called with value: {value}");
        await Assert.That(_builderInvocations).IsEqualTo(1);
        var details = TestContext.Current!.Metadata.TestDetails;
        await Assert.That(Path.GetFileName(details.TestFilePath)).IsEqualTo("DynamicTestIndexTests.cs");
        await Assert.That(details.TestLineNumber).IsGreaterThan(0);
    }

#pragma warning disable TUnitWIP0001
    [DynamicTestBuilder]
#pragma warning restore TUnitWIP0001
    public void BuildTests(DynamicTestBuilderContext context)
    {
        Interlocked.Increment(ref _builderInvocations);

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
