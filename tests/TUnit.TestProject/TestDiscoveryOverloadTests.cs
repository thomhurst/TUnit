using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Two overloads of the same [After(TestDiscovery)] hook: both must run in source-generated and reflection mode.
public static class TestDiscoveryOverloadHooks
{
    public static int WithoutContextCalls;
    public static int WithContextCalls;

    [After(TestDiscovery)]
    public static void Inspect()
    {
        Interlocked.Increment(ref WithoutContextCalls);
    }

    [After(TestDiscovery)]
    public static void Inspect(TestDiscoveryContext context)
    {
        Interlocked.Increment(ref WithContextCalls);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class TestDiscoveryOverloadTests
{
    [Test]
    public async Task Both_Overloads_Ran()
    {
        await Assert.That(TestDiscoveryOverloadHooks.WithoutContextCalls).IsEqualTo(1);
        await Assert.That(TestDiscoveryOverloadHooks.WithContextCalls).IsEqualTo(1);
    }
}
