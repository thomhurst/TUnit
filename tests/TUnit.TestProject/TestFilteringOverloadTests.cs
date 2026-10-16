using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Two overloads of the same [After(TestFiltering)] hook: both must run in source-generated and reflection mode.
public static class TestFilteringOverloadHooks
{
    public static int WithoutContextCalls;
    public static int WithContextCalls;

    [After(TestFiltering)]
    public static void Inspect()
    {
        Interlocked.Increment(ref WithoutContextCalls);
    }

    [After(TestFiltering)]
    public static void Inspect(TestFilteringContext context)
    {
        Interlocked.Increment(ref WithContextCalls);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestFilteringOverloadTests
{
    [Test]
    public async Task Both_Overloads_Ran()
    {
        await Assert.That(TestFilteringOverloadHooks.WithoutContextCalls).IsEqualTo(1);
        await Assert.That(TestFilteringOverloadHooks.WithContextCalls).IsEqualTo(1);
    }
}
