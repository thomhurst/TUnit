using System.Runtime.CompilerServices;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.DynamicTests;

[EngineTest(ExpectedResult.Pass)]
public class RegisteredDynamicTestSourceTests
{
    [ModuleInitializer]
    public static void RegisterSource()
    {
        SourceRegistrar.RegisterDynamic(new CustomSource());
    }

    // Deliberately has no [Test] or [DynamicTestBuilder]: only registration discovers it.
    public async Task RegisteredTest(string sessionId)
    {
        await Assert.That(sessionId).IsNotNullOrEmpty();
    }

    private sealed class CustomSource : IDynamicTestSource
    {
        public IReadOnlyList<AbstractDynamicTest> CollectDynamicTests(string sessionId)
        {
            var context = new DynamicTestBuilderContext("RegisteredDynamicTestSourceTests.cs", 1);
            context.AddTest(new DynamicTest<RegisteredDynamicTestSourceTests>
            {
                TestMethod = test => test.RegisteredTest(sessionId),
                TestMethodArguments = [sessionId],
                Attributes = []
            });
            return context.Tests;
        }
    }
}
